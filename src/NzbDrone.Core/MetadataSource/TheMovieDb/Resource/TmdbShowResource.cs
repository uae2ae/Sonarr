using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.TheMovieDb.Resource
{
    public class TmdbGenreResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TmdbNetworkResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginCountry { get; set; }
    }

    public class TmdbCrewResource
    {
        public string Name { get; set; }
        public string Job { get; set; }
        public string Department { get; set; }
        public string ProfilePath { get; set; }
    }

    public class TmdbCastResource
    {
        public string Name { get; set; }
        public string Character { get; set; }
        public int Order { get; set; }
        public string ProfilePath { get; set; }
    }

    public class TmdbCreditsResource
    {
        public TmdbCreditsResource()
        {
            Cast = new List<TmdbCastResource>();
            Crew = new List<TmdbCrewResource>();
        }

        public List<TmdbCastResource> Cast { get; set; }
        public List<TmdbCrewResource> Crew { get; set; }
    }

    public class TmdbImageResource
    {
        public string FilePath { get; set; }
        public string Iso6391 { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
    }

    public class TmdbImagesResource
    {
        public TmdbImagesResource()
        {
            Posters = new List<TmdbImageResource>();
            Backdrops = new List<TmdbImageResource>();
            Logos = new List<TmdbImageResource>();
        }

        public List<TmdbImageResource> Posters { get; set; }
        public List<TmdbImageResource> Backdrops { get; set; }
        public List<TmdbImageResource> Logos { get; set; }
    }

    public class TmdbContentRatingResource
    {
        public string Rating { get; set; }
        public string Iso31661 { get; set; }
        public string Descriptors { get; set; }
    }

    public class TmdbContentRatingsResource
    {
        public TmdbContentRatingsResource()
        {
            Results = new List<TmdbContentRatingResource>();
        }

        public List<TmdbContentRatingResource> Results { get; set; }
    }

    public class TmdbExternalIdsResource
    {
        public int? TvdbId { get; set; }
        public string ImdbId { get; set; }
        public int? TvRageId { get; set; }
        public int? WikidataId { get; set; }
    }

    public class TmdbSeasonSummaryResource
    {
        public int Id { get; set; }
        public int SeasonNumber { get; set; }
        public int EpisodeCount { get; set; }
        public string AirDate { get; set; }
        public string Name { get; set; }
        public string Overview { get; set; }
        public string PosterPath { get; set; }
    }

    public class TmdbShowResource
    {
        public TmdbShowResource()
        {
            Genres = new List<TmdbGenreResource>();
            Networks = new List<TmdbNetworkResource>();
            Seasons = new List<TmdbSeasonSummaryResource>();
            OriginCountry = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Overview { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string FirstAirDate { get; set; }
        public string LastAirDate { get; set; }
        public string OriginalLanguage { get; set; }
        public List<string> OriginCountry { get; set; }
        public List<int> EpisodeRunTime { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public List<TmdbGenreResource> Genres { get; set; }
        public List<TmdbNetworkResource> Networks { get; set; }
        public List<TmdbSeasonSummaryResource> Seasons { get; set; }
        public TmdbExternalIdsResource ExternalIds { get; set; }
        public TmdbCreditsResource Credits { get; set; }
        public TmdbImagesResource Images { get; set; }
        public TmdbContentRatingsResource ContentRatings { get; set; }
    }
}
