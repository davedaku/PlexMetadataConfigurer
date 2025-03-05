namespace PlexMetadataConfigurer;

// todo: move to a file, ENV var, or something

internal static class Config
{
	/// <summary>
	///		A valid auth token for your Plex server
	/// </summary>
	internal const string PlexServerAuthToken = "sYiJKvt2bU9kKrfLdc84";

	/// <summary>
	///		HTTP(s) address for your Plex server, including port (e.g. "http://localhost:32400")
	/// </summary>
	internal const string PlexServerAddress = "http://ghostwheel:32400";

	/// <summary>
	///		Will only configurer the library with this name within the Plex server
	/// </summary>
	internal const string LibraryName = "Sports";

	/// <summary>
	///		If not empty: will only configurer the "show" with this name within the library
	/// </summary>
	internal const string ShowName = "World Superbike 2022";

	/// <summary>
	///		The library must be configured with this agent to be configurered
	/// </summary>
	internal const string RequiredLibraryAgent = "tv.plex.agents.none";

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
	internal const string PlexLibraryDirPrefix = "A:"; // where the Plex library files are, from the Plex server's perspective
	internal const string LocalDirPrefixReplacement = "R:"; // where the .plexmeta files are, from the current host's perspective

	/// <summary>
	///		Name of the file (including extension) that will contain show/season metadata configuration
	/// </summary>
	internal const string ConfigurationFilename = ".plexmeta";

	/// <summary>
	///		If true: everything will run except all API modification requests will be cancelled 
	///		(they will appear to have failed, any that are OK were checks that wouldn't have run
	///		an API modification anyway)
	/// </summary>
	internal const bool DryRun = false;
}
