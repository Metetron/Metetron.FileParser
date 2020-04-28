using System;

namespace Parsnet.FileWatchers.Exceptions
{
    public class WatcherOptionsNotConfiguredException : Exception
    {
        public WatcherOptionsNotConfiguredException() : base("Please configure the watcher options befors starting the watcher")
        {

        }
    }
}