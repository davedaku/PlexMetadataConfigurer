using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

public record class Episode(
	[property: JsonPropertyName("ratingKey")] string Key,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("summary")] string Summary,
	[property: JsonPropertyName("year")] int Year,
	[property: JsonPropertyName("thumb")] string Thumbnail,
	[property: JsonPropertyName("art")] string Art,
	[property: JsonPropertyName("duration")] long Duration,
	List<Media> Media,
	List<Image> Image
	);

/// <summary>
///		A `GET  /library/metadata/{key}/children?unwatched=1` response model, contains one object
/// </summary>
public record class EpisodeSections(EpisodesMediaContainer MediaContainer);

public record class EpisodesMediaContainer(
	[property: JsonPropertyName("key")] string Key,
	[property: JsonPropertyName("art")] string Art,
	List<Episode> Metadata
	);