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
///		Short-term use-case is just MotoGP: update existing seasons/episodes, then I'll mark
///		them all Watched, and then re-run this after adding new files
///		
///		Long-term this should try to read from a file in each directory 
///		(`.episodes.json` (a serialized `List<Episode>`) inside a `Season ##` dir)
///		rather than be opinionated about how to parse the video file names (or even what the actual Media is)
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

		string? sectionKey = await FindLibrarySectionKeyAsync(client);
		if (string.IsNullOrEmpty(sectionKey))
			return;

		var shows = (await GetAllShowsWithUnwatchedEpisodesAsync(client, sectionKey))
			.Where(s => string.IsNullOrEmpty(PlexConfig.ShowName) || PlexConfig.ShowName.Equals(s.Title, StringComparison.OrdinalIgnoreCase));
		foreach (var show in shows)
		{
			var seasons = await GetAllSeasons(client, show.Key);

			foreach (var season in seasons)
			{
				var episodes = await GetAllEpisodes(client, season.Key);

				foreach (var episode in episodes)
				{
					Media? media = episode.Media?.FirstOrDefault();
					var updatedValues = new EpisodeUpdate();

					Console.WriteLine($"{episode.Key} \t{media?.Part.FirstOrDefault()?.File}");

					var titleFromMedia = EpisodeTitleFromMediaPath(media);
					if (!string.IsNullOrEmpty(titleFromMedia) && !titleFromMedia.Equals(episode.Title))
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

	static string? EpisodeTitleFromMediaPath(Media? media)
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

		return Utility.EpisodeTitle(filename);
	}

	static async Task<string?> FindLibrarySectionKeyAsync(HttpClient client)
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

	static async Task<List<Show>> GetAllShowsWithUnwatchedEpisodesAsync(HttpClient client, string sectionKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/sections/{sectionKey}/unwatched");
		var showSections = await JsonSerializer.DeserializeAsync<ShowSections>(stream);

		var shows = showSections?.MediaContainer.Metadata;

		return shows ?? [];
	}

	static async Task<List<Season>> GetAllSeasons(HttpClient client, string showKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/metadata/{showKey}/children?unwatched=1");
		var seasonSections = await JsonSerializer.DeserializeAsync<SeasonSections>(stream);

		var seasons = seasonSections?.MediaContainer.Metadata;

		return seasons ?? [];
	}

	static async Task<List<Episode>> GetAllEpisodes(HttpClient client, string seasonKey)
	{
		await using Stream stream = await client.GetStreamAsync($"{PlexConfig.ServerAddress}/library/metadata/{seasonKey}/children?unwatched=1");
		var episodeSections = await JsonSerializer.DeserializeAsync<EpisodeSections>(stream);

		var episodes = episodeSections?.MediaContainer.Metadata;

		return episodes ?? [];
	}

	static async Task<bool> UpdateEpisode(HttpClient client, string sectionKey, string episodeKey, EpisodeUpdate values)
	{
		const int magicTypeKey = 4; // I think this is an enum like { ???, Movie = 1, ???, ???, Episode = 4 }
		var requestUrl = new StringBuilder($"{PlexConfig.ServerAddress}/library/sections/{sectionKey}/all?type={magicTypeKey}&id={episodeKey}&includeExternalMedia=1");

		var updateParams = values.GetQuerystringParams();
		if (string.IsNullOrEmpty(updateParams))
		{
			// nothing updated/set on EpisodeUpdate
			return true;
		}
		Console.WriteLine($"\tUpdateParam = '{updateParams}'");
		requestUrl.Append($"{updateParams}");

		var url = requestUrl.ToString();

		using var response = await client.PutAsync(url, content: null);
		return response.IsSuccessStatusCode;
	}

}




