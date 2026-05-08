using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class SkyHookMetadataSource : MetadataSourceBase<NullConfig>
    {
        private readonly SkyHookProxy _proxy;
        private readonly Logger _logger;

        public SkyHookMetadataSource(SkyHookProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public override string Name => "TheTVDB";

        public override IEnumerable<ProviderDefinition> DefaultDefinitions =>
            new List<ProviderDefinition>
            {
                new MetadataSourceDefinition
                {
                    Name = "TheTVDB",
                    Implementation = GetType().Name,
                    Settings = new NullConfig(),
                    Enable = true,
                    Priority = 1
                }
            };

        public override Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbId, int tmdbId)
        {
            if (tvdbId > 0)
            {
                return _proxy.GetSeriesInfo(tvdbId);
            }

            // If no TVDB ID, this source cannot fetch the series
            throw new SeriesNotFoundException(tvdbId);
        }

        public override List<Series> SearchForNewSeries(string title) =>
            _proxy.SearchForNewSeries(title);

        public override List<Series> SearchForNewSeriesByImdbId(string imdbId) =>
            _proxy.SearchForNewSeriesByImdbId(imdbId);

        public override List<Series> SearchForNewSeriesByAniListId(int aniListId) =>
            _proxy.SearchForNewSeriesByAniListId(aniListId);

        public override List<Series> SearchForNewSeriesByTmdbId(int tmdbId) =>
            _proxy.SearchForNewSeriesByTmdbId(tmdbId);

        public override List<Series> SearchForNewSeriesByMyAnimeListId(int malId) =>
            _proxy.SearchForNewSeriesByMyAnimeListId(malId);

        public override ValidationResult Test()
        {
            var results = new List<ValidationFailure>();

            try
            {
                _proxy.SearchForNewSeries("test");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect to TheTVDB via SkyHook");
                results.Add(new ValidationFailure("Url", "Unable to connect to TheTVDB: " + ex.Message));
            }

            return new ValidationResult(results);
        }
    }
}
