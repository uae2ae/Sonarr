using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.TheMovieDb.Resource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource.TheMovieDb
{
    public class TheMovieDbProxy : MetadataSourceBase<TheMovieDbSettings>
    {
        private const string TmdbBaseUrl = "https://api.themoviedb.org/3";
        private const string TmdbImageBaseUrl = "https://image.tmdb.org/t/p/original";

        private readonly IHttpClient _httpClient;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public TheMovieDbProxy(IHttpClient httpClient,
                               ISeriesService seriesService,
                               Logger logger)
        {
            _httpClient = httpClient;
            _seriesService = seriesService;
            _logger = logger;
        }

        public override string Name => "TheMovieDB";

        public override IEnumerable<ProviderDefinition> DefaultDefinitions =>
            new List<ProviderDefinition>
            {
                new MetadataSourceDefinition
                {
                    Name = "TheMovieDB",
                    Implementation = GetType().Name,
                    Settings = new TheMovieDbSettings(),
                    Enable = false,
                    Priority = 3
                }
            };

        public override Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbId, int tmdbId)
        {
            if (!Settings.Validate().IsValid)
            {
                throw new Exception("TheMovieDB API key is not configured.");
            }

            // If TMDB ID is not provided, look it up via TVDB ID
            if (tmdbId == 0 && tvdbId > 0)
            {
                tmdbId = FindTmdbIdByTvdbId(tvdbId);

                if (tmdbId == 0)
                {
                    throw new SeriesNotFoundException(tvdbId);
                }
            }

            if (tmdbId == 0)
            {
                throw new SeriesNotFoundException(tvdbId);
            }

            var show = GetShowDetails(tmdbId);

            if (show == null)
            {
                throw new SeriesNotFoundException(tvdbId);
            }

            var episodes = GetAllEpisodes(tmdbId, show);
            var series = MapSeries(show);

            return new Tuple<Series, List<Episode>>(series, episodes);
        }

        public override List<Series> SearchForNewSeries(string title)
        {
            if (!Settings.Validate().IsValid)
            {
                return new List<Series>();
            }

            if (title.IsPathValid(PathValidationType.AnyOs))
            {
                throw new InvalidSearchTermException("Invalid search term '{0}'", title);
            }

            try
            {
                var lowerTitle = title.ToLowerInvariant();

                if (lowerTitle.StartsWith("tmdb:") || lowerTitle.StartsWith("tmdbid:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace) ||
                        !int.TryParse(slug, out var tmdbId) || tmdbId <= 0)
                    {
                        return new List<Series>();
                    }

                    return SearchForNewSeriesByTmdbId(tmdbId);
                }

                var request = BuildRequest($"{TmdbBaseUrl}/search/tv")
                    .AddQueryParam("query", title.ToLower().Trim())
                    .AddQueryParam("language", "en-US")
                    .Build();

                var response = _httpClient.Get<TmdbSearchResource>(request);

                if (response.Resource?.Results == null)
                {
                    return new List<Series>();
                }

                return response.Resource.Results.Select(MapSearchResult).ToList();
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "TheMovieDB search failed for '{0}'", title);
                throw new TheMovieDbException("Search for '{0}' failed. Unable to communicate with TheMovieDB.", ex, title);
            }
            catch (Exception ex) when (!(ex is InvalidSearchTermException || ex is TheMovieDbException))
            {
                _logger.Warn(ex, "TheMovieDB search failed for '{0}'", title);
                throw new TheMovieDbException("Search for '{0}' failed. Invalid response from TheMovieDB.", ex, title);
            }
        }

        public override List<Series> SearchForNewSeriesByImdbId(string imdbId)
        {
            if (!Settings.Validate().IsValid)
            {
                return new List<Series>();
            }

            imdbId = Parser.Parser.NormalizeImdbId(imdbId);

            if (imdbId == null)
            {
                return new List<Series>();
            }

            try
            {
                var request = BuildRequest($"{TmdbBaseUrl}/find/{imdbId}")
                    .AddQueryParam("external_source", "imdb_id")
                    .Build();

                var response = _httpClient.Get<TmdbFindResource>(request);

                if (response.Resource?.TvResults == null || !response.Resource.TvResults.Any())
                {
                    return new List<Series>();
                }

                return response.Resource.TvResults
                    .Select(r => SearchForNewSeriesByTmdbId(r.Id))
                    .SelectMany(l => l)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TheMovieDB search by IMDB ID failed for '{0}'", imdbId);
                return new List<Series>();
            }
        }

        public override List<Series> SearchForNewSeriesByAniListId(int aniListId)
        {
            // TheMovieDB doesn't support AniList ID lookup
            return new List<Series>();
        }

        public override List<Series> SearchForNewSeriesByTmdbId(int tmdbId)
        {
            if (!Settings.Validate().IsValid)
            {
                return new List<Series>();
            }

            try
            {
                var existing = _seriesService.FindByTmdbId(tmdbId);

                if (existing != null)
                {
                    return new List<Series> { existing };
                }

                var show = GetShowDetails(tmdbId);

                if (show == null)
                {
                    return new List<Series>();
                }

                return new List<Series> { MapSeries(show) };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TheMovieDB search by TMDB ID failed for '{0}'", tmdbId);
                return new List<Series>();
            }
        }

        public override List<Series> SearchForNewSeriesByMyAnimeListId(int malId)
        {
            // TheMovieDB doesn't support MyAnimeList ID lookup
            return new List<Series>();
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var request = BuildRequest($"{TmdbBaseUrl}/configuration").Build();
                var response = _httpClient.Get(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    failures.Add(new ValidationFailure("ApiKey",
                        $"Unable to connect to TheMovieDB. Status: {response.StatusCode}"));
                }
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
            {
                failures.Add(new ValidationFailure("ApiKey", "Invalid API key for TheMovieDB"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to validate TheMovieDB API key");
                failures.Add(new ValidationFailure("ApiKey", "Unable to connect to TheMovieDB: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        private int FindTmdbIdByTvdbId(int tvdbId)
        {
            try
            {
                var request = BuildRequest($"{TmdbBaseUrl}/find/{tvdbId}")
                    .AddQueryParam("external_source", "tvdb_id")
                    .Build();

                var response = _httpClient.Get<TmdbFindResource>(request);

                if (response.Resource?.TvResults?.Any() == true)
                {
                    return response.Resource.TvResults[0].Id;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to find TMDB ID for TVDB ID {0}", tvdbId);
            }

            return 0;
        }

        private TmdbShowResource GetShowDetails(int tmdbId)
        {
            try
            {
                var request = BuildRequest($"{TmdbBaseUrl}/tv/{tmdbId}")
                    .AddQueryParam("append_to_response", "external_ids,credits,images,content_ratings")
                    .AddQueryParam("language", "en-US")
                    .Build();

                var response = _httpClient.Get<TmdbShowResource>(request);
                return response.Resource;
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        private List<Episode> GetAllEpisodes(int tmdbId, TmdbShowResource show)
        {
            var allEpisodes = new List<Episode>();

            foreach (var seasonSummary in show.Seasons)
            {
                try
                {
                    var request = BuildRequest($"{TmdbBaseUrl}/tv/{tmdbId}/season/{seasonSummary.SeasonNumber}")
                        .AddQueryParam("language", "en-US")
                        .Build();

                    var response = _httpClient.Get<TmdbSeasonResource>(request);

                    if (response.Resource?.Episodes != null)
                    {
                        allEpisodes.AddRange(response.Resource.Episodes.Select(MapEpisode));
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to fetch episodes for TMDB show {0} season {1}",
                        tmdbId, seasonSummary.SeasonNumber);
                }
            }

            return allEpisodes;
        }

        private HttpRequestBuilder BuildRequest(string url)
        {
            return new HttpRequestBuilder(url)
                .Accept(HttpAccept.Json)
                .SetHeader("Authorization", $"Bearer {Settings.ApiKey}");
        }

        private Series MapSearchResult(TmdbSearchResultResource result)
        {
            var existing = _seriesService.FindByTmdbId(result.Id);

            if (existing != null)
            {
                return existing;
            }

            var series = new Series
            {
                TmdbId = result.Id,
                Title = result.Name,
                CleanTitle = Parser.Parser.CleanSeriesTitle(result.Name),
                SortTitle = SeriesTitleNormalizer.Normalize(result.Name, 0),
                Overview = result.Overview,
                TitleSlug = result.Name?.ToLowerInvariant().Replace(" ", "-"),
                Monitored = true,
                OriginalLanguage = result.OriginalLanguage.IsNotNullOrWhiteSpace()
                    ? IsoLanguages.Find(result.OriginalLanguage.ToLower())?.Language ?? Language.English
                    : Language.English
            };

            if (result.FirstAirDate.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(result.FirstAirDate, "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var firstAired))
            {
                series.FirstAired = firstAired;
                series.Year = firstAired.Year;
            }

            if (result.PosterPath.IsNotNullOrWhiteSpace())
            {
                series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster,
                    TmdbImageBaseUrl + result.PosterPath));
            }

            if (result.BackdropPath.IsNotNullOrWhiteSpace())
            {
                series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Fanart,
                    TmdbImageBaseUrl + result.BackdropPath));
            }

            if (result.OriginCountry?.Any() == true)
            {
                series.OriginalCountry = result.OriginCountry[0];
            }

            return series;
        }

        private Series MapSeries(TmdbShowResource show)
        {
            var series = new Series
            {
                TmdbId = show.Id,
                Title = show.Name,
                CleanTitle = Parser.Parser.CleanSeriesTitle(show.Name),
                SortTitle = SeriesTitleNormalizer.Normalize(show.Name, 0),
                Overview = show.Overview,
                TitleSlug = show.Name?.ToLowerInvariant().Replace(" ", "-"),
                Monitored = true,
                OriginalLanguage = show.OriginalLanguage.IsNotNullOrWhiteSpace()
                    ? IsoLanguages.Find(show.OriginalLanguage.ToLower())?.Language ?? Language.English
                    : Language.English
            };

            // Map external IDs
            if (show.ExternalIds != null)
            {
                if (show.ExternalIds.TvdbId.HasValue)
                {
                    series.TvdbId = show.ExternalIds.TvdbId.Value;
                    series.SortTitle = SeriesTitleNormalizer.Normalize(show.Name, series.TvdbId);
                }

                series.ImdbId = show.ExternalIds.ImdbId;
            }

            if (show.FirstAirDate.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(show.FirstAirDate, "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var firstAired))
            {
                series.FirstAired = firstAired;
                series.Year = firstAired.Year;
            }

            if (show.LastAirDate.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(show.LastAirDate, "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var lastAired))
            {
                series.LastAired = lastAired;
            }

            series.Status = MapStatus(show.Status);
            series.Genres = show.Genres?.Select(g => g.Name).ToList() ?? new List<string>();

            if (show.Networks?.Any() == true)
            {
                series.Network = show.Networks[0].Name;
            }

            if (show.EpisodeRunTime?.Any() == true)
            {
                series.Runtime = show.EpisodeRunTime[0];
            }

            series.Ratings = new Ratings
            {
                Votes = show.VoteCount,
                Value = (decimal)show.VoteAverage
            };

            if (show.OriginCountry?.Any() == true)
            {
                series.OriginalCountry = show.OriginCountry[0];
            }

            // Content rating
            if (show.ContentRatings?.Results?.Any() == true)
            {
                var usRating = show.ContentRatings.Results
                    .FirstOrDefault(r => r.Iso31661 == "US");

                if (usRating != null)
                {
                    series.Certification = usRating.Rating;
                }
            }

            // Images
            if (show.Images != null)
            {
                var poster = show.Images.Posters.FirstOrDefault(p => p.Iso6391 == "en")
                             ?? show.Images.Posters.FirstOrDefault();

                if (poster != null)
                {
                    series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster,
                        TmdbImageBaseUrl + poster.FilePath));
                }

                var backdrop = show.Images.Backdrops.FirstOrDefault();

                if (backdrop != null)
                {
                    series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Fanart,
                        TmdbImageBaseUrl + backdrop.FilePath));
                }

                var logo = show.Images.Logos.FirstOrDefault(l => l.Iso6391 == "en")
                           ?? show.Images.Logos.FirstOrDefault();

                if (logo != null)
                {
                    series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Clearlogo,
                        TmdbImageBaseUrl + logo.FilePath));
                }
            }
            else
            {
                // Fallback: no append_to_response images available yet
            }

            // Actors
            if (show.Credits?.Cast?.Any() == true)
            {
                series.Actors = show.Credits.Cast
                    .OrderBy(c => c.Order)
                    .Take(20)
                    .Select(c => new Actor
                    {
                        Name = c.Name,
                        Character = c.Character,
                        Images = c.ProfilePath.IsNotNullOrWhiteSpace()
                            ? new List<MediaCover.MediaCover>
                            {
                                new MediaCover.MediaCover(MediaCoverTypes.Headshot,
                                    TmdbImageBaseUrl + c.ProfilePath)
                            }
                            : new List<MediaCover.MediaCover>()
                    })
                    .ToList();
            }

            // Seasons
            series.Seasons = show.Seasons?.Select(s => new Season
            {
                SeasonNumber = s.SeasonNumber,
                Monitored = s.SeasonNumber > 0,
                Images = s.PosterPath.IsNotNullOrWhiteSpace()
                    ? new List<MediaCover.MediaCover>
                    {
                        new MediaCover.MediaCover(MediaCoverTypes.Poster,
                            TmdbImageBaseUrl + s.PosterPath)
                    }
                    : new List<MediaCover.MediaCover>()
            }).ToList() ?? new List<Season>();

            return series;
        }

        private static Episode MapEpisode(TmdbEpisodeResource e)
        {
            var episode = new Episode
            {
                EpisodeNumber = e.EpisodeNumber,
                SeasonNumber = e.SeasonNumber,
                Title = e.Name,
                Overview = e.Overview,
                AirDate = e.AirDate,
                Runtime = e.Runtime ?? 0,
                Ratings = new Ratings
                {
                    Votes = e.VoteCount,
                    Value = (decimal)e.VoteAverage
                }
            };

            if (e.AirDate.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(e.AirDate, "yyyy-MM-dd",
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var airDateUtc))
            {
                episode.AirDateUtc = airDateUtc;
            }

            if (e.StillPath.IsNotNullOrWhiteSpace())
            {
                episode.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Screenshot,
                    TmdbImageBaseUrl + e.StillPath));
            }

            return episode;
        }

        private static SeriesStatusType MapStatus(string status)
        {
            if (status == null)
            {
                return SeriesStatusType.Continuing;
            }

            switch (status.ToLowerInvariant())
            {
                case "ended":
                case "canceled":
                case "cancelled":
                    return SeriesStatusType.Ended;
                case "planned":
                case "pilot":
                case "in production":
                    return SeriesStatusType.Upcoming;
                default:
                    return SeriesStatusType.Continuing;
            }
        }

        private sealed class InvalidSearchTermException : Exception
        {
            public InvalidSearchTermException(string message, params object[] args)
                : base(string.Format(message, args))
            {
            }
        }
    }
}
