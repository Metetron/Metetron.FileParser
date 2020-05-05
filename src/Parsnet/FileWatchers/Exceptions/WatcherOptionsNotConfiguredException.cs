using System;

namespace Parsnet.FileWatchers.Exceptions
{
    public class WatcherOptionsNotConfiguredException : Exception
    {
        public WatcherOptionsNotConfiguredException() : base("Please configure the watcher options before starting the watcher")
        {

        }
    }
}
