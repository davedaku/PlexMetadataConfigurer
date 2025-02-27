using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

public record class Show(
	[property: JsonPropertyName("ratingKey")] string Key,
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("summary")] string Summary
	);

/// <summary>
///		The `GET  /library/sections/{key}/unwatched` response model, contains one object
/// </summary>
public record class ShowSections(UnwatchedShowsMediaContainer MediaContainer);

public record class UnwatchedShowsMediaContainer(
	[property: JsonPropertyName("title")] string Title,
	List<Show> Metadata
	);