using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource
{
    public abstract class MetadataSourceBase<TSettings> : IMetadataSource
        where TSettings : IProviderConfig, new()
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public virtual IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public abstract Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbId, int tmdbId);

        public abstract List<Series> SearchForNewSeries(string title);

        public abstract List<Series> SearchForNewSeriesByImdbId(string imdbId);

        public abstract List<Series> SearchForNewSeriesByAniListId(int aniListId);

        public abstract List<Series> SearchForNewSeriesByTmdbId(int tmdbId);

        public abstract List<Series> SearchForNewSeriesByMyAnimeListId(int malId);

        public abstract ValidationResult Test();

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }
    }
}
