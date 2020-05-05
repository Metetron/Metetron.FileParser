using System;
using Parsnet.Options;

namespace Parsnet.FileWatchers.Exceptions
{
    public class WatcherIsNotRunningException : Exception
    {
        public WatcherIsNotRunningException(WatcherOptions options) : base($"The watcher for parser \"{options?.ParserName}\" is not started and can therefore not be stopped.")
        {

        }
    }
}
