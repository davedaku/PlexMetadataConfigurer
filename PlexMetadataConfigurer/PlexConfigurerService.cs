using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
///		metadata for each season and episode. If needed, falls back to parsing the episode filenames 
///		for titles.
///		
///		Uses the API again to update the metadata within your Plex library.
/// </remarks>
internal class PlexConfigurerService : IHostedService
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

	private readonly Config? config;
	private readonly IHostApplicationLifetime hostLifetime;
	private readonly ILogger<PlexConfigurerService> logger;

	public PlexConfigurerService(IConfiguration configuration, IHostApplicationLifetime hostLifetime, ILogger<PlexConfigurerService> logger)
	{
		config = configuration.Get<Config>();
		this.hostLifetime = hostLifetime;
		this.logger = logger;
	}

	public async Task StartAsync(CancellationToken cancelToken)
	{
		if (!ConfigurationValid())
		{
			hostLifetime.StopApplication();
			return;
		}

		using HttpClient client = new();
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
		client.DefaultRequestHeaders.Add("User-Agent", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Product", "PlexMetadataConfigurer");
		client.DefaultRequestHeaders.Add("X-Plex-Token", config!.AuthToken);

		var plexServer = new PlexServerApi(config, client);

		// a Plex sectionKey identifies a library
		string? sectionKey = await plexServer.FindLibrarySectionKeyAsync(cancelToken);
		if (string.IsNullOrEmpty(sectionKey))
			return;

		var shows = await GetFilteredShowsAsync(plexServer, config, sectionKey, cancelToken);
		foreach (var show in shows)
		{
			var seasons = await plexServer.GetAllSeasonsAsync(show.Key, cancelToken);
			foreach (var season in seasons)
			{
				Console.WriteLine(new string('-', 60));
				var episodes = await plexServer.GetAllEpisodesAsync(season.Key, cancelToken);
				var seasonConfig = await GetSeasonMetaFileAsync(config, episodes.FirstOrDefault(), cancelToken);

				await UpdateSeasonMetadataAsync(plexServer, sectionKey, season, episodes, seasonConfig, cancelToken);

				foreach (var episode in episodes)
					await UpdateEpisodeMetadataAsync(plexServer, sectionKey, episode, seasonConfig, cancelToken);
			}
		}

		hostLifetime.StopApplication();
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		logger.LogDebug("Stopping");
		return Task.CompletedTask;
	}

	/// <summary>
	///		Ensures the configuration object is valid and contains all required properties. After this
	///		method is invoked, `config` can no longer be `null`
	/// </summary>
	private bool ConfigurationValid()
	{
		string? configError = null;
		if (config != null)
		{
			if (string.IsNullOrWhiteSpace(config.ServerAddress))
				configError = $"{nameof(config.ServerAddress)} must be configured";

			else if (string.IsNullOrWhiteSpace(config.AuthToken))
				configError = $"{nameof(config.AuthToken)} must be configured";
		}
		if (config is null || configError != null)
		{
			logger.LogCritical("{error}", configError ?? "Cannot locate configuration.");
			return false;
		}

		if (config.DryRun)
			logger.LogInformation("{configOption} enabled: any attempted modification requests should fail immediately", nameof(config.DryRun));

		return true;
	}

	/// <summary>
	///		Query the API for shows with unwatched episodes, then apply any configured filtering
	/// </summary>
	private static async Task<IEnumerable<Show>> GetFilteredShowsAsync(PlexServerApi plexServer, Config config, string sectionKey, CancellationToken cancellation)
	{
		var shows = await plexServer.GetAllShowsWithUnwatchedEpisodesAsync(sectionKey, cancellation);

		if (string.IsNullOrEmpty(config.Show))
			return shows;

		return shows.Where(show => config.Show.Equals(show.Title, StringComparison.OrdinalIgnoreCase));
	}

	private static async Task<SeasonPlexMeta?> GetSeasonMetaFileAsync(Config config, Episode? firstEpisode, CancellationToken cancellation)
	{
		var firstFilename = firstEpisode?.Media.FirstOrDefault()?.Part.FirstOrDefault()?.File;
		if (string.IsNullOrWhiteSpace(firstFilename))
			return null;

		var dirSeparator = firstFilename.Contains('\\') ? '\\' : '/';

		var lastSlash = firstFilename.LastIndexOf(dirSeparator);
		var dirPath = firstFilename.Substring(0, lastSlash);
		var metaFilePath = $"{dirPath}{dirSeparator}{Config.ConfigurationFilename}";
		if (!string.IsNullOrWhiteSpace(config.LibraryDirPrefix) && metaFilePath.StartsWith(config.LibraryDirPrefix))
			metaFilePath = $"{config.LocalDirPrefix}{metaFilePath.Substring(config.LibraryDirPrefix.Length)}";

		Console.WriteLine($"Looking for '{metaFilePath}'...");

		try
		{
			using var reader = new StreamReader(metaFilePath);
			var plexmeta = await JsonSerializer.DeserializeAsync<SeasonPlexMeta>(reader.BaseStream, jsonOpts, cancellation);
			Console.WriteLine($"\tLoaded");

			return plexmeta;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\t{ex.Message}");
			return null;
		}
	}

	private static async Task UpdateSeasonMetadataAsync(PlexServerApi plexServer, string sectionKey, Season season, List<Episode> episodes, SeasonPlexMeta? seasonConfig, CancellationToken cancelToken)
	{
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
			updatedValues.Title = TitleFilenameParser.EpisodeTitle(media);

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


}
