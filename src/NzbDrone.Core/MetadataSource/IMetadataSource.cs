using System;
using System.Collections.Generic;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MetadataSource
{
    public interface IMetadataSource : IProvider, ISearchForNewSeries
    {
        Tuple<Series, List<Episode>> GetSeriesInfo(int tvdbId, int tmdbId);
    }
}
