using System.Text.Json.Serialization;

namespace PlexMetadataConfigurer.DTO;

/// <summary>
///		A 'section' within a PLEX library, but a "library" within the UI (e.g. 'Movies', 'Shows')
/// </summary>
public record class Section(
	[property: JsonPropertyName("key")] string Key,
	[property: JsonPropertyName("title")] string Title,
	[property: JsonPropertyName("agent")] string Agent
);

/// <summary>
///		The `GET  /library/sections` response model, contains one object
/// </summary>
public record class LibrarySections(LibrarySectionMediaContainer MediaContainer);

/// <summary>
///		Part of the `GET  /library/sections` response, the `MediaContainer`, contains the actual library sections
/// </summary>
public record class LibrarySectionMediaContainer(
	[property: JsonPropertyName("title1")] string Title,
	List<Section> Directory
	);

