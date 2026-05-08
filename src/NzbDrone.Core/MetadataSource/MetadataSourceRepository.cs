using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.MetadataSource
{
    public interface IMetadataSourceRepository : IProviderRepository<MetadataSourceDefinition>
    {
    }

    public class MetadataSourceRepository : ProviderRepository<MetadataSourceDefinition>, IMetadataSourceRepository
    {
        public MetadataSourceRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
