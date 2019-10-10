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
        private readonly IFileSystem _fileSystem;
        private readonly WatcherOptions _options;
        private readonly IFileChecker _fileChecker;
        private readonly IMapper _mapper;

        public FileCreatedWatcherTask(ILogger<FileCreatedWatcherTask<T>> logger, IWatcherDataRepository watcherRepository, IFileSystem fileSystem, WatcherOptions options, IFileChecker fileChecker, IMapper mapper)
        {
            _logger = logger;
            _watcherRepository = watcherRepository;
            _fileSystem = fileSystem;
            _options = options;
            _fileChecker = fileChecker;
            this._mapper = mapper;
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
            return _fileChecker.GetNewlyCreatedFiles(checkOptions);
        }



        /// <summary>
        /// Enqueue jobs for each file in HangFire
        /// </summary>
        /// <param name="files">The files to enqueue</param>
        private void EnqueueFiles(IList<IFileInfo> files)
        {
            foreach (var file in files)
            {
                var workingPath = $"{_options.WorkingDirectoryPath}\\{Guid.NewGuid()}\\{file.Name}";
                var backupPath = $"{_options.BackupDirectoryPath}\\{DateTime.Today:yyyy}\\{DateTime.Today:MMMM}";

                var copyJobId = BackgroundJob.Enqueue(() => CopyTask.CopyFileToDirectory(file.FullName, _options.WorkingDirectoryPath));
                var parserJobId = BackgroundJob.ContinueJobWith(copyJobId, () => new T().ParseFile(workingPath));
                var backupJobId = BackgroundJob.ContinueJobWith(parserJobId, () => CopyTask.CopyFileToDirectory(workingPath, backupPath));
                var cleanupJobId = BackgroundJob.ContinueJobWith(backupJobId, () => CleanupTask.DeleteFile(workingPath));

                if (_options.DeleteSourceFileAfterParsing)
                    BackgroundJob.ContinueJobWith(cleanupJobId, () => CleanupTask.DeleteFile(file.FullName));
            }

            _logger.LogDebug("{ParserName}: Enqueued {NewFilesCount} files for parsing...", _options.ParserName, files.Count);
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
                    Task.Run(async () => await CheckForNewFiles(), cancellationToken);
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