using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.MetadataSource
{
    public interface IMetadataSourceFactory : IProviderFactory<IMetadataSource, MetadataSourceDefinition>
    {
        List<IMetadataSource> EnabledInPriorityOrder();
    }

    public class MetadataSourceFactory : ProviderFactory<IMetadataSource, MetadataSourceDefinition>, IMetadataSourceFactory
    {
        private readonly IMetadataSourceRepository _providerRepository;
        private readonly Logger _logger;

        public MetadataSourceFactory(IMetadataSourceRepository providerRepository,
                                     IEnumerable<IMetadataSource> providers,
                                     IServiceProvider container,
                                     IEventAggregator eventAggregator,
                                     Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _providerRepository = providerRepository;
            _logger = logger;
        }

        protected override void InitializeProviders()
        {
            var definitions = new List<MetadataSourceDefinition>();

            var priority = 1;

            foreach (var provider in _providers)
            {
                var existingDefinitions = provider.DefaultDefinitions
                    .OfType<MetadataSourceDefinition>()
                    .ToList();

                if (existingDefinitions.Any())
                {
                    definitions.AddRange(existingDefinitions);
                }
                else
                {
                    definitions.Add(new MetadataSourceDefinition
                    {
                        Enable = provider.GetType().Name == "SkyHookMetadataSource",
                        Name = provider.Name,
                        Implementation = provider.GetType().Name,
                        Settings = (IProviderConfig)Activator.CreateInstance(provider.ConfigContract),
                        Priority = priority++
                    });
                }
            }

            var currentProviders = All();

            var newProviders = definitions
                .Where(def => currentProviders.All(c => c.Implementation != def.Implementation))
                .ToList();

            if (newProviders.Any())
            {
                _providerRepository.InsertMany(newProviders);
            }
        }

        public override ValidationResult Test(MetadataSourceDefinition definition)
        {
            var result = new ValidationResult();
            var instance = GetInstance(definition);

            try
            {
                result = instance.Test();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test metadata source {0}", definition.Name);
            }

            return result;
        }

        public List<IMetadataSource> EnabledInPriorityOrder()
        {
            return GetAvailableProviders()
                .Where(n => ((MetadataSourceDefinition)n.Definition).Enable)
                .OrderBy(n => ((MetadataSourceDefinition)n.Definition).Priority)
                .ToList();
        }
    }
}
