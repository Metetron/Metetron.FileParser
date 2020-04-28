using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parsnet.Abstractions;
using Parsnet.FileWatchers.CreationTimeWatcher.Data;
using Parsnet.WatcherConfiguration;

namespace Parsnet.FileWatchers.CreationTimeWatcher
{
    public class CreationTimeWatcherTask<T> : WatcherBase<T> where T : IParser, new()
    {
        private readonly ICreationTimeWatcherRepository _repository;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<CreationTimeWatcherTask<T>> _logger;

        public CreationTimeWatcherTask(ICreationTimeWatcherRepository repository, IFileSystem fileSystem, IFileQueue queue, ILogger<CreationTimeWatcherTask<T>> logger) : base(queue, logger)
        {
            _repository = repository;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public override async Task<IEnumerable<IFileInfo>> CheckForFilesToParseAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{ParserName}: Checking whether there are new files or not...", Options.ParserName);

            var parserData = await GetOrCreateWatcherDataAsync();

            var newFiles = GetNewlyCreatedFiles(parserData.LastCreationTimeUtc);

            if (newFiles.Any())
            {
                await _repository.UpdateLastCreationTimeAsync(Options.ParserName, newFiles.Max(f => f.CreationTimeUtc.Ticks));
            }

            return newFiles;
        }

        private async Task<CreationTimeWatcherData> GetOrCreateWatcherDataAsync()
        {
            var parserData = await _repository.GetWatcherDataAsync(Options.ParserName);

            if (parserData != null)
                return parserData;

            return await CreateWatcherDataAsync();
        }

        private async Task<CreationTimeWatcherData> CreateWatcherDataAsync()
        {
            _logger.LogDebug("{ParserName}: Data not found in database. Creating it from scratch...", Options.ParserName);

            var watcherData = new CreationTimeWatcherData { ParserName = Options.ParserName, LastCreationTimeUtc = DateTime.MinValue.ToUniversalTime().Ticks };
            await _repository.AddWatcherDataAsync(watcherData);

            return watcherData;
        }

        private IEnumerable<IFileInfo> GetNewlyCreatedFiles(long lastCreatedFileTicks)
        {
            var newFiles = new List<IFileInfo>();

            newFiles.AddRange(GetNewFilesFromSubDirectories(lastCreatedFileTicks));

            newFiles.AddRange(GetNewFilesFromMainDirectory(lastCreatedFileTicks));

            return newFiles;
        }

        private IEnumerable<IFileInfo> GetNewFilesFromSubDirectories(long lastCreatedFileTicks)
        {
            if (Options.SubDirectorySearchPattern == null)
                return Array.Empty<IFileInfo>();

            var mainDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(Options.DirectoryToWatch);

            var subDirectories = mainDirectory
                .GetDirectories()
                .Where(sd => Regex.IsMatch(sd.Name, Options.SubDirectorySearchPattern));

            return subDirectories.SelectMany(subDir => GetFilesFromDirectory(subDir, lastCreatedFileTicks));
        }

        private IEnumerable<IFileInfo> GetNewFilesFromMainDirectory(long lastCreatedFileTicks)
        {
            if (!Options.CheckMainDirectory && Options.SubDirectorySearchPattern != null)
                return Array.Empty<IFileInfo>();

            var mainDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(Options.DirectoryToWatch);
            return GetFilesFromDirectory(mainDirectory, lastCreatedFileTicks);
        }

        private IEnumerable<IFileInfo> GetFilesFromDirectory(IDirectoryInfo directory, long lastCreatedFileTicks)
        {
            _logger.LogDebug("{ParserName}: Checking directory \"{Directory}\" for new files...", Options.ParserName, directory.FullName);
            var allFiles = directory.GetFiles();

            var newFiles = allFiles.Where(f => Regex.IsMatch(f.Name, Options.FileSearchPattern)
                && f.CreationTimeUtc.Ticks > lastCreatedFileTicks);

            _logger.LogDebug("{ParserName}: Found {NewFilesCount} files in directory \"{Directory}\"", Options.ParserName, newFiles.Count(), directory.FullName);

            return newFiles;
        }
    }
}