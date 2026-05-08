using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.TheMovieDb.Resource
{
    public class TmdbSearchResultResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Overview { get; set; }
        public string FirstAirDate { get; set; }
        public string OriginalLanguage { get; set; }
        public string PosterPath { get; set; }
        public string BackdropPath { get; set; }
        public List<string> OriginCountry { get; set; }
        public double VoteAverage { get; set; }
        public int VoteCount { get; set; }
    }

    public class TmdbSearchResource
    {
        public TmdbSearchResource()
        {
            Results = new List<TmdbSearchResultResource>();
        }

        public int Page { get; set; }
        public int TotalResults { get; set; }
        public int TotalPages { get; set; }
        public List<TmdbSearchResultResource> Results { get; set; }
    }

    public class TmdbFindTvResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Overview { get; set; }
        public string FirstAirDate { get; set; }
        public string OriginalLanguage { get; set; }
        public string PosterPath { get; set; }
        public string BackdropPath { get; set; }
    }

    public class TmdbFindResource
    {
        public TmdbFindResource()
        {
            TvResults = new List<TmdbFindTvResult>();
        }

        public List<TmdbFindTvResult> TvResults { get; set; }
    }
}
