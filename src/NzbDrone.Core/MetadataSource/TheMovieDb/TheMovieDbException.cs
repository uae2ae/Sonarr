using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.MetadataSource.TheMovieDb
{
    public class TheMovieDbException : NzbDroneException
    {
        public TheMovieDbException(string message, params object[] args)
            : base(message, args)
        {
        }

        public TheMovieDbException(string message, System.Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
