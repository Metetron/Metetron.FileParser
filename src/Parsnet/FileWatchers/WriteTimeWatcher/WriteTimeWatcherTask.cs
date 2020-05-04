using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parsnet.Abstractions;
using Parsnet.FileWatchers.WriteTimeWatcher.Data;

namespace Parsnet.FileWatchers.WriteTimeWatcher
{
    public class WriteTimeWatcherTask<T> : WatcherBase<T> where T : IParser, new()
    {
        private readonly IWriteTimeWatcherRepository _repository;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<WriteTimeWatcherTask<T>> _logger;

        public WriteTimeWatcherTask(IWriteTimeWatcherRepository repository, IFileSystem fileSystem, IFileQueue queue, ILogger<WriteTimeWatcherTask<T>> logger) : base(queue, logger)
        {
            _repository = repository;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public override async Task<IEnumerable<IFileInfo>> CheckForFilesToParseAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{ParserName}: Checking whether there are files that have new data or not...", Options.ParserName);

            var parserData = await GetOrCreateWatcherDataAsync();

            var filesToParse = GetFilesWithNewData(parserData.LastWriteTimeUtc);

            if (filesToParse.Any())
                await _repository.UpdateLastWriteTimeAsync(Options.ParserName, filesToParse.Max(f => f.LastWriteTimeUtc.Ticks));

            return filesToParse;
        }

        private async Task<WriteTimeWatcherData> GetOrCreateWatcherDataAsync()
        {
            var parserData = await _repository.GetWatcherDataAsync(Options.ParserName);

            if (parserData != null)
                return parserData;

            return await CreateWatcherDataAsync();
        }

        private async Task<WriteTimeWatcherData> CreateWatcherDataAsync()
        {
            _logger.LogDebug("{ParserName}: Data not found in database. Creating it from scratch...", Options.ParserName);

            var watcherData = new WriteTimeWatcherData { ParserName = Options.ParserName, LastWriteTimeUtc = DateTime.MinValue.ToUniversalTime().Ticks };
            await _repository.AddWatcherDataAsync(watcherData);

            return watcherData;
        }

        public IEnumerable<IFileInfo> GetFilesWithNewData(long lastWriteTimeTicks)
        {
            var filesWithNewData = new List<IFileInfo>();

            filesWithNewData.AddRange(GetFilesWithNewDataFromSubDirectories(lastWriteTimeTicks));
            filesWithNewData.AddRange(GetFilesWithNewDataFromMainDirectory(lastWriteTimeTicks));

            return filesWithNewData;
        }

        private IEnumerable<IFileInfo> GetFilesWithNewDataFromSubDirectories(long lastWriteTimeTicks)
        {
            if (Options.SubDirectorySearchPattern == null)
                return Array.Empty<IFileInfo>();

            var mainDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(Options.DirectoryToWatch);

            var subDirectories = mainDirectory
                .GetDirectories()
                .Where(sd => Regex.IsMatch(sd.Name, Options.SubDirectorySearchPattern));

            return subDirectories.SelectMany(sd => GetFilesFromDirectory(sd, lastWriteTimeTicks));
        }

        private IEnumerable<IFileInfo> GetFilesWithNewDataFromMainDirectory(long lastWriteTimeTicks)
        {
            if (!Options.CheckMainDirectory && Options.SubDirectorySearchPattern != null)
                return Array.Empty<IFileInfo>();

            var mainDirectory = _fileSystem.DirectoryInfo.FromDirectoryName(Options.DirectoryToWatch);

            return GetFilesFromDirectory(mainDirectory, lastWriteTimeTicks);
        }

        private IEnumerable<IFileInfo> GetFilesFromDirectory(IDirectoryInfo directory, long lastWriteTimeTicks)
        {
            _logger.LogDebug("{ParserName}: Checking directory \"{Directory}\" for files with new data...", Options.ParserName, directory.FullName);
            var allFiles = directory.GetFiles();

            var filesWithNewData = allFiles.Where(f => Regex.IsMatch(f.Name, Options.FileSearchPattern)
                && f.LastWriteTimeUtc.Ticks > lastWriteTimeTicks);

            _logger.LogDebug("{ParserName}: Found {FileCount} files with new data in directory \"{Directory}\"", Options.ParserName, filesWithNewData.Count(), directory.FullName);
            return filesWithNewData;
        }
    }
}
