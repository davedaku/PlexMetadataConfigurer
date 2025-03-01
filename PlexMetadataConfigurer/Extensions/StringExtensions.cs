namespace PlexMetadataConfigurer.Extensions;

public static class StringExtensions
{
	/// <summary>
	///		HttpUtility.UrlEncode(str) is replacing spaces with `+`?
	/// </summary>
	public static string QuerystringEncode(this string str)
	{
		return str
			.Trim()
			.Replace(" ", "%20")
			.Replace("!", "%21")
			.Replace("\"", "%22")
			.Replace("#", "%23")
			.Replace("$", "%24")
			.Replace("%", "%25")
			.Replace("&", "%26")
			.Replace("'", "%27")
			.Replace("(", "%28")
			.Replace(")", "%29")
			.Replace("*", "%2A")
			.Replace("+", "%2B")
			.Replace(",", "%2C")
			.Replace("-", "%2D")
			.Replace(".", "%2E")
			.Replace("/", "%2F")
		;
	}

}
