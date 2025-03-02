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
	internal const string ShowName = "";

	/// <summary>
	///		The library must be configured with this agent to be configurered
	/// </summary>
	internal const string RequiredLibraryAgent = "tv.plex.agents.none";

	/// <summary>
	///		If true: everything will run except all API modification requests will be cancelled 
	///		(they will appear to have failed, any that are OK were checks that wouldn't have run
	///		an API modification anyway)
	/// </summary>
	internal const bool DryRun = false;

	/// <summary>
	///		If true: will (attempt to) make the API modification even if not changing the value 
	///		(e.g. the title already is the parsed & proposed value)
	/// </summary>
	internal const bool AlwaysModify = false;
}
