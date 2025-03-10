# Plex Metadata Configurer

Updates season and episode information (title, summary, etc.) for **unwatched** episodes within a single TV library without an agent.

Connects to your Plex server's REST API, finds all unwatched episodes within the configured library (and optionally show), and updates supported metadata as configured in a `.plexmeta` file in the season directory (like the officially supported `.plexmatch` files). If no `.plexmeta` file is present, or it doesn't have a title for the season or any episode, that title will be derived from the directory/file name.

## Usage

### Prerequisites
  - **a Plex server** where you can edit content details
  - the server's **address and port** (e.g. `http://localhost:32400`, `http://denComputer:32400`)
  - **a TV library** there using the conventional Plex directory/file naming scheme, and **using the `tv.plex.agents.none` agent** (otherwise the agent and this app would both be trying to set the metadata, differently)
  - an **Auth Token** (see: https://support.plex.tv/articles/204059436-finding-an-authentication-token-x-plex-token/ ) (note: this is a temporary token, and a temporary auth solution)

### Download or Build
  - If you're code-savy and so inclined, pull this repo and build (`dotnet publish` from the repo root; remember to update `appsettings.json` in the output dir)
  - Otherwise download the latest artifact from (todo) then:
   1. extract all the files somewhere
   2. edit `appsettings.json` (or use another method, see below) to configure for your server and library
   3. run the `PlexMetadataConfigurer` executable

### Configuration
See `Config.cs` in the project source for complete documentation.

| Property | Type | Notes |
| --- | --- | --- |
| ServerAddress | string | Required. The complete URI (protocol, host, port) of your Plex server (for its REST API) |
| AuthToken | string | Required. A Plex auth token for your server |
| Library | string | Required. The (case insensitive) name of a compatible library in that Plex server to configure metadata for |
| Show | string | Optional. The (case insensitive) name of a single show  within the library to configure. If unset, all shows within the library will be configured |
| DryRun | boolean | Optional. If true, everything will run except all API modification requests will be cancelled  (they will appear to have failed, any that are OK were checks that wouldn't have run an API modification anyway) |
| LibraryDirPrefix | string | Used with LocalDirPrefix when running this program from a host other than the Plex server, to read `.plexmeta` files from a different source than Plex is reading the episode files from |
| LocalDirPrefix | string | Used with LibraryDirPrefix. |

#### Configuring with `appsettings.json`
File should be in the same directory as the executable

```json
{
	"ServerAddress": "http://localhost:32400",
	"AuthToken": "yOURpLEXtOK3N",
	"Library": "YourLibraryName"
}
```

#### Configuring with Command-Line Arguments
Takes precedence over `appsettings.json` values

```
> .\PlexMetadataConfigurer.exe --ServerAddress="http://localhost:32400" --AuthToken="yOURpLEXtOK3N"
```

### .plexmeta
For complete control over the supported metadata, place a JSON text file named `.plexmeta` (no extension) in each season directory of your library, alongside the media files (technically these *can* be configured to be elsewhere).

See `SeasonPlexMeta.cs` in the project source for complete documentation.

#### Example
```json
{
	"season": {
		"title": "Race 1 - Spain",
		"summary": "the World Championship event in Spain",		
	},
	"episodes": [
		{
			"file": "s01e01.Qualifying.mp4",
			"title": "Vroom Qualifying"
		},
		{
			"file": "02.Race.ts",
			"title": "Vroom Vroom Race"
		},
		{
			"file": "s01e03.mp4",
			"title": "Post Race Analysis"
		},	
	]
}
```

## Background

### the Problem

Is this you?

> My DVR recordings of Sports! events don't fit neatly into Plex's naming schemes, but I've found a way to make it work by creating a new "show" for each year, and then a new "season" for each weekend/event, then each "episode" is the different events throughout that weekend! Which works! ..But I have to go into the Plex web UI and rename each season and episode individually, because your Plex library either has no agent (`tv.plex.agents.none`) or the wrong agent. ...And then, that time I lost my library (not the actual media files, just the Plex metadata!), and... Yeah, I had to re-do it all...

As rambled: I have a pretty specific show/season/episode scheme that's working well for me, but getting the metadata for it set in Plex is a bit of a PITA. 

There's the [.plexmatch](https://support.plex.tv/articles/plexmatch/) solution for placing a configuration file alongside your content, in each show's directory, but that only helps the agent identify it and then populate whatever metadata the agent has.

### History & Attribution

Early on I found my way to [Plex custom season title script](https://web.archive.org/web/20230102221830/https://pastebin.com/qMVCp4Cv), a solution in Python for renaming your season titles based on part of the directory name, and [Python-PlexAPI](https://github.com/pkkid/python-plexapi) the package it uses to do the Plexy bits. They've both provided some insights and inspiration.

The (unofficial?) docs available at ["M-C"'s Postman workspace](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details) and [Plexopedia](https://www.plexopedia.com/plex-media-server/api/library/details/) have been very helpful. Special shout-out to this [rare PUT documentation](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details)

And special thanks/curses to LukeHagar's [plex-api-spec](https://github.com/LukeHagar/plex-api-spec) and its (autogenerated?) [plexcsharp](https://github.com/LukeHagar/plexcsharp). His projects have been immensely helpful in writing my own solution that doesn't use it. I wasn't able to wrap my head around the `plexcsharp` API, and turned to just trying out the REST requests it documents. This worked well enough for my needs to justify dropping the complexity this package, and implementing my own API interaction. 

The project scope grows.

## Current State

1. Connects to your Plex server's REST API, using an auth token
	- these tokens are temporary, and will expire after an unspecified time
2. Navigates your Plex server to find the configured library, then finds all **unwatched** episodes and the seasons and shows they're in
3. Looks for a `.plexmeta` file in the season directory, and loads intended metadata from it for the season and each episode.
	- if this file doesn't exist, or doesn't contain a Title for either the season or any episodes, a Title value will attempt to be parsed from the episode filename
4. Uses your Plex server's REST API to update the title of seasons and episodes

## Short Term Objectives
- setup a basic build pipeline in github

## Long Term Backlog
Grouped by prioritized category.

1. Bugs
2. Important Features
	- switch from user token authentication to https://forums.plex.tv/t/authenticating-with-plex/609370
	- support configuration through environment variables
	- ability to specify thumbnail (and background?) images in config files, and update them through the API
3. Research
4. Cleanup & Refinement
	- support `.plexmeta` or `.plexmeta.json` filename
5. Unsortables
