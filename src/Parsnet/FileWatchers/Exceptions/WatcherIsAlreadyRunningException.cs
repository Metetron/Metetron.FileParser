using System;
using Parsnet.Options;

namespace Parsnet.FileWatchers.Exceptions
{
    public class WatcherIsAlreadyRunningException : Exception
    {
        public WatcherIsAlreadyRunningException(WatcherOptions options) : base($"The parser \"{options?.ParserName}\" is already started.")
        {

        }
    }
}
