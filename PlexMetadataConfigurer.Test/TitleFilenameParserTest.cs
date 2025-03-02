
namespace PlexMetadataConfigurer.Test;

public class TitleFilenameParserTest
{
	[Theory]
	[InlineData("R:\\sports\\MGP 2022\\07- France\\s07e03.Race.MGP.mp4", "France")]
	[InlineData("R:/sports/MGP 2022/07- France/s07e03.Race.MGP.mp4", "France")]
	[InlineData("R:/sports/MGP 2022/Season 07- France/s07e03.Race.MGP.mp4", "France")]
	[InlineData("R:/sports/MGP 2022/Season 07 - France/s07e03.Race.MGP.mp4", "France")]
	[InlineData("R:/sports/MGP 2022/Season 07-France/s07e03.Race.MGP.mp4", "France")]
	[InlineData("R:/sports/MGP 2022/season 7 - France/s07e03.Race.MGP.mp4", "France")]
	public void Utility_SeasonTitle(string filename, string? expectedTitle)
	{
		var result = TitleFilenameParser.SeasonTitle(filename);

		if (expectedTitle is null)
			Assert.Null(result);
		else
			Assert.Equal(expectedTitle, result);
	}

	[Theory]
	[InlineData("s02e01.Race.M3.mp4", "Race M3")]
	[InlineData("s12e02.Qualifying.MGP.mp4", "Qualifying MGP")]
	[InlineData("s06e03.Race.MGP.mp4", "Race MGP")]
	[InlineData("s06e01. World300 - FP1 (593mb 1920x1080 47.546fps 2214kbps x265 deef).mkv", "World300 - FP1")]
	public void Utility_EpisodeTitle(string filename, string? expectedTitle)
	{
		var result = TitleFilenameParser.EpisodeTitle(filename);

		if (expectedTitle is null)
			Assert.Null(result);
		else
			Assert.Equal(expectedTitle, result);
	}
}
