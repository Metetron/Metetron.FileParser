using Parsnet.Options;

namespace Parsnet.Abstractions
{
    public interface IFileWatcher
    {
        WatcherOptions Options { get; }
        void SetOptions(WatcherOptions options);
        void Start();
        void Stop();
    }
}
