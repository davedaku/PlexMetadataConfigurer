

namespace PlexMetadataConfigurer.Test
{
	public class UtilityTest
	{
		[Theory]
		[InlineData("s02e01.Race.Moto3.mp4", "Race Moto3")]
		[InlineData("s12e02.Qualifying.MotoGP.mp4", "Qualifying MotoGP")]
		//[InlineData("s14e04MotoGP.2022.Round14.Italy.Misano.Race.Web-Rip.1080p.50fps.X264.English.Russian.Natural.Sounds-DC46.mkv", "Episode 4", "hahahahaha")]
		//[InlineData("s13e03-MotoGP.2022.Round.13.AustrianGP.Spielberg.Austria.Race.1080p50.SS.mkv", "Episode 3", "todo")]
		//[InlineData("s06e02MotoGP.2023.Round06.Italy.Mugello.Sprint.WEB-DL.1080p.H264.English.Russian-DC46.mkv", "Episode 2", "todo")]
		[InlineData("s06e03.Race.MotoGP.mp4", "Race MotoGP")]
		[InlineData("s06e01. WorldSSP300 - FP1 (593mb 1920x1080 47.546fps 2214kbps x265 deef).mkv", "WorldSSP300 - FP1")]
		public void Utility_EpisodeTitle(string filename, string? expectedTitle)
		{
			var result = Utility.EpisodeTitle(filename);

			if (expectedTitle is null)
				Assert.Null(result);
			else
				Assert.Equal(expectedTitle, result);
		}
	}
}
