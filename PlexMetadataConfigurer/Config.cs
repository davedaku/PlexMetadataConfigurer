namespace PlexMetadataConfigurer;

/// <summary>
///		Configurer configuration, deserialized from optional `appsettings.json` file(s)
/// </summary>
public class Config
{
	/// <summary>
	///		If true: everything will run except all API modification requests will be cancelled 
	///		(they will appear to have failed, any that are OK were checks that wouldn't have run
	///		an API modification anyway)
	/// </summary>
	public bool DryRun { get; set; } = false;

	/// <summary>
	///		HTTP(s) address for your Plex server, including port (e.g. "http://localhost:32400")
	/// </summary>
	public string ServerAddress { get; set; } = string.Empty;

	/// <summary>
	///		A valid auth token for your Plex server
	/// </summary>
	public string AuthToken { get; set; } = string.Empty;

	/// <summary>
	///		Will only configurer the library with this name within the Plex server
	/// </summary>
	public string Library { get; set; } = string.Empty;

	/// <summary>
	///		If not empty: will only configurer the show with this name within the library
	/// </summary>
	public string Show { get; set; } = string.Empty;

	/// <summary>
	///		If not empty: This prefix to a directory path will be replaced with `LibraryConfigFilePrefixReplacement`
	///		when looking for the `ConfigurationFilename`
	/// </summary>
	/// <remarks>
	///		This allows you to run this remotely, with the Plex library available through some network share, or to
	///		keep the configuration files separate from your Plex content (but in mirrored directories after this prefix)
	///		
	///		When running this on the Plex server, leave both empty to disable
	/// </remarks>
	public string LibraryDirPrefix { get; set; } = string.Empty; // where the Plex library files are, from the Plex server's perspective
	public string LocalDirPrefix { get; set; } = string.Empty; // where the .plexmeta files are, from the current host's perspective

	/// <summary>
	///		Name of the file (including extension) that will contain show/season metadata configuration
	/// </summary>
	public const string ConfigurationFilename = ".plexmeta";

	/// <summary>
	///		The library must be configured with this agent to be configurered
	/// </summary>
	public const string RequiredLibraryAgent = "tv.plex.agents.none";
}
