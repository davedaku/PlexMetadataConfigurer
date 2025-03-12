# Plex Metadata Configurer

Updates season and episode information (title, summary, etc.) for **unwatched** episodes within a single TV library without an agent.

Connects to your Plex server's REST API, finds all unwatched episodes within the configured library (and optionally show), and updates supported metadata as configured in a `.plexmeta.json` file in the season directory (like the officially supported `.plexmatch` files). If no `.plexmeta.json` file is present, or it doesn't have a title for the season or any episode, that title will be derived from the directory/file name.

## Usage

### Prerequisites
  - **a Plex server** where you can edit content details
  - the server's **address and port** (e.g. `http://localhost:32400`, `http://denComputer:32400`)
  - **a TV library** there using the conventional Plex directory/file naming scheme, and **using the `tv.plex.agents.none` agent** (otherwise the agent and this app would both be trying to set the metadata, differently)
  - an **Auth Token** (see: https://support.plex.tv/articles/204059436-finding-an-authentication-token-x-plex-token/ ) (note: this is a temporary token, and a temporary auth solution)

### Download or Build
  - If you're code-savy and so inclined, pull this repo and build (`dotnet publish` from the repo root; remember to update `appsettings.json` in the output dir)
  - Otherwise:
    1. download the correct (Windows or Linux) artifact from the most recent `main` branch build in [the repo's Actions](https://github.com/davedaku/PlexMetadataConfigurer/actions) then:
    2. extract all the files somewhere
    3. edit `appsettings.json` (or use another method, see below) to configure for your server and library
    4. run the `PlexMetadataConfigurer` executable

### Configuration
See `Config.cs` in the project source for complete documentation.

| Property | Type | Notes |
| --- | --- | --- |
| ServerAddress | string | Required. The complete URI (protocol, host, port) of your Plex server (for its REST API) |
| AuthToken | string | Required. A Plex auth token for your server |
| Library | string | Required. The (case insensitive) name of a compatible library in that Plex server to configure metadata for |
| Show | string | Optional. The (case insensitive) name of a single show  within the library to configure. If unset, all shows within the library will be configured |
| DryRun | boolean | Optional. If true, everything will run except all API modification requests will be cancelled  (they will appear to have failed, any that are OK were checks that wouldn't have run an API modification anyway) |
| LibraryDirPrefix | string | Used with LocalDirPrefix when running this program from a host other than the Plex server, to read `.plexmeta.json` files from a different source than Plex is reading the episode files from |
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
For complete control over the supported metadata, place a JSON text file named `.plexmeta.json` in each season directory of your library, alongside the media files.

The file, and everything within it, are optional. Each episode is identified by `file` matching the local filename exactly. Seasons and episodes can each have a `title` and `summary` defined.

See `SeasonPlexMeta.cs` in the project source for complete documentation.

#### Example
```json
{
   "season": {
      "title": "Race 1 - Spain",
      "summary": "the World Championship event in Spain"
   },
   "episodes": [
      {
         "file": "s01e01.Qualifying.mp4",
         "title": "Qualifying"
      },
      {
         "file": "02.Race.ts",
         "title": "Vroom Vroom"
      },
      {
         "file": "s01e03.mp4",
         "title": "Post Analysis",
         "summary": "People talking about what happened"
      }
   ]
}
```

### Usage Example

My DVR recordings of Sport! events don't fit neatly into Plex's naming schemes, and there's no good agent available for retrieving metadata. 

My own solution is to, first, use: 
	- a `TV` Library with no agent (`tv.plex.agents.none`)
	- a Show *per year* of the SPORT
	- a Season per weekend/location
	- an Episode for each broadcast/event

If you follow normal Plex naming guidelines for TV content, this works pretty well organizationally, but there's no metadata so Plex just shows numbers and filenames.

> Previously, I would then use Plex's web UI to **manually set** any metadata I care about (and if, theoretically, something happens to my Plex library files on disk while rebuilding my server and I need to re-import my library, I would theoretically have to do it all over again).
> 
> Now, if I ever need to re-import my library, I only need to again run this program once to assign metadata for the library. 

To setup and use PlexMetadataConfigurer for my Plex server, I:
	1. went to the repo [Actions](https://github.com/davedaku/PlexMetadataConfigurer/actions) and then the most-recent run
	2. downloaded the `plexMetadataConfigurer_windows_x64` artifact (a `.zip` file)
	3. extracted this to `C:\Program Files\PlexMetadataConfigurer` on my server (could be anywhere)
	4. edited the `appsettings.json` file there (which requires admin elevation since it's in Program Files) to set my server address and auth token
	5. created a shortcut on my desktop to the `.exe` there, and set the Library name there as an argument (`"C:\Program Files\PlexMetadataConfigurer\PlexMetadataConfigurer.exe" --Library Sports`)

When I add new content to that library, I: 
	1. create/edit the local `.plexmeta.json` file
	2. run the shortcut.

Run the program directly from a command line if you need to keep the output visible.

## Attribution

Early on I found my way to [Plex custom season title script](https://web.archive.org/web/20230102221830/https://pastebin.com/qMVCp4Cv), a solution in Python for renaming your season titles based on part of the directory name, and [Python-PlexAPI](https://github.com/pkkid/python-plexapi) the package it uses to do the Plexy bits. They've both provided some insights and inspiration.

The (unofficial?) docs available at ["M-C"'s Postman workspace](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details) and [Plexopedia](https://www.plexopedia.com/plex-media-server/api/library/details/) have been very helpful. Special shout-out to this [rare PUT documentation](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details)

And special thanks/curses to LukeHagar's [plex-api-spec](https://github.com/LukeHagar/plex-api-spec) and its [plexcsharp](https://github.com/LukeHagar/plexcsharp). His projects have been immensely helpful in writing my own solution that doesn't use them.

## Short Term Objectives

## Long Term Backlog
Grouped by prioritized category.

1. Bugs
2. Important Features
	- switch from user token authentication to https://forums.plex.tv/t/authenticating-with-plex/609370
	- support configuration through environment variables
3. Cleanup & Refinement
	- (in addition to current `.plexmeta.json`) support `.plexmeta` with serialization like `.plexmatch`
	- ability to specify thumbnail (and background?) images in .plexmeta , and update them through the API
	- support any other reasonable metadata in `.plexmeta(.json)`
4. Unsortables
