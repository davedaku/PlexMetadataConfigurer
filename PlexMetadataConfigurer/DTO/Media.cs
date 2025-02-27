using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

public record class Media(

	[property: JsonPropertyName("id")] long Id,
	[property: JsonPropertyName("duration")] long Duration,
	[property: JsonPropertyName("height")] int Height,
	[property: JsonPropertyName("container")] string Container,
	List<MediaPart> Part
	);

public record class MediaPart(

	[property: JsonPropertyName("id")] long Id,
	[property: JsonPropertyName("key")] string Key,
	[property: JsonPropertyName("duration")] long Duration,
	[property: JsonPropertyName("container")] string Container,
	[property: JsonPropertyName("file")] string File
	);
