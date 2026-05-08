using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.TheMovieDb.Resource
{
    public class TmdbEpisodeResource
    {
        public int Id { get; set; }
        public int EpisodeNumber { get; set; }
        public int SeasonNumber { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string AirDate { get; set; }
        public int? Runtime { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public string StillPath { get; set; }
    }

    public class TmdbSeasonResource
    {
        public TmdbSeasonResource()
        {
            Episodes = new List<TmdbEpisodeResource>();
        }

        public int Id { get; set; }
        public int SeasonNumber { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string AirDate { get; set; }
        public string PosterPath { get; set; }
        public List<TmdbEpisodeResource> Episodes { get; set; }
    }
}
