using PlexMetadataConfigurer.DTO;
using PlexMetadataConfigurer.PlexMeta;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer;

/// <summary>
///		Configures the metadata for a Plex library with no metadata agent
/// </summary>
/// <remarks>
///		Uses your Plex server's REST API to iterate through all unplayed episodes within
///		a TV library with no metadata agent.
///		
///		Looks for a `.plexmeta` configuration file in each directory to specify the
///		metadata for each season and episode. Falls back to parsing the episode filenames 
///		for better titles.
///		
///		Uses the API again to update the metadata within your Plex library.
/// </remarks>
internal class Program
{
	private static readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions
	{
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		IndentCharacter = '\t',
		IndentSize = 1,
		MaxDepth = 16,
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

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

		// a Plex sectionKey identifies a library
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
				var seasonConfig = await GetSeasonMetaFileAsync(episodes.FirstOrDefault(), cancelToken);

				await UpdateSeasonMetadataAsync(plexServer, sectionKey, season, episodes, seasonConfig, cancelToken);

				foreach (var episode in episodes)
					await UpdateEpisodeMetadataAsync(plexServer, sectionKey, episode, seasonConfig, cancelToken);
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

	private static async Task<SeasonPlexMeta?> GetSeasonMetaFileAsync(Episode? firstEpisode, CancellationToken cancellation)
	{
		var firstFilename = firstEpisode?.Media.FirstOrDefault()?.Part.FirstOrDefault()?.File;
		if (string.IsNullOrWhiteSpace(firstFilename))
			return null;

		var dirSeparator = firstFilename.Contains('\\') ? '\\' : '/';

		var lastSlash = firstFilename.LastIndexOf(dirSeparator);
		var dirPath = firstFilename.Substring(0, lastSlash);
		var metaFilePath = $"{dirPath}{dirSeparator}{Config.ConfigurationFilename}";
		if (!string.IsNullOrWhiteSpace(Config.PlexLibraryDirPrefix) && metaFilePath.StartsWith(Config.PlexLibraryDirPrefix))
			metaFilePath = $"{Config.LocalDirPrefixReplacement}{metaFilePath.Substring(Config.PlexLibraryDirPrefix.Length)}";

		Console.WriteLine($"Looking for '{metaFilePath}'...");

		try
		{
			using var reader = new StreamReader(metaFilePath);
			var config = await JsonSerializer.DeserializeAsync<SeasonPlexMeta>(reader.BaseStream, jsonOpts, cancellation);
			Console.WriteLine($"\tLoaded");

			return config;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\t{ex.Message}");
			return null;
		}
	}

	private static async Task UpdateSeasonMetadataAsync(PlexServerApi plexServer, string sectionKey, Season season, List<Episode> episodes, SeasonPlexMeta? seasonConfig, CancellationToken cancelToken)
	{
		Console.WriteLine(new string('-', 60));
		Console.WriteLine($"'{season.Title}' ({season.Key}) has {episodes.Count} unwatched episodes");

		var updatedValues = new SeasonUpdate
		{
			Summary = seasonConfig?.Season?.Summary,
			Title = seasonConfig?.Season?.Title
		};

		if (string.IsNullOrWhiteSpace(updatedValues.Title))
		{
			var firstFilename = episodes.FirstOrDefault()?.Media.FirstOrDefault()?.Part.FirstOrDefault()?.File;
			updatedValues.Title = TitleFilenameParser.SeasonTitle(firstFilename);
		}

		if (updatedValues.Unchanged(season))
		{
			Console.WriteLine("\tSeason unchanged");
		}
		else
		{
			var success = await plexServer.UpdateSeasonAsync(sectionKey, season.Key, updatedValues, cancelToken);
			if (success)
				Console.WriteLine("\tSeason updated successfully");
			else
				Console.WriteLine("\tSeason update failed");
		}

		Console.WriteLine();
	}

	private static async Task UpdateEpisodeMetadataAsync(PlexServerApi plexServer, string sectionKey, Episode episode, SeasonPlexMeta? seasonConfig, CancellationToken cancelToken)
	{
		Media? media = episode.Media?.FirstOrDefault();

		if (media is null)
		{
			Console.WriteLine($"{episode.Key} \t (No episode media found!)");
			return;
		}

		Console.WriteLine($"{episode.Key} \t{media.Part.FirstOrDefault()?.File}");

		var episodeConfig = episode.Media is null ? null : seasonConfig?.Episodes?
			.FirstOrDefault(ec => !string.IsNullOrWhiteSpace(ec.File) && episode.Media.Any(m => m.Part.Any(part => part.File.EndsWith(ec.File))));

		var updatedValues = new EpisodeUpdate
		{
			Title = episodeConfig?.Title,
			Summary = episodeConfig?.Summary
		};

		if (string.IsNullOrWhiteSpace(updatedValues.Title))
			updatedValues.Title = ParseFilenameForEpisodeTitle(media);

		if (updatedValues.Unchanged(episode))
		{
			Console.WriteLine("\tUnchanged");
		}
		else
		{
			var success = await plexServer.UpdateEpisodeAsync(sectionKey, episode.Key, updatedValues, cancelToken);
			if (success)
				Console.WriteLine("\tUpdated successfully");
			else
				Console.WriteLine("\tUpdate failed");
		}

		Console.WriteLine();
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
