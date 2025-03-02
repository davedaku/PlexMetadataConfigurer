using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.PlexMeta;

/// <summary>
///		Model for the `.plexmeta` files within a season directory (adjacent to the episode files)
/// </summary>
public record class SeasonPlexMeta(
	[property: JsonPropertyName("season")] SasonPlexMetadata? Season,
	[property: JsonPropertyName("episodes")] List<SeasonPlexMetaEpisode>? Episodes
	);

public record class SasonPlexMetadata(
	[property: JsonPropertyName("title")] string? Title,
	[property: JsonPropertyName("summary")] string? Summary
	);

/// <summary>
///		Configuration for a single episode within a season
/// </summary>
/// <param name="File">
///		Filename used to identify the episode (todo: also support this being null and another property, 
///		like `number` (or just position in array) being used. This filename specification is needed when
///		filename parsing would fail anyway, but if filenames are like `s##e## - Title.ext` then this could
///		just be an ordered array without identifiers)
/// </param>
public record class SeasonPlexMetaEpisode(
	[property: JsonPropertyName("file")] string? File,
	[property: JsonPropertyName("title")] string? Title,
	[property: JsonPropertyName("summary")] string? Summary
	);
