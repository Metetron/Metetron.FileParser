using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Metetron.FileParser.WatcherConfiguration;
using Microsoft.Extensions.Logging;

namespace Metetron.FileParser.FileTasks
{
    public class FileChecker : IFileChecker
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<FileChecker> _logger;

        public FileChecker(IFileSystem fileSystem, ILogger<FileChecker> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public IEnumerable<IFileInfo> GetNewlyCreatedFiles(FileCheckOptions options)
        {
            var newFiles = new List<IFileInfo>();

            newFiles.AddRange(GetNewFilesFromSubdirectories(options));

            newFiles.AddRange(GetNewFilesFromMainDirectory(options));

            return newFiles;
        }

        /// <summary>
        /// Gets the new files from the subdirectories that are watched
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files that were found in the subdirectories</returns>
        private IEnumerable<IFileInfo> GetNewFilesFromSubdirectories(FileCheckOptions options)
        {
            if (options.SubDirectorySearchPattern == null)
                return Array.Empty<IFileInfo>();

            var subDirectories = options.MainDirectory
                .GetDirectories()
                .Where(sd => options.SubDirectorySearchPattern.IsMatch(sd.Name));

            var newFiles = new List<IFileInfo>();

            foreach (var subDirectory in subDirectories)
            {
                var newFilesInSubdirectory = GetFilesFromDirectory(options, subDirectory);

                newFiles.AddRange(newFilesInSubdirectory);
            }

            return newFiles;
        }

        /// <summary>
        /// Gets the new files from the main directory that is watched
        /// </summary>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files that were found in the main directory</returns>
        private IEnumerable<IFileInfo> GetNewFilesFromMainDirectory(FileCheckOptions options)
        {
            if (!options.CheckMainDirectory)
                return Array.Empty<IFileInfo>();

            return GetFilesFromDirectory(options, options.MainDirectory);
        }

        /// <summary>
        /// Gets the new files from a single directory
        /// </summary>
        /// <param name="directory">The directory that should be checked for new files</param>
        /// <param name="lastFileCreationInTicks">The UTC creation time of the last matching file in ticks</param>
        /// <returns>A list of new files, that were found in the directory</returns>
        private IEnumerable<IFileInfo> GetFilesFromDirectory(FileCheckOptions options, IDirectoryInfo directory)
        {
            _logger.LogDebug("{ParserName}: Checking directory \"{Directory}\" for new files...", options.ParserName, directory.FullName);
            var allFiles = directory.GetFiles();

            var newFiles = allFiles.Where(f => options.FileSearchPattern.IsMatch(f.Name) && f.CreationTimeUtc.Ticks > options.LastCreationTimeInTicks);
            _logger.LogDebug("{ParserName}: Found {NewFilesCount} in directory \"{Directory}\"", options.ParserName, newFiles.Count(), directory.FullName);

            return newFiles;
        }
    }
}