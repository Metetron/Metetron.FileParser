using System.Threading;
using Parsnet.FileWatchers.Exceptions;
using Parsnet.WatcherConfiguration;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO.Abstractions;
using Parsnet.Abstractions;
using System.Linq;

namespace Parsnet.FileWatchers
{
    public abstract class WatcherBase<T> where T : IParser, new()
    {
        public WatcherOptions Options { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly CancellationTokenSource _tokenSource;
        private readonly ILogger _logger;
        private readonly IFileQueue _queue;
        private Task _watcherTask;

        public WatcherBase(IFileQueue queue, ILogger logger)
        {
            _tokenSource = new CancellationTokenSource();
            _queue = queue;
            _logger = logger;
        }

        /// <summary>
        /// Check the directories for files that need to be parsed
        /// </summary>
        /// <param name="cancellationToken">Token to cancel async operations</param>
        /// <returns>A list of files that are ready to be parsed</returns>
        public abstract Task<IEnumerable<IFileInfo>> CheckForFilesToParseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Main Loop for the watcher. Checks for files that need to be parsed periodically and submits them to the file queue
        /// </summary>
        private void ParserLoop()
        {
            Task.Run(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var files = await CheckForFilesToParseAsync(_tokenSource.Token);

                        if (files.Any())
                            _queue.EnqueueNewFilesForProcessing<T>(Options, files.ToList());
                        else
                            _logger.LogInformation("{ParserName}: Found no new files...", Options.ParserName);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "{ParserName}: Exception occured while checking for new files.", Options.ParserName);
                    }
                    await Task.Delay(Options.PollingInterval, _tokenSource.Token);
                }
            });
        }

        /// <summary>
        /// Start the watcher task
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                throw new WatcherIsAlreadyRunningException(Options);

            if (Options == null)
                throw new WatcherOptionsNotConfiguredException();

            _watcherTask = new Task(() => ParserLoop(), _tokenSource.Token, TaskCreationOptions.LongRunning);
            _watcherTask.Start();
            IsRunning = true;
        }

        /// <summary>
        /// Stop the watcher task
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                throw new WatcherIsNotRunningException(Options);

            _tokenSource.Cancel();
            IsRunning = false;
        }

        /// <summary>
        /// Set the options for the watcher
        /// </summary>
        /// <param name="options">The options which should be used by the watcher</param>
        public void SetOptions(WatcherOptions options)
        {
            if (IsRunning)
                throw new WatcherIsAlreadyRunningException(Options);

            Options = options;
        }
    }
}