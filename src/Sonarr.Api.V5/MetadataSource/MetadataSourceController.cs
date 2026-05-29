using FluentValidation;
using NzbDrone.Core.MetadataSource;
using NzbDrone.SignalR;
using Sonarr.Api.V5.Provider;
using Sonarr.Http;

namespace Sonarr.Api.V5.MetadataSource;

[V5ApiController("metadatasource")]
public class MetadataSourceController : ProviderControllerBase<MetadataSourceResource, MetadataSourceBulkResource, IMetadataSource, MetadataSourceDefinition>
{
    public static readonly MetadataSourceResourceMapper ResourceMapper = new();
    public static readonly MetadataSourceBulkResourceMapper BulkResourceMapper = new();

    private readonly IMetadataSourceFactory _metadataSourceFactory;

    public MetadataSourceController(IBroadcastSignalRMessage signalRBroadcaster,
        IMetadataSourceFactory metadataSourceFactory)
        : base(signalRBroadcaster, metadataSourceFactory, "metadatasource", ResourceMapper, BulkResourceMapper)
    {
        _metadataSourceFactory = metadataSourceFactory;

        SharedValidator.RuleFor(c => c.Priority)
            .InclusiveBetween(1, 50)
            .WithMessage("Priority must be between 1 and 50");

        SharedValidator.RuleFor(c => c.Enable)
            .Must((resource, enable) =>
            {
                if (enable)
                {
                    return true;
                }

                return _metadataSourceFactory.All().Count(d => d.Enable && d.Id != resource.Id) >= 1;
            })
            .WithMessage("At least one metadata source must be enabled.");
    }
}
