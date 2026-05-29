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

            // Insert default sources: TheTVDB (priority 1, enabled), TVmaze (priority 2, enabled), TheMovieDB (priority 3, disabled until API key is set)
            Insert.IntoTable("MetadataSources").Row(new
            {
                Name = "TheTVDB",
                Implementation = "SkyHookMetadataSource",
                Settings = "{}",
                ConfigContract = "NullConfig",
                Enable = true,
                Priority = 1
            });

            Insert.IntoTable("MetadataSources").Row(new
            {
                Name = "TVmaze",
                Implementation = "TvMazeProxy",
                Settings = "{}",
                ConfigContract = "NullConfig",
                Enable = true,
                Priority = 2
            });

            Insert.IntoTable("MetadataSources").Row(new
            {
                Name = "TheMovieDB",
                Implementation = "TheMovieDbProxy",
                Settings = "{\"ApiKey\":\"\"}",
                ConfigContract = "TheMovieDbSettings",
                Enable = false,
                Priority = 3
            });
        }
    }
}
