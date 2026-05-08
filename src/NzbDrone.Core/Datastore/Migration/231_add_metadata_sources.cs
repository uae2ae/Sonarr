using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(231)]
    public class add_metadata_sources : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("MetadataSources")
                  .WithColumn("Name").AsString().NotNullable().Unique()
                  .WithColumn("Implementation").AsString().NotNullable()
                  .WithColumn("Settings").AsString().Nullable()
                  .WithColumn("ConfigContract").AsString().Nullable()
                  .WithColumn("Enable").AsBoolean().NotNullable().WithDefaultValue(true)
                  .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(1);

            // Insert the default SkyHook (TheTVDB) source
            Insert.IntoTable("MetadataSources").Row(new
            {
                Name = "TheTVDB",
                Implementation = "SkyHookMetadataSource",
                Settings = "{}",
                ConfigContract = "NullConfig",
                Enable = true,
                Priority = 1
            });
        }
    }
}
