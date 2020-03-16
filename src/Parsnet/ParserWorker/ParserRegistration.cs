using System.Threading;
using Parsnet.Abstractions;
using Parsnet.WatcherConfiguration;

namespace Parsnet.ParserWorker
{
    public class ParserRegistration
    {
        private readonly CancellationTokenSource _tokenSource;

        public WatcherOptions ParserOptions { get; }
        public IFileWatcher Watcher { get; }
        public CancellationToken CancellationToken => _tokenSource.Token;

        public ParserRegistration(WatcherOptions options, IFileWatcher watcher)
        {
            ParserOptions = options;
            Watcher = watcher;
            _tokenSource = new CancellationTokenSource();
        }

        public void CancelRegistration()
        {
            _tokenSource.Cancel();
        }
    }
}
