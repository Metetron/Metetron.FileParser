using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly WatcherOptions _options;

        private readonly Regex _filePattern;
        private readonly Regex _subDirectoryPattern;

        public FileCreatedWatcherTask(ILogger<FileCreatedWatcherTask<T>> logger, IWatcherDataRepository watcherRepository, WatcherOptions options)
        {
            this._logger = logger;
            this._watcherRepository = watcherRepository;
            this._options = options;

            _filePattern = new Regex(options.FileSearchPattern);
            _subDirectoryPattern = new Regex(options.SubDirectorySearchPattern);
        }

        /// <summary>
        /// Check the directories for new files
        /// </summary>
        /// <returns>A list of new files, that have not been parsed yet</returns>
        private async Task<IEnumerable<FileInfo>> CheckForNewFiles()
        {
            _logger.LogDebug("{ParserName}: Checking whether there are new files...", _options.ParserName);

            var parserData = await GetWatcherDataAsync();

            return GetNewFiles(parserData.LastFileCreationInTicks);
        }

        /// <summary>
        /// Get the new files from the directories that are watchec
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files, that have not been parsed yet.</returns>
        private IEnumerable<FileInfo> GetNewFiles(long lastFileCreationInTicks)
        {
            var newFiles = new List<FileInfo>();

            var dirInfo = new DirectoryInfo(_options.DirectoryToWatch);

            newFiles.AddRange(GetNewFilesFromSubdirectories(lastFileCreationInTicks));

            newFiles.AddRange(GetNewFilesFromMainDirectory(lastFileCreationInTicks));

            return newFiles;
        }

        /// <summary>
        /// Gets the new files from the subdirectories that are watched
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files that were found in the subdirectories</returns>
        private IEnumerable<FileInfo> GetNewFilesFromSubdirectories(long lastFileCreationInTicks)
        {
            if (string.IsNullOrWhiteSpace(_options.SubDirectorySearchPattern))
                return Array.Empty<FileInfo>();

            var filePattern = new Regex(_options.FileSearchPattern);
            var subDirectoryPattern = new Regex(_options.SubDirectorySearchPattern);
            var subDirectories = new DirectoryInfo(_options.DirectoryToWatch)
                .GetDirectories()
                .Where(sd => subDirectoryPattern.IsMatch(sd.Name));

            var newFiles = new List<FileInfo>();

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
        private IEnumerable<FileInfo> GetNewFilesFromMainDirectory(long lastFileCreationInTicks)
        {
            if (!_options.CheckMainDirectory)
                return Array.Empty<FileInfo>();

            return GetFilesFromDirectory(new DirectoryInfo(_options.DirectoryToWatch), lastFileCreationInTicks);
        }

        /// <summary>
        /// Gets the new files from a single directory
        /// </summary>
        /// <param name="directory">The directory that should be checked for new files</param>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files, that were found in the directory</returns>
        private IEnumerable<FileInfo> GetFilesFromDirectory(DirectoryInfo directory, long lastFileCreationInTicks)
        {
            var allFiles = directory.GetFiles();

            return allFiles.Where(f => _filePattern.IsMatch(f.Name) && f.CreationTimeUtc.Ticks > lastFileCreationInTicks);
        }

        /// <summary>
        /// Enqueue jobs for each file in HangFire
        /// </summary>
        /// <param name="files">The files to enqueue</param>
        private void EnqueueFiles(IEnumerable<FileInfo> files)
        {
            foreach (var file in files)
            {
                var workingPath = $"{_options.WorkingDirectoryPath}\\{Guid.NewGuid()}\\{file.Name}";
                var backupPath = $"{_options.BackupDirectoryPath}\\{DateTime.Today:yyyy}\\{DateTime.Today:MMMM}";

                var copyJobId = BackgroundJob.Enqueue(() => CopyTask.CopyFileToDirectory(file.FullName, _options.WorkingDirectoryPath));
                var parserJobId = BackgroundJob.ContinueJobWith(copyJobId, () => new T().ParseFile(workingPath));
                var backupJobId = BackgroundJob.ContinueJobWith(parserJobId, () => CopyTask.CopyFileToDirectory(workingPath, backupPath));
                var cleanupJobId = BackgroundJob.ContinueJobWith(backupJobId, () => CleanupTask.DeleteFile(workingPath));

                if (_options.DeletesourceFileAfterParsing)
                    BackgroundJob.ContinueJobWith(cleanupJobId, () => CleanupTask.DeleteFile(file.FullName));
            }
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

            parserData = new WatcherData { ParserName = _options.ParserName, LastFileCreationInTicks = DateTime.MinValue.Ticks };

            await _watcherRepository.InsertWatcherDataAsync(parserData);

            return parserData;
        }
    }
}