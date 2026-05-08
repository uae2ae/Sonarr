using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.MetadataSource
{
    public class MetadataSourceDefinition : ProviderDefinition
    {
        public const int DefaultPriority = 1;

        public MetadataSourceDefinition()
        {
            Priority = DefaultPriority;
            Enable = true;
        }

        public int Priority { get; set; }
        public override bool Enable { get; set; }
    }
}
