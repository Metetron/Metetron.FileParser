using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hangfire;
using Metetron.FileParser.Abstractions;
using Metetron.FileParser.FileTasks;
using Metetron.FileParser.WatcherConfiguration;
using Microsoft.Extensions.Logging;

namespace Metetron.FileParser.FileCreatedWatcher
{
    public class FileCreatedWatcherTask<T> where T : IParser, new()
    {
        private readonly ILogger<FileCreatedWatcherTask<T>> _logger;
        private readonly IWatcherDataRepository _watcherRepository;
        private readonly WatcherOptions _options;
        private readonly IFileChecker _fileChecker;
        private readonly IMapper _mapper;
        private readonly IFileWorker _fileWorker;

        public FileCreatedWatcherTask(ILogger<FileCreatedWatcherTask<T>> logger, IWatcherDataRepository watcherRepository, WatcherOptions options, IFileChecker fileChecker, IMapper mapper, IFileWorker fileWorker)
        {
            _logger = logger;
            _watcherRepository = watcherRepository;
            _options = options;
            _fileChecker = fileChecker;
            _mapper = mapper;
            _fileWorker = fileWorker;
        }

        /// <summary>
        /// Check the directories for new files
        /// </summary>
        /// <returns>A list of new files, that have not been parsed yet</returns>
        private async Task<IEnumerable<IFileInfo>> CheckForNewFiles()
        {
            _logger.LogInformation("{ParserName}: Checking whether there are new files or not...", _options.ParserName);

            var parserData = await GetWatcherDataAsync();
            _logger.LogDebug("{ParserName}: Got parser data from database...", _options.ParserName);

            var checkOptions = _mapper.Map<FileCheckOptions>(_options);
            checkOptions.LastCreationTimeInTicks = parserData.LastFileCreationInTicks;
            var newFiles = _fileChecker.GetNewlyCreatedFiles(checkOptions);

            parserData.LastFileCreationInTicks = newFiles.Max(f => f.CreationTimeUtc.Ticks);
            await _watcherRepository.UpdateWatcherDataAsync(parserData);

            return newFiles;
        }

        /// <summary>
        /// Get the stored data of the parser or create it, if ot does not exist
        /// </summary>
        /// <returns>The watcher data</returns>
        private async Task<WatcherData> GetWatcherDataAsync()
        {
            var parserData = await _watcherRepository.GetWatcherDataAsync(_options.ParserName);

            if (parserData != null)
                return parserData;

            _logger.LogDebug("{ParserName}: Data not found in database. Create it from scratch...", _options.ParserName);
            parserData = new WatcherData { ParserName = _options.ParserName, LastFileCreationInTicks = DateTime.MinValue.Ticks };

            await _watcherRepository.InsertWatcherDataAsync(parserData);

            return parserData;
        }

        /// <summary>
        /// Loop that check for new files and queues them up
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the loop</param>
        private void ParserLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var files = await CheckForNewFiles();
                        _fileWorker.EnqueueNewFilesForProcessing<T>(_options, files.ToList());
                    }, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "{ParserName}: Exception occurred while checking for new files");
                }
                Task.Run(async () => await Task.Delay(_options.PollingInterval), cancellationToken);
            }
        }

        /// <summary>
        /// Used for starting the parser thread
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        private void Start(CancellationToken cancellationToken)
        {
            var task = new Task(() => ParserLoop(cancellationToken), cancellationToken, TaskCreationOptions.LongRunning);
            task.Start();
        }
    }
}