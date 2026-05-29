using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.TvMaze.Resource
{
    public class TvMazeCountryResource
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Timezone { get; set; }
    }

    public class TvMazeNetworkResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TvMazeCountryResource Country { get; set; }
    }

    public class TvMazeExternalsResource
    {
        [JsonProperty("tvrage")]
        public int? TvRage { get; set; }

        [JsonProperty("thetvdb")]
        public int? TheTvDb { get; set; }

        [JsonProperty("imdb")]
        public string Imdb { get; set; }
    }

    public class TvMazeImageResource
    {
        public string Medium { get; set; }
        public string Original { get; set; }
    }

    public class TvMazeRatingResource
    {
        public double? Average { get; set; }
    }

    public class TvMazeScheduleResource
    {
        public string Time { get; set; }
        public List<string> Days { get; set; }
    }

    public class TvMazeShowResource
    {
        public TvMazeShowResource()
        {
            Genres = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public List<string> Genres { get; set; }
        public string Status { get; set; }
        public int? Runtime { get; set; }
        public int? AverageRuntime { get; set; }
        public string Premiered { get; set; }
        public string Ended { get; set; }
        public string OfficialSite { get; set; }
        public TvMazeScheduleResource Schedule { get; set; }
        public TvMazeRatingResource Rating { get; set; }
        public TvMazeNetworkResource Network { get; set; }

        [JsonProperty("webChannel")]
        public TvMazeNetworkResource WebChannel { get; set; }

        public TvMazeExternalsResource Externals { get; set; }
        public TvMazeImageResource Image { get; set; }
        public string Summary { get; set; }

        [JsonProperty("_embedded")]
        public TvMazeEmbeddedResource Embedded { get; set; }
    }

    public class TvMazeEmbeddedResource
    {
        public TvMazeEmbeddedResource()
        {
            Episodes = new List<TvMazeEpisodeResource>();
            Cast = new List<TvMazeCastResource>();
            Seasons = new List<TvMazeSeasonResource>();
        }

        public List<TvMazeEpisodeResource> Episodes { get; set; }
        public List<TvMazeCastResource> Cast { get; set; }
        public List<TvMazeSeasonResource> Seasons { get; set; }
    }

    public class TvMazeEpisodeResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Season { get; set; }
        public int? Number { get; set; }
        public string Type { get; set; }
        public string Airdate { get; set; }
        public string Airtime { get; set; }
        public string Airstamp { get; set; }
        public int? Runtime { get; set; }
        public TvMazeRatingResource Rating { get; set; }
        public TvMazeImageResource Image { get; set; }
        public string Summary { get; set; }
    }

    public class TvMazePersonResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TvMazeImageResource Image { get; set; }
    }

    public class TvMazeCharacterResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TvMazeCastResource
    {
        public TvMazePersonResource Person { get; set; }
        public TvMazeCharacterResource Character { get; set; }
    }

    public class TvMazeSeasonResource
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public int? EpisodeCount { get; set; }
        public string PremiereDate { get; set; }
        public string EndDate { get; set; }
        public TvMazeNetworkResource Network { get; set; }
        public TvMazeImageResource Image { get; set; }
    }

    public class TvMazeSearchResultResource
    {
        public double Score { get; set; }
        public TvMazeShowResource Show { get; set; }
    }
}
