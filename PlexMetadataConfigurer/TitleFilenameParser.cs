using System.Text.RegularExpressions;

namespace PlexMetadataConfigurer;

/// <summary>
///		Parses file names (or directories from their path) for titles content
/// </summary>
/// <remarks>
///		`partial` for the Regex
/// </remarks>
public static partial class TitleFilenameParser
{
	/// <summary>
	///		Get a season title from a value appended to a normal season directory
	/// </summary>
	public static string? SeasonTitle(string? episodeMediaFilename)
	{
		if (string.IsNullOrWhiteSpace(episodeMediaFilename))
			return null;

		string seasonDir = string.Empty;
		try
		{
			char dirSeparator = episodeMediaFilename.Contains('\\') ? '\\' : '/';
			var minusFile = episodeMediaFilename.Substring(0, episodeMediaFilename.LastIndexOf(dirSeparator));
			seasonDir = minusFile.Substring(minusFile.LastIndexOf(dirSeparator) + 1);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\tError parsing directory from episode media filename! {ex.Message}");
		}

		if (string.IsNullOrEmpty(seasonDir))
			return null;

		// try to parse it, expecting something like `(Season )## - {title}`
		var dirMatch = DirectoryNameRegex().Match(seasonDir);
		if (dirMatch.Success && (dirMatch.Groups.Count > 3))
			return dirMatch.Groups[3].Value;

		Console.WriteLine($"\tCould not match '{seasonDir}' to pattern");
		return null;
	}

	/// <summary>
	///		Get an episode title from the episode filename
	/// </summary>
	public static string? EpisodeTitle(string mediaFilename, int maxLength = 48)
	{
		int seasonNum = 0;
		int episodeNum = 0;
		string? episodeTitle = null;

		var cleanSimpleFormat = CleanSimpleEpFilenameFormatRegex().Match(mediaFilename);
		if (cleanSimpleFormat.Success)
		{
			// [0] is the entire string that had matches
			_ = int.TryParse(cleanSimpleFormat.Groups[1].Value, out seasonNum);
			_ = int.TryParse(cleanSimpleFormat.Groups[2].Value, out episodeNum);
			episodeTitle = cleanSimpleFormat.Groups[3].Value;
			// [4] is anything in `()` at the end of the filename
			// [5] is the file extension
		}

		if (string.IsNullOrEmpty(episodeTitle))
		{
			// todo: try other regex patterns

			Console.WriteLine($"\t{nameof(CleanSimpleEpFilenameFormatRegex)} can't parse '{mediaFilename}'");
		}

		if (string.IsNullOrEmpty(episodeTitle))
			return null;

		episodeTitle = string.Join(string.Empty, episodeTitle
			.Replace('.', ' ')
			.Replace('_', ' ')
			.Take(maxLength)
			).Trim();

		return episodeTitle;
	}

	/// <summary>
	///		Intended for directory names like: 'Season 07 - France`
	/// </summary>
	[GeneratedRegex(@"^(SEASON |Season |season )?(\d+)\s?-?\s?(.*)$")]
	private static partial Regex DirectoryNameRegex();

	/// <summary>
	///		Intended for filenames like: `s05e04.Sprint.Race.mp4`
	/// </summary>
	/// <remarks>
	///		Also works for names like `GP.s05e04-Sprint.Race (1080p).mp4`
	///		
	///		Falls apart when there's no separator after 's#e#' (`s03e02SprintRace.mp4`), or for
	///		names with suffixes like `...Qualifying.WEB-DL.1080p.H264.English.Ukrainian-DC46.mkv`
	/// </remarks>
	[GeneratedRegex(@"s([\d]+)e([\d]+).(.*?)(\(.*\))?\.(avi|mkv|mp4|ts)$")]
	private static partial Regex CleanSimpleEpFilenameFormatRegex();
}
