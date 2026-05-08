using NzbDrone.Core.MetadataSource;
using Sonarr.Api.V5.Provider;

namespace Sonarr.Api.V5.MetadataSource;

public class MetadataSourceResource : ProviderResource<MetadataSourceResource>
{
    public bool Enable { get; set; }
    public int Priority { get; set; }
}

public class MetadataSourceResourceMapper : ProviderResourceMapper<MetadataSourceResource, MetadataSourceDefinition>
{
    public override MetadataSourceResource ToResource(MetadataSourceDefinition definition)
    {
        var resource = base.ToResource(definition);

        resource.Enable = definition.Enable;
        resource.Priority = definition.Priority;

        return resource;
    }

    public override MetadataSourceDefinition ToModel(MetadataSourceResource resource, MetadataSourceDefinition? existingDefinition)
    {
        var definition = base.ToModel(resource, existingDefinition);

        definition.Enable = resource.Enable;
        definition.Priority = resource.Priority;

        return definition;
    }
}

public class MetadataSourceBulkResource : ProviderBulkResource<MetadataSourceBulkResource>
{
    public bool? Enable { get; set; }
    public int? Priority { get; set; }
}

public class MetadataSourceBulkResourceMapper : ProviderBulkResourceMapper<MetadataSourceBulkResource, MetadataSourceDefinition>
{
    public override List<MetadataSourceDefinition> UpdateModel(MetadataSourceBulkResource resource, List<MetadataSourceDefinition> existingDefinitions)
    {
        existingDefinitions.ForEach(existing =>
        {
            existing.Enable = resource.Enable ?? existing.Enable;
            existing.Priority = resource.Priority ?? existing.Priority;
        });

        return existingDefinitions;
    }
}
