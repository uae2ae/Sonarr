using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource
{
    public class MetadataSourceAggregator : IProvideSeriesInfo, ISearchForNewSeries
    {
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly Logger _logger;

        public MetadataSourceAggregator(IMetadataSourceFactory metadataSourceFactory,
                                        Logger logger)
        {
            _metadataSourceFactory = metadataSourceFactory;
            _logger = logger;
        }

        public Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbSeriesId)
        {
            return GetSeriesInfo(tvdbSeriesId, 0);
        }

        public Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbSeriesId, int tmdbId)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            if (!sources.Any())
            {
                throw new Exception("No metadata sources are enabled.");
            }

            var lastException = default(Exception);

            foreach (var source in sources)
            {
                try
                {
                    _logger.Debug("Fetching series info (tvdbId={0}, tmdbId={1}) from {2}",
                                  tvdbSeriesId, tmdbId, source.Name);

                    return source.GetSeriesInfo(tvdbSeriesId, tmdbId);
                }
                catch (SeriesNotFoundException ex)
                {
                    _logger.Debug("Series (tvdbId={0}, tmdbId={1}) not found in {2}, trying next source.",
                                  tvdbSeriesId, tmdbId, source.Name);
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to get series info from {0}, trying next source.", source.Name);
                    lastException = ex;
                }
            }

            if (lastException is SeriesNotFoundException)
            {
                throw lastException;
            }

            throw new SeriesNotFoundException(tvdbSeriesId);
        }

        public List<Series> SearchForNewSeries(string title)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            if (!sources.Any())
            {
                return new List<Series>();
            }

            var results = new List<Series>();

            foreach (var source in sources)
            {
                try
                {
                    var sourceResults = source.SearchForNewSeries(title);
                    MergeResults(results, sourceResults);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Search failed for source {0}", source.Name);
                }
            }

            return results;
        }

        public List<Series> SearchForNewSeriesByImdbId(string imdbId)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            foreach (var source in sources)
            {
                try
                {
                    var result = source.SearchForNewSeriesByImdbId(imdbId);

                    if (result.Any())
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "SearchByImdbId failed for source {0}", source.Name);
                }
            }

            return new List<Series>();
        }

        public List<Series> SearchForNewSeriesByAniListId(int aniListId)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            foreach (var source in sources)
            {
                try
                {
                    var result = source.SearchForNewSeriesByAniListId(aniListId);

                    if (result.Any())
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "SearchByAniListId failed for source {0}", source.Name);
                }
            }

            return new List<Series>();
        }

        public List<Series> SearchForNewSeriesByTmdbId(int tmdbId)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            foreach (var source in sources)
            {
                try
                {
                    var result = source.SearchForNewSeriesByTmdbId(tmdbId);

                    if (result.Any())
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "SearchByTmdbId failed for source {0}", source.Name);
                }
            }

            return new List<Series>();
        }

        public List<Series> SearchForNewSeriesByMyAnimeListId(int malId)
        {
            var sources = _metadataSourceFactory.EnabledInPriorityOrder();

            foreach (var source in sources)
            {
                try
                {
                    var result = source.SearchForNewSeriesByMyAnimeListId(malId);

                    if (result.Any())
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "SearchByMalId failed for source {0}", source.Name);
                }
            }

            return new List<Series>();
        }

        private static void MergeResults(List<Series> existing, List<Series> newResults)
        {
            foreach (var series in newResults)
            {
                // Deduplicate by TvdbId (if non-zero) or TmdbId
                var isDuplicate = false;

                if (series.TvdbId > 0)
                {
                    isDuplicate = existing.Any(e => e.TvdbId == series.TvdbId);
                }
                else if (series.TmdbId > 0)
                {
                    isDuplicate = existing.Any(e => e.TmdbId == series.TmdbId && e.TmdbId > 0);
                }

                if (!isDuplicate)
                {
                    existing.Add(series);
                }
            }
        }
    }
}
