# Plex Metadata Configurer

Update season and episode information (title, description, thumbnail, etc) for **unwatched** episodes within a single library.

The library must be configured to use the `tv.plex.agents.none` Agent, otherwise the agent and this app would both be trying to set the metadata (differently).

## Problem

Is this you?

> My DVR recordings of Sports! events don't fit neatly into Plex's naming schemes, but I've found a way to make it work by creating a new "show" for each year, and then a new "season" for each weekend/event, then each "episode" is the different events throughout that weekend! Which works! ..But I have to go into the Plex web UI and rename each season and episode individually, because your Plex library either has no agent (`tv.plex.agents.none`) or the wrong agent. ...And then, that time I lost my library (not the actual media files, just the Plex metadata!), and... Yeah, I had to re-do it all...

As rambled: I have a pretty specific show/season/episode scheme that's working well for me, but getting the metadata for it set in Plex is a bit of a PITA. 

There's the [.plexmatch](https://support.plex.tv/articles/plexmatch/) solution for placing a configuration file alongside your content, in each show's directory, but that only helps the agent identify it and then populate whatever metadata it has.

I probably should have looked into how to build a custom agent. But instead: THIS!

## Background & Attribution
Early on I found my way to [Plex custom season title script](https://web.archive.org/web/20230102221830/https://pastebin.com/qMVCp4Cv), a solution in Python for renaming your season titles based on part of the directory name, and [Python-PlexAPI](https://github.com/pkkid/python-plexapi) the package it uses to do the Plexy bits. They've both provided some insights and inspiration.

The (unofficial?) docs available at ["M-C"'s Postman workspace](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details) and [Plexopedia](https://www.plexopedia.com/plex-media-server/api/library/details/) have been very helpful. Special shout-out to [rare PUT docs](https://www.postman.com/fyvekatz/m-c-s-public-workspace/request/6gfy9hu/update-movie-details)

And special thanks/curses to LukeHagar's [plex-api-spec](https://github.com/LukeHagar/plex-api-spec) and its (autogenerated?) [plexcsharp](https://github.com/LukeHagar/plexcsharp), which gave me the initial optimism of "Oh, I can just toss together a little script in Program.cs using this package, this'll be easy". If you do want to explore his solution, be mindful of [at least this bug](https://github.com/LukeHagar/plexcsharp/issues/10). His projects have been immensely helpful in writing my own solution that doesn't use it.

I wasn't able to wrap my head around the `plexcsharp` API, and turned to just trying out the REST requests it documents. The responses from my Plex server were great: a pretty pleasant REST API! I was poking around there for awhile, trying to understand `plexcsharp`, when I decided to drop the dependency and greenfield it with a new project that makes just the REST requests I care about. The project scope grows.

## Current State

1. Connects to your Plex server, using an auth token
2. Navigates your Plex server's REST API to find the configured library, then find each show, season, and unplayed episode (only shows/seasons with unplayed episodes will be included)
3. Generates titles for seasons and episodes based on their directory/file name
4. Uses your Plex server's REST API to update the title of seasons and episodes

## Short Term Objectives
- Add some sort of marker to the metadata to record that this episode's been processed by us? Ethically, without trashing people's metadata?

## Long Term Backlog
Grouped by prioritized category. Unordered within category.

1. Bugs
2. Important Features
	- Try to read the directories used by the media files (probably with new config for path root), verify the same files exist as seen for episodes
		- Then look for a `.configure` file in that directory, and read it before iterating through the episodes. Use it's configuration to apply metadata
		- e.g. `.configure`:
		```json
		{
			"season": {
				"title": "Race 3 - Barcelona",
				"summary": "lorem ipsum",
				"img": // todo			
			},
			"episodes": [
				{
					"file": "s03e01.qualy.mp4",
					"title": "Qualifying",
					"img": // todo	
				},
				{
					"file": "s03e02.sprint.mp4",
					"title": "Sprint"
					"img": // todo	
				},
				{
					"file": "s03e03.race.mp4",
					"title": "Race"
					"img": // todo	
				},
				{
					"file": "s03e03.analysis.mp4",
					"title": "Post-Race Analysis",
					"summary": "lorem ipsum"
				},
			]
		}
		```
		- Do the same at the show level (e.g. `A:/TV_Sports/MotoGP_2024`), but here let the seasons speak for themselves in their own files
		```json
			{
			"show": {
				"title": "Liberty GP 2024",
				"summary": "lorem ipsum",
				"img": // todo			
			}
		}
		```
3. Research
4. Cleanup & Refinement
	- Replace all the `Console` writing with some sort of lightweight logging that can be configured to different sinks? Is there actually a **lightweight** solution?
5. Unsortables
