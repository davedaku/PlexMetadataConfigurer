﻿using System.Text;

namespace PlexMetadataConfigurer.DTO;

/// <summary>
///		DTO used to prepare a PUT to update an episode's metadata.
/// </summary>
/// <remarks>
///		Intentional duplication between EpisodeUpdate/SeasonUpdate: properties should diverge over time, and 
///		an interface on the methods doesn't have any apparent value (other than theoretical unit testing)
/// </remarks>
public class EpisodeUpdate
{
	//public string? Art { get; set; } // todo

	//public List<Image>? Image { get; set; } // todo

	public string? Summary { get; set; }

	//public string? Thumbnail { get; set; } // todo

	public string? Title { get; set; }

	public bool Unchanged(Episode existing)
	{
		if (Summary is null != existing.Summary is null)
			return false;

		if (Summary != null && !Summary.Equals(existing.Summary))
			return false;

		if (Title is null != existing.Title is null)
			return false;

		if (Title != null && !Title.Equals(existing.Title))
			return false;

		return true;
	}

	public string GetQuerystringParams()
	{
		// these values should still have spaces, etc in them. They will be encoded later (by the httpClient, apparently)

		var querystring = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(Title))
			querystring.Append($"&title.value={Title}");

		if (!string.IsNullOrWhiteSpace(Summary))
			querystring.Append($"&summary.value={Summary}");

		return querystring.ToString();
	}
}
