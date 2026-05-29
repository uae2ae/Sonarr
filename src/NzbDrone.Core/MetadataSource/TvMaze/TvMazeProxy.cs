using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.TvMaze.Resource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource.TvMaze
{
    public class TvMazeProxy : MetadataSourceBase<NullConfig>
    {
        private const string TvMazeBaseUrl = "https://api.tvmaze.com";

        private readonly IHttpClient _httpClient;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public TvMazeProxy(IHttpClient httpClient,
                           ISeriesService seriesService,
                           Logger logger)
        {
            _httpClient = httpClient;
            _seriesService = seriesService;
            _logger = logger;
        }

        public override string Name => "TVmaze";

        public override IEnumerable<ProviderDefinition> DefaultDefinitions =>
            new List<ProviderDefinition>
            {
                new MetadataSourceDefinition
                {
                    Name = "TVmaze",
                    Implementation = GetType().Name,
                    Settings = new NullConfig(),
                    Enable = true,
                    Priority = 2
                }
            };

        public override Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbId, int tmdbId)
        {
            TvMazeShowResource show = null;

            if (tvdbId > 0)
            {
                show = LookupByTvdbId(tvdbId);
            }

            if (show == null)
            {
                throw new SeriesNotFoundException(tvdbId);
            }

            show = GetShowWithEmbeds(show.Id);

            if (show == null)
            {
                throw new SeriesNotFoundException(tvdbId);
            }

            var series = MapSeries(show);
            var episodes = MapEpisodes(show);

            return new Tuple<Series, List<Episode>>(series, episodes);
        }

        public override List<Series> SearchForNewSeries(string title)
        {
            try
            {
                var lowerTitle = title.ToLowerInvariant();

                if (lowerTitle.StartsWith("tvmaze:") || lowerTitle.StartsWith("tvmazeid:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace) ||
                        !int.TryParse(slug, out var tvMazeId) || tvMazeId <= 0)
                    {
                        return new List<Series>();
                    }

                    var showById = GetShowWithEmbeds(tvMazeId);

                    if (showById == null)
                    {
                        return new List<Series>();
                    }

                    return new List<Series> { MapSeries(showById) };
                }

                if (lowerTitle.StartsWith("tvdb:") || lowerTitle.StartsWith("tvdbid:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace) ||
                        !int.TryParse(slug, out var tvdbId) || tvdbId <= 0)
                    {
                        return new List<Series>();
                    }

                    var byTvdb = LookupByTvdbId(tvdbId);

                    if (byTvdb == null)
                    {
                        return new List<Series>();
                    }

                    return new List<Series> { MapSeries(byTvdb) };
                }

                var request = new HttpRequestBuilder($"{TvMazeBaseUrl}/search/shows")
                    .Accept(HttpAccept.Json)
                    .AddQueryParam("q", title.ToLower().Trim())
                    .Build();

                var response = _httpClient.Get<List<TvMazeSearchResultResource>>(request);

                if (response.Resource == null || !response.Resource.Any())
                {
                    return new List<Series>();
                }

                return response.Resource
                    .Where(r => r.Show != null)
                    .Select(r => MapSearchResult(r.Show))
                    .ToList();
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "TVmaze search failed for '{0}'", title);
                throw new TvMazeException("Search for '{0}' failed. Unable to communicate with TVmaze.", ex, title);
            }
            catch (Exception ex) when (!(ex is TvMazeException))
            {
                _logger.Warn(ex, "TVmaze search failed for '{0}'", title);
                throw new TvMazeException("Search for '{0}' failed. Invalid response from TVmaze.", ex, title);
            }
        }

        public override List<Series> SearchForNewSeriesByImdbId(string imdbId)
        {
            imdbId = Parser.Parser.NormalizeImdbId(imdbId);

            if (imdbId == null)
            {
                return new List<Series>();
            }

            try
            {
                var request = new HttpRequestBuilder($"{TvMazeBaseUrl}/lookup/shows")
                    .Accept(HttpAccept.Json)
                    .AddQueryParam("imdb", imdbId)
                    .Build();

                var response = _httpClient.Get<TvMazeShowResource>(request);

                if (response.Resource == null)
                {
                    return new List<Series>();
                }

                return new List<Series> { MapSearchResult(response.Resource) };
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<Series>();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TVmaze lookup by IMDB ID failed for '{0}'", imdbId);
                return new List<Series>();
            }
        }

        public override List<Series> SearchForNewSeriesByAniListId(int aniListId)
        {
            // TVmaze doesn't support AniList ID lookup
            return new List<Series>();
        }

        public override List<Series> SearchForNewSeriesByTmdbId(int tmdbId)
        {
            // TVmaze doesn't support TMDB ID lookup
            return new List<Series>();
        }

        public override List<Series> SearchForNewSeriesByMyAnimeListId(int malId)
        {
            // TVmaze doesn't support MyAnimeList ID lookup
            return new List<Series>();
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var request = new HttpRequestBuilder($"{TvMazeBaseUrl}/shows/1")
                    .Accept(HttpAccept.Json)
                    .Build();

                var response = _httpClient.Get(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    failures.Add(new ValidationFailure("Url",
                        $"Unable to connect to TVmaze. Status: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to TVmaze");
                failures.Add(new ValidationFailure("Url", "Unable to connect to TVmaze: " + ex.Message));
            }

            return new ValidationResult(failures);
        }

        private TvMazeShowResource LookupByTvdbId(int tvdbId)
        {
            try
            {
                var request = new HttpRequestBuilder($"{TvMazeBaseUrl}/lookup/shows")
                    .Accept(HttpAccept.Json)
                    .AddQueryParam("thetvdb", tvdbId)
                    .Build();

                var response = _httpClient.Get<TvMazeShowResource>(request);
                return response.Resource;
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TVmaze lookup by TVDB ID failed for {0}", tvdbId);
                return null;
            }
        }

        private TvMazeShowResource GetShowWithEmbeds(int tvMazeId)
        {
            try
            {
                var request = new HttpRequestBuilder($"{TvMazeBaseUrl}/shows/{tvMazeId}")
                    .Accept(HttpAccept.Json)
                    .AddQueryParam("embed[]", "episodes")
                    .AddQueryParam("embed[]", "cast")
                    .AddQueryParam("embed[]", "seasons")
                    .Build();

                var response = _httpClient.Get<TvMazeShowResource>(request);
                return response.Resource;
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TVmaze fetch failed for show ID {0}", tvMazeId);
                return null;
            }
        }

        private Series MapSearchResult(TvMazeShowResource show)
        {
            if (show.Externals?.TheTvDb.HasValue == true)
            {
                var existing = _seriesService.FindByTvdbId(show.Externals.TheTvDb.Value);

                if (existing != null)
                {
                    return existing;
                }
            }

            return MapSeries(show);
        }

        private Series MapSeries(TvMazeShowResource show)
        {
            var series = new Series
            {
                TvMazeId = show.Id,
                Title = show.Name,
                CleanTitle = Parser.Parser.CleanSeriesTitle(show.Name),
                SortTitle = SeriesTitleNormalizer.Normalize(show.Name, 0),
                Overview = StripHtml(show.Summary),
                Monitored = true
            };

            if (show.Externals != null)
            {
                if (show.Externals.TheTvDb.HasValue)
                {
                    series.TvdbId = show.Externals.TheTvDb.Value;
                    series.SortTitle = SeriesTitleNormalizer.Normalize(show.Name, series.TvdbId);
                }

                if (show.Externals.TvRage.HasValue)
                {
                    series.TvRageId = show.Externals.TvRage.Value;
                }

                series.ImdbId = show.Externals.Imdb;
            }

            series.TitleSlug = show.Name?.ToLowerInvariant().Replace(" ", "-");

            if (show.Language.IsNotNullOrWhiteSpace())
            {
                series.OriginalLanguage = IsoLanguages.Find(show.Language.ToLower())?.Language ?? Language.English;
            }
            else
            {
                series.OriginalLanguage = Language.English;
            }

            if (show.Premiered.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(show.Premiered, "yyyy-MM-dd",
                    System.Globalization.DateTimeFormatInfo.InvariantInfo,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var firstAired))
            {
                series.FirstAired = firstAired;
                series.Year = firstAired.Year;
            }

            if (show.Ended.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact(show.Ended, "yyyy-MM-dd",
                    System.Globalization.DateTimeFormatInfo.InvariantInfo,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var lastAired))
            {
                series.LastAired = lastAired;
            }

            series.Status = MapStatus(show.Status);
            series.Genres = show.Genres ?? new List<string>();

            var network = show.Network ?? show.WebChannel;

            if (network != null)
            {
                series.Network = network.Name;

                if (network.Country != null)
                {
                    series.OriginalCountry = network.Country.Code;
                }
            }

            if (show.Runtime.HasValue)
            {
                series.Runtime = show.Runtime.Value;
            }
            else if (show.AverageRuntime.HasValue)
            {
                series.Runtime = show.AverageRuntime.Value;
            }

            if (show.Schedule != null && show.Schedule.Time.IsNotNullOrWhiteSpace())
            {
                series.AirTime = show.Schedule.Time;
            }

            if (show.Rating?.Average.HasValue == true)
            {
                series.Ratings = new Ratings
                {
                    Value = (decimal)show.Rating.Average.Value
                };
            }

            // Images
            if (show.Image != null)
            {
                if (show.Image.Original.IsNotNullOrWhiteSpace())
                {
                    series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster, show.Image.Original));
                }
                else if (show.Image.Medium.IsNotNullOrWhiteSpace())
                {
                    series.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Poster, show.Image.Medium));
                }
            }

            // Seasons from embedded
            if (show.Embedded?.Seasons?.Any() == true)
            {
                series.Seasons = show.Embedded.Seasons.Select(s => new Season
                {
                    SeasonNumber = s.Number,
                    Monitored = s.Number > 0,
                    Images = s.Image?.Original.IsNotNullOrWhiteSpace() == true
                        ? new List<MediaCover.MediaCover>
                        {
                            new MediaCover.MediaCover(MediaCoverTypes.Poster, s.Image.Original)
                        }
                        : new List<MediaCover.MediaCover>()
                }).ToList();
            }

            // Actors from embedded cast
            if (show.Embedded?.Cast?.Any() == true)
            {
                series.Actors = show.Embedded.Cast
                    .Where(c => c.Person != null && c.Character != null)
                    .Take(20)
                    .Select(c => new Actor
                    {
                        Name = c.Person.Name,
                        Character = c.Character.Name,
                        Images = c.Person.Image?.Original.IsNotNullOrWhiteSpace() == true
                            ? new List<MediaCover.MediaCover>
                            {
                                new MediaCover.MediaCover(MediaCoverTypes.Headshot, c.Person.Image.Original)
                            }
                            : new List<MediaCover.MediaCover>()
                    })
                    .ToList();
            }

            return series;
        }

        private static List<Episode> MapEpisodes(TvMazeShowResource show)
        {
            if (show.Embedded?.Episodes == null)
            {
                return new List<Episode>();
            }

            return show.Embedded.Episodes
                .Where(e => e.Number.HasValue) // skip unnumbered specials
                .Select(MapEpisode)
                .ToList();
        }

        private static Episode MapEpisode(TvMazeEpisodeResource e)
        {
            var episode = new Episode
            {
                SeasonNumber = e.Season,
                EpisodeNumber = e.Number ?? 0,
                Title = e.Name,
                Overview = StripHtml(e.Summary),
                AirDate = e.Airdate,
                Runtime = e.Runtime ?? 0
            };

            if (e.Airstamp.IsNotNullOrWhiteSpace() &&
                DateTime.TryParse(e.Airstamp,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var airDateUtc))
            {
                episode.AirDateUtc = airDateUtc.ToUniversalTime();
            }

            if (e.Rating?.Average.HasValue == true)
            {
                episode.Ratings = new Ratings
                {
                    Value = (decimal)e.Rating.Average.Value
                };
            }

            if (e.Image?.Original.IsNotNullOrWhiteSpace() == true)
            {
                episode.Images.Add(new MediaCover.MediaCover(MediaCoverTypes.Screenshot, e.Image.Original));
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
                case "to be determined":
                    return SeriesStatusType.Ended;
                case "in development":
                    return SeriesStatusType.Upcoming;
                default:
                    return SeriesStatusType.Continuing;
            }
        }

        private static string StripHtml(string html)
        {
            if (html.IsNullOrWhiteSpace())
            {
                return html;
            }

            return Regex.Replace(html, "<[^>]*>", string.Empty).Trim();
        }
    }
}
