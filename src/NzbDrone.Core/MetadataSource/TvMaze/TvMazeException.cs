using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.MetadataSource.TvMaze
{
    public class TvMazeException : NzbDroneException
    {
        public TvMazeException(string message, params object[] args)
            : base(message, args)
        {
        }

        public TvMazeException(string message, System.Exception innerException, params object[] args)
            : base(message, innerException, args)
        {
        }
    }
}
