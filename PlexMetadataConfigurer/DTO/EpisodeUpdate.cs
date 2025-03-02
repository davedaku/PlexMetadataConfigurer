using System.Text;

namespace PlexMetadataConfigurer.DTO;

/// <summary>
///		DTO used to prepare a PUT to update an episode's metadata. 
/// </summary>
public class EpisodeUpdate
{
	public string? Art { get; set; }

	public List<Image>? Image { get; set; }

	public string? Summary { get; set; }

	public string? Thumbnail { get; set; }

	public string? Title { get; set; }

	public int? Year { get; set; }

	public string GetQuerystringParams()
	{
		var querystring = new StringBuilder();

		if (!string.IsNullOrWhiteSpace(Title))
		{
			string encodedTitle = Title; // this should still have spaces, etc in it. It will be encoded later (by the httpClient, apparently)
			querystring.Append($"&title.value={encodedTitle}");
		}

		// todo: other props

		return querystring.ToString();
	}
}