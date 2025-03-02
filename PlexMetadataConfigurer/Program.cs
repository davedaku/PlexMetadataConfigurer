using PlexMetadataConfigurer.DTO;
using System.Net.Http.Headers;
using System.Net.Mime;

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
		var cancelToken = new CancellationTokenSource().Token; // todo: replace

		using HttpClient client = new();
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
		client.DefaultRequestHeaders.Add("User-Agent", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Product", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Token", Config.PlexServerAuthToken);

		var plexServer = new PlexServerApi(client);

		// a Plex sectionKey identifies a Library
		string? sectionKey = await plexServer.FindLibrarySectionKeyAsync(cancelToken);
		if (string.IsNullOrEmpty(sectionKey))
			return;

		var shows = await GetFilteredShowsAsync(plexServer, sectionKey, cancelToken);
		foreach (var show in shows)
		{
			var seasons = await plexServer.GetAllSeasonsAsync(show.Key, cancelToken);

			foreach (var season in seasons)
			{
				var episodes = await plexServer.GetAllEpisodesAsync(season.Key, cancelToken);

				Console.WriteLine($"'{season.Title}' ({season.Key}) has {episodes.Count} unwatched episodes");
				// get the season dir name (need to use an episode's media path to do this)
				var firstFilename = episodes.FirstOrDefault()?.Media.FirstOrDefault()?.Part.FirstOrDefault()?.File;
				var newSeasonTitle = TitleFilenameParser.SeasonTitle(firstFilename);

				// if parsed, send PUT to update season title
				if (!string.IsNullOrWhiteSpace(newSeasonTitle) && (Config.AlwaysModify || !newSeasonTitle.Equals(season.Title)))
				{
					Console.WriteLine($"\tTitle: '{season.Title}' => '{newSeasonTitle}'");

					var success = await plexServer.UpdateSeasonAsync(sectionKey, season.Key, newSeasonTitle, cancelToken);
					if (success)
						Console.WriteLine($"\tOK");
					else
						Console.WriteLine($"\tSeason update failed.");
				}
				Console.WriteLine();

				foreach (var episode in episodes)
				{
					Media? media = episode.Media?.FirstOrDefault();
					var updatedValues = new EpisodeUpdate();

					Console.WriteLine($"{episode.Key} \t{media?.Part.FirstOrDefault()?.File}");

					var titleFromMedia = ParseFilenameForEpisodeTitle(media);
					if (!string.IsNullOrEmpty(titleFromMedia) && (Config.AlwaysModify || !titleFromMedia.Equals(episode.Title)))
					{
						Console.WriteLine($"\tTitle: '{episode.Title}' => '{titleFromMedia}'");
						updatedValues.Title = titleFromMedia;
					}

					// todo: other properties

					var success = await plexServer.UpdateEpisodeAsync(sectionKey, episode.Key, updatedValues, cancelToken);
					if (success)
						Console.WriteLine($"\tOK");
					else
						Console.WriteLine($"\tEpisode update failed.");

					Console.WriteLine();
				}

			}
		}
	}

	/// <summary>
	///		Query the API for shows with unwatched episodes, then apply any configured filtering
	/// </summary>
	private static async Task<IEnumerable<Show>> GetFilteredShowsAsync(PlexServerApi plexServer, string sectionKey, CancellationToken cancellation)
	{
		var shows = await plexServer.GetAllShowsWithUnwatchedEpisodesAsync(sectionKey, cancellation);

		if (string.IsNullOrEmpty(Config.ShowName))
			return shows;

		return shows.Where(show => Config.ShowName.Equals(show.Title, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	///		Parse a media filepath to just the filename, which will then be parsed further
	/// </summary>
	private static string? ParseFilenameForEpisodeTitle(Media? media)
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

}
