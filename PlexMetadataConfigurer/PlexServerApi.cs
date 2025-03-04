using PlexMetadataConfigurer.DTO;
using System.Text;
using System.Text.Json;

namespace PlexMetadataConfigurer;

/// <summary>
///		Handles sending requests to the Plex server
/// </summary>
public class PlexServerApi
{
	private readonly HttpClient client;

	internal PlexServerApi(HttpClient client)
	{
		this.client = client;
	}

	public async Task<string?> FindLibrarySectionKeyAsync(CancellationToken cancellation)
	{
		await using Stream stream = await client.GetStreamAsync($"{Config.PlexServerAddress}/library/sections", cancellation);
		var librarySections = await JsonSerializer.DeserializeAsync<LibrarySections>(stream, cancellationToken: cancellation);

		Section? section = librarySections?.MediaContainer.Directory
			.FirstOrDefault(section => section.Title.Equals(Config.LibraryName, StringComparison.OrdinalIgnoreCase));

		if (section is null)
		{
			Console.WriteLine($"Could not find library '{Config.LibraryName}' in the '{librarySections?.MediaContainer.Title}' directory");
			return null;
		}

		if (!section.Agent.Equals(Config.RequiredLibraryAgent, StringComparison.OrdinalIgnoreCase))
		{
			// this is so we're only messing with metadata where an agent isn't also doing it unaware of us
			Console.WriteLine($"Found library '{Config.LibraryName}', but it uses the '{section.Agent}' agent, and must use '{Config.RequiredLibraryAgent}'");
			return null;
		}

		return section.Key;
	}

	public async Task<List<Show>> GetAllShowsWithUnwatchedEpisodesAsync(string sectionKey, CancellationToken cancellation)
	{
		await using Stream stream = await client.GetStreamAsync($"{Config.PlexServerAddress}/library/sections/{sectionKey}/unwatched", cancellation);
		var showSections = await JsonSerializer.DeserializeAsync<ShowSections>(stream, cancellationToken: cancellation);

		var shows = showSections?.MediaContainer.Metadata;

		return shows ?? [];
	}

	public async Task<List<Season>> GetAllSeasonsAsync(string showKey, CancellationToken cancellation)
	{
		await using Stream stream = await client.GetStreamAsync($"{Config.PlexServerAddress}/library/metadata/{showKey}/children?unwatched=1", cancellation);
		var seasonSections = await JsonSerializer.DeserializeAsync<SeasonSections>(stream, cancellationToken: cancellation);

		var seasons = seasonSections?.MediaContainer.Metadata;

		return seasons ?? [];
	}

	public async Task<List<Episode>> GetAllEpisodesAsync(string seasonKey, CancellationToken cancellation)
	{
		await using Stream stream = await client.GetStreamAsync($"{Config.PlexServerAddress}/library/metadata/{seasonKey}/children?unwatched=1", cancellation);
		var episodeSections = await JsonSerializer.DeserializeAsync<EpisodeSections>(stream, cancellationToken: cancellation);

		var episodes = episodeSections?.MediaContainer.Metadata;

		return episodes ?? [];
	}

	// todo: refactor to be more like UpdateEpisode so this can support more than just the title
	public async Task<bool> UpdateSeasonAsync(string sectionKey, string seasonKey, string seasonTitle, CancellationToken cancellation)
	{
		const int magicTypeKey = 3; // I think this is an enum like { ???, Movie = 1, ???, TVSeason = 3, TVEpisode = 4 }
		var requestUrl = new StringBuilder($"{Config.PlexServerAddress}/library/sections/{sectionKey}/all?type={magicTypeKey}&id={seasonKey}&includeExternalMedia=1");
		requestUrl.Append($"&title.value={seasonTitle}");

		if (Config.DryRun)
			return false;

		var url = requestUrl.ToString();
		using var response = await client.PutAsync(url, content: null, cancellationToken: cancellation);
		return response.IsSuccessStatusCode;
	}

	public async Task<bool> UpdateEpisodeAsync(string sectionKey, string episodeKey, EpisodeUpdate values, CancellationToken cancellation)
	{
		const int magicTypeKey = 4; // I think this is an enum like { ???, Movie = 1, ???, TVSeason = 3, Episode = 4 }
		var requestUrl = new StringBuilder($"{Config.PlexServerAddress}/library/sections/{sectionKey}/all?type={magicTypeKey}&id={episodeKey}&includeExternalMedia=1");

		var updateParams = values.GetQuerystringParams();
		if (string.IsNullOrEmpty(updateParams))
		{
			// nothing updated/set on EpisodeUpdate
			return true;
		}
		requestUrl.Append($"{updateParams}");

		if (Config.DryRun)
			return false;

		var url = requestUrl.ToString();
		using var response = await client.PutAsync(url, content: null, cancellationToken: cancellation);
		return response.IsSuccessStatusCode;
	}

}
