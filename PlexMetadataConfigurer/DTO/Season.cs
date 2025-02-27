using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

public record class Season(
	[property: JsonPropertyName("ratingKey")] string Key,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("summary")] string Summary,
	[property: JsonPropertyName("art")] string Art,
	List<Image> Image
	);

/// <summary>
///		A `GET  /library/metadata/{key}/children?unwatched=1` response model, contains one object
/// </summary>
public record class SeasonSections(SeasonsMediaContainer MediaContainer);

public record class SeasonsMediaContainer(
	[property: JsonPropertyName("art")] string Art,
	[property: JsonPropertyName("key")] string Key,
	[property: JsonPropertyName("parentTitle")] string Title,
	List<Season> Metadata
	);
