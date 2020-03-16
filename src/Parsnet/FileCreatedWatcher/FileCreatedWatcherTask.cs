using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Parsnet.Abstractions;
using Parsnet.Options;
using Parsnet.WatcherConfiguration;
using Microsoft.Extensions.Logging;

namespace Parsnet.FileCreatedWatcher
{
    public class FileCreatedWatcherTask<T> : IFileWatcher where T : IParser, new()
    {
        private readonly ILogger<FileCreatedWatcherTask<T>> _logger;
        private readonly IWatcherDataRepository _watcherRepository;
        private readonly IFileChecker _fileChecker;
        private readonly IMapper _mapper;
        private readonly IFileQueue _fileWorker;
        private readonly CancellationTokenSource _tokenSource;
        private bool _isWatcherRunning = false;

        public WatcherOptions Options { get; private set; }

        public FileCreatedWatcherTask(ILogger<FileCreatedWatcherTask<T>> logger, IWatcherDataRepository watcherRepository, IFileChecker fileChecker, IMapper mapper, IFileQueue fileWorker)
        {
            _logger = logger;
            _watcherRepository = watcherRepository;
            _fileChecker = fileChecker;
            _mapper = mapper;
            _fileWorker = fileWorker;
            _tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Check the directories for new files
        /// </summary>
        /// <returns>A list of new files, that have not been parsed yet</returns>
        private async Task<IEnumerable<IFileInfo>> CheckForNewFilesAsync()
        {
            _logger.LogInformation("{ParserName}: Checking whether there are new files or not...", Options.ParserName);

            var parserData = await GetWatcherDataAsync();
            _logger.LogDebug("{ParserName}: Got parser data from database...", Options.ParserName);

            var checkOptions = _mapper.Map<FileCheckOptions>(Options);
            checkOptions.LastCreationTimeInTicks = parserData.LastFileCreationInTicks;
            var newFiles = _fileChecker.GetNewlyCreatedFiles(checkOptions);

            if (newFiles.Any())
            {
                parserData.LastFileCreationInTicks = newFiles.Max(f => f.CreationTimeUtc.Ticks);
                await _watcherRepository.UpdateWatcherDataAsync(parserData);
            }

            return newFiles;
        }

        /// <summary>
        /// Get the stored data of the parser or create it, if ot does not exist
        /// </summary>
        /// <returns>The watcher data</returns>
        private async Task<WatcherData> GetWatcherDataAsync()
        {
            var parserData = await _watcherRepository.GetWatcherDataAsync(Options.ParserName);

            if (parserData != null)
                return parserData;

            _logger.LogDebug("{ParserName}: Data not found in database. Create it from scratch...", Options.ParserName);
            parserData = new WatcherData { ParserName = Options.ParserName, LastFileCreationInTicks = DateTime.MinValue.Ticks };

            await _watcherRepository.InsertWatcherDataAsync(parserData);

            return parserData;
        }

        /// <summary>
        /// Loop that check for new files and queues them up
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the loop</param>
        private void ParserLoop(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var files = await CheckForNewFilesAsync();

                        if (files.Any())
                        {
                            _fileWorker.EnqueueNewFilesForProcessing<T>(Options, files.ToList());
                        }
                        else
                        {
                            _logger.LogInformation("{ParserName}: Found no new files...", Options.ParserName);
                        }

                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "{ParserName}: Exception occurred while checking for new files", Options.ParserName);
                    }
                    await Task.Delay(Options.PollingInterval);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Used for starting the parser thread
        /// </summary>
        public void Start()
        {
            if (_isWatcherRunning)
                throw new WatcherIsAlreadyRunningException();

            if (Options == null)
                throw new ArgumentNullException(nameof(Options), "Please set the watcher options before starting the watcher");

            var task = new Task(() => ParserLoop(_tokenSource.Token), _tokenSource.Token, TaskCreationOptions.LongRunning);
            task.Start();
            _isWatcherRunning = true;
        }

        /// <summary>
        /// Stops the parser thread
        /// </summary>
        public void Stop()
        {
            if (!_isWatcherRunning)
                throw new WatcherIsAlreadyStoppedException();

            _tokenSource.Cancel();
            _isWatcherRunning = false;
        }

        /// <summary>
        /// Sets the options for the watcher
        /// </summary>
        /// <param name="options">The options</param>
        public void SetOptions(WatcherOptions options)
        {
            if (_isWatcherRunning)
                throw new WatcherIsAlreadyRunningException();

            Options = options;
        }
    }
}
