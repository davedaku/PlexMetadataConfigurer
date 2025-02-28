using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlexMetadataConfigurer;

// todo: get rid of this, refactor this away somehow
public static partial class Utility
{



	public static string EpisodeTitle(string currentTitle, string mediaFilename, int maxLength = 48)
	{
		int seasonNum = 0;
		int episodeNum = 0;
		string? episodeTitle = null;

		var cleanSimpleFormat = CleanSimpleFormatRegex().Match(mediaFilename);
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
			// todo: try other patterns
			Console.WriteLine($"\t{nameof(CleanSimpleFormatRegex)} can't parse '{mediaFilename}'");
		}

		if (string.IsNullOrEmpty(episodeTitle))
			return currentTitle;

		episodeTitle = string.Join(string.Empty, episodeTitle
			.Replace('.', ' ')
			.Replace('_', ' ')
			.Take(maxLength)
			).Trim();

		return episodeTitle;
	}

	/// <summary>
	///		Intended for filenames like: `s05e04.MotoGP.Sprint.Race.mp4`
	/// </summary>
	/// <remarks>
	///		Also works for names like `MotoGP.s05e04-MotoGP.Sprint.Race (1080p).mp4`
	///		
	///		Falls apart when there's no separator after 's#e#', or for
	///		names like `s06e01MotoGP.2023.Round06.Italy.Mugello.Qualifying.WEB-DL.1080p.H264.English.Russian-DC46.mkv`
	/// </remarks>
	[GeneratedRegex(@"s([\d]+)e([\d]+).(.*?)(\(.*\))?\.(avi|mkv|mp4|ts)$")]
	private static partial Regex CleanSimpleFormatRegex();
}
