using PlexMetadataConfigurer.DTO;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace PlexMetadataConfigurer;

/// <summary>
///		Cleans up the metadata for a Plex library with no metadata agent
/// </summary>
/// <remarks>
///		Short-term this changes the titles of seasons and episodes based on the
///		directory structure and filenames.
///		
///		Long-term this should try to read from a file in each directory to specify
///		several metadata properties for each episode/season, and fall back to parsing
///		the filenames if that config file is missing
/// </remarks>
internal class Program
{
	static async Task Main(string[] args)
	{
		using HttpClient client = new();
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
		client.DefaultRequestHeaders.Add("User-Agent", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Product", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Token", PlexConfig.AuthToken);

		// a Plex sectionKey identifies a Library
		string? sectionKey = await FindLibrarySectionKeyAsync(client);
		if (string.IsNullOrEmpty(sectionKey))
			return;

		var shows = await GetFilteredShowsAsync(client, sectionKey);
		foreach (var show in shows)
		{
			var seasons = await GetAllSeasons(client, show.Key);

			foreach (var season in seasons)
			{
				var episodes = await GetAllEpisodes(client, season.Key);

				Console.WriteLine($"'{season.Title}' ({season.Key}) has {episodes.Count} unwatched episodes");
				// get the season dir name (need to use an episode's media path to do this)
				var firstFilename = episodes.FirstOrDefault()?.Media.FirstOrDefault()?.Part.FirstOrDefault()?.File;
				var newSeasonTitle = TitleFilenameParser.SeasonTitle(firstFilename);

				// if parsed, send PUT to update season title
				if (!string.IsNullOrWhiteSpace(newSeasonTitle) && (PlexConfig.AlwaysModify || !newSeasonTitle.Equals(season.Title)))
				{
					Console.WriteLine($"\tTitle: '{season.Title}' => '{newSeasonTitle}'");

					var success = await UpdateSeason(client, sectionKey, season.Key, newSeasonTitle);
					if (success)
						Console.WriteLine($"\tOK");
					else
						Console.WriteLine($"\tUpdate failed.");
				}
				Console.WriteLine();


				foreach (var episode in episodes)
				{
					Media? media = episode.Media?.FirstOrDefault();
					var updatedValues = new EpisodeUpdate();

					Console.WriteLine($"{episode.Key} \t{media?.Part.FirstOrDefault()?.File}");

					var titleFromMedia = EpisodeTitleFromMediaPath(media);
					if (!string.IsNullOrEmpty(titleFromMedia) && (PlexConfig.AlwaysModify || !titleFromMedia.Equals(episode.Title)))
					{
						Console.WriteLine($"\tTitle: '{episode.Title}' => '{titleFromMedia}'");
						updatedValues.Title = titleFromMedia;
					}

					// todo: other properties

					var success = await UpdateEpisode(client, sectionKey, episode.Key, updatedValues);
					if (success)
						Console.WriteLine($"\tOK");
					else
						Console.WriteLine($"\tUpdate failed.");

					Console.WriteLine();
				}

			}
		}
	}

	private static async Task<IEnumerable<Show>> GetFilteredShowsAsync(HttpClient client, string sectionKey)
	{
		var shows = await GetAllShowsWithUnwatchedEpisodesAsync(client, sectionKey);

		if (string.IsNullOrEmpty(PlexConfig.ShowName))
			return shows;

		return shows.Where(show => PlexConfig.ShowName.Equals(show.Title, StringComparison.OrdinalIgnoreCase));
	}

	private static string? EpisodeTitleFromMediaPath(Media? media)
	{
		if (media is null)
			return null;

		var filepath = media.Part.FirstOrDefault()?.File;
		if (string.IsNullOrEmpty(filepath))
			return null;

		var lastSlash = filepath.LastIndexOf('\\');
		if (lastSlash < 0)
			lastSlash = filepath.LastIndexOf('/');

		string filename;
		if (lastSlash >= 0)
			filename = filepath.Substring(lastSlash);
		else
			filename = filepath;

		return TitleFilenameParser.EpisodeTitle(filename);
	}

	private static async Task<string?> FindLibrarySectionKeyAsync(HttpClient client)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/sections");
		var librarySections = await JsonSerializer.DeserializeAsync<LibrarySections>(stream);

		Section? section = librarySections?.MediaContainer.Directory
			.FirstOrDefault(section => section.Title.Equals(PlexConfig.LibraryName, StringComparison.OrdinalIgnoreCase));

		if (section is null)
		{
			Console.WriteLine($"Could not find '{PlexConfig.LibraryName}' in the '{librarySections?.MediaContainer.Title}' Directory");
			return null;
		}

		if (!section.Agent.Equals(PlexConfig.RequiredAgent, StringComparison.OrdinalIgnoreCase))
		{
			// this is so we're only messing with metadata where an agent isn't also doing it unaware of us
			Console.WriteLine($"Found '{PlexConfig.LibraryName}', but it uses the '{section.Agent}' agent, and must use '{PlexConfig.RequiredAgent}'");
			return null;
		}

		return section.Key;
	}

	private static async Task<List<Show>> GetAllShowsWithUnwatchedEpisodesAsync(HttpClient client, string sectionKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/sections/{sectionKey}/unwatched");
		var showSections = await JsonSerializer.DeserializeAsync<ShowSections>(stream);

		var shows = showSections?.MediaContainer.Metadata;

		return shows ?? [];
	}

	private static async Task<List<Season>> GetAllSeasons(HttpClient client, string showKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/metadata/{showKey}/children?unwatched=1");
		var seasonSections = await JsonSerializer.DeserializeAsync<SeasonSections>(stream);

		var seasons = seasonSections?.MediaContainer.Metadata;

		return seasons ?? [];
	}

	private static async Task<List<Episode>> GetAllEpisodes(HttpClient client, string seasonKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/metadata/{seasonKey}/children?unwatched=1");
		var episodeSections = await JsonSerializer.DeserializeAsync<EpisodeSections>(stream);

		var episodes = episodeSections?.MediaContainer.Metadata;

		return episodes ?? [];
	}

	// todo: refactor to be more like UpdateEpisode so this can support more than just the title
	private static async Task<bool> UpdateSeason(HttpClient client, string sectionKey, string seasonKey, string seasonTitle)
	{
		const int magicTypeKey = 3; // I think this is an enum like { ???, Movie = 1, ???, TVSeason = 3, TVEpisode = 4 }
		var requestUrl = new StringBuilder($"{PlexConfig.ServerAddress}/library/sections/{sectionKey}/all?type={magicTypeKey}&id={seasonKey}&includeExternalMedia=1");
		requestUrl.Append($"&title.value={seasonTitle}");

		if (PlexConfig.DryRun)
			return false;

		var url = requestUrl.ToString();
		using var response = await client.PutAsync(url, content: null);
		return response.IsSuccessStatusCode;
	}

	private static async Task<bool> UpdateEpisode(HttpClient client, string sectionKey, string episodeKey, EpisodeUpdate values)
	{

		const int magicTypeKey = 4; // I think this is an enum like { ???, Movie = 1, ???, TVSeason = 3, Episode = 4 }
		var requestUrl = new StringBuilder($"{PlexConfig.ServerAddress}/library/sections/{sectionKey}/all?type={magicTypeKey}&id={episodeKey}&includeExternalMedia=1");

		var updateParams = values.GetQuerystringParams();
		if (string.IsNullOrEmpty(updateParams))
		{
			// nothing updated/set on EpisodeUpdate
			return true;
		}
		requestUrl.Append($"{updateParams}");

		if (PlexConfig.DryRun)
			return false;

		var url = requestUrl.ToString();
		using var response = await client.PutAsync(url, content: null);
		return response.IsSuccessStatusCode;
	}

}




