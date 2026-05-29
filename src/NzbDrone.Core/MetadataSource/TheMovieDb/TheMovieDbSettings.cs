using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.MetadataSource.TheMovieDb
{
    public class TheMovieDbSettingsValidator : AbstractValidator<TheMovieDbSettings>
    {
        public TheMovieDbSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty().WithMessage("An API key is required for TheMovieDB");
        }
    }

    public class TheMovieDbSettings : IProviderConfig
    {
        private static readonly TheMovieDbSettingsValidator Validator = new ();

        public TheMovieDbSettings()
        {
            ApiKey = string.Empty;
        }

        [FieldDefinition(0, Label = "API Key", HelpText = "Your TheMovieDB API key. You can obtain one at https://www.themoviedb.org/settings/api", Type = FieldType.Password)]
        public string ApiKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
