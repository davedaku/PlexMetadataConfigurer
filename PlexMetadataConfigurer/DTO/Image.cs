using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

public record class Image(
	[property: JsonPropertyName("alt")] string Alt,
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("url")] string Url
	);