using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly Regex _filePattern;
        private readonly Regex _subDirectoryPattern;

        public FileCreatedWatcherTask(ILogger<FileCreatedWatcherTask<T>> logger, IWatcherDataRepository watcherRepository, IFileSystem fileSystem, WatcherOptions options)
        {
            _logger = logger;
            _watcherRepository = watcherRepository;
            _fileSystem = fileSystem;
            _options = options;

            _filePattern = new Regex(options.FileSearchPattern);
            _subDirectoryPattern = new Regex(options.SubDirectorySearchPattern);
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

            return GetNewFiles(parserData.LastFileCreationInTicks);
        }

        /// <summary>
        /// Get the new files from the directories that are watchec
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of filesInfos, that have not been parsed yet.</returns>
        private IEnumerable<IFileInfo> GetNewFiles(long lastFileCreationInTicks)
        {
            var newFiles = new List<IFileInfo>();

            var dirInfo = _fileSystem.DirectoryInfo.FromDirectoryName(_options.DirectoryToWatch);

            newFiles.AddRange(GetNewFilesFromSubdirectories(lastFileCreationInTicks));

            newFiles.AddRange(GetNewFilesFromMainDirectory(lastFileCreationInTicks));

            return newFiles;
        }

        /// <summary>
        /// Gets the new files from the subdirectories that are watched
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files that were found in the subdirectories</returns>
        private IEnumerable<IFileInfo> GetNewFilesFromSubdirectories(long lastFileCreationInTicks)
        {
            if (string.IsNullOrWhiteSpace(_options.SubDirectorySearchPattern))
                return Array.Empty<IFileInfo>();

            var filePattern = new Regex(_options.FileSearchPattern);
            var subDirectoryPattern = new Regex(_options.SubDirectorySearchPattern);
            var subDirectories = _fileSystem.DirectoryInfo.FromDirectoryName(_options.DirectoryToWatch)
                .GetDirectories()
                .Where(sd => subDirectoryPattern.IsMatch(sd.Name));

            var newFiles = new List<IFileInfo>();

            foreach (var subDirectory in subDirectories)
            {
                var newFilesInSubdirectory = GetFilesFromDirectory(subDirectory, lastFileCreationInTicks);

                newFiles.AddRange(newFilesInSubdirectory);
            }

            return newFiles;
        }

        /// <summary>
        /// Gets the new files from the main directory that is watched
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files that were found in the main directory</returns>
        private IEnumerable<IFileInfo> GetNewFilesFromMainDirectory(long lastFileCreationInTicks)
        {
            if (!_options.CheckMainDirectory)
                return Array.Empty<IFileInfo>();

            var directory = _fileSystem.DirectoryInfo.FromDirectoryName(_options.DirectoryToWatch);

            return GetFilesFromDirectory(directory, lastFileCreationInTicks);
        }

        /// <summary>
        /// Gets the new files from a single directory
        /// </summary>
        /// <param name="directory">The directory that should be checked for new files</param>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files, that were found in the directory</returns>
        private IEnumerable<IFileInfo> GetFilesFromDirectory(IDirectoryInfo directory, long lastFileCreationInTicks)
        {
            _logger.LogDebug("{ParserName}: Checking directory \"{Directory}\" for new files...", _options.ParserName, directory.FullName);
            var allFiles = directory.GetFiles();

            var newFiles = allFiles.Where(f => _filePattern.IsMatch(f.Name) && f.CreationTimeUtc.Ticks > lastFileCreationInTicks);
            _logger.LogDebug("{ParserName}: Found {NewFilesCount} in directory \"{Directory}\"", _options.ParserName, newFiles.Count(), directory.FullName);

            return newFiles;
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