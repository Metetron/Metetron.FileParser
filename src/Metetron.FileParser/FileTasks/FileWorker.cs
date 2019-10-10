using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Hangfire;
using Metetron.FileParser.Abstractions;
using Metetron.FileParser.WatcherConfiguration;
using Microsoft.Extensions.Logging;

namespace Metetron.FileParser.FileTasks
{
    public class FileWorker : IFileWorker
    {
        private readonly ILogger<FileWorker> _logger;

        public FileWorker(ILogger<FileWorker> logger)
        {
            _logger = logger;
        }

        public void EnqueueNewFilesForProcessing<T>(WatcherOptions options, IList<IFileInfo> newFiles) where T : IParser, new()
        {
            foreach (var file in newFiles)
            {
                var workingPath = $"{options.WorkingDirectoryPath}\\{Guid.NewGuid()}\\{file.Name}";
                var backupPath = $"{options.BackupDirectoryPath}\\{DateTime.Today:yyyy}\\{DateTime.Today:MMMM}";

                var copyJobId = BackgroundJob.Enqueue(() => CopyTask.CopyFileToDirectory(file.FullName, options.WorkingDirectoryPath));
                var parserJobId = BackgroundJob.ContinueJobWith(copyJobId, () => new T().ParseFile(workingPath));
                var backupJobId = BackgroundJob.ContinueJobWith(parserJobId, () => CopyTask.CopyFileToDirectory(workingPath, backupPath));
                var cleanupJobId = BackgroundJob.ContinueJobWith(backupJobId, () => CleanupTask.DeleteFile(workingPath));

                if (options.DeleteSourceFileAfterParsing)
                    BackgroundJob.ContinueJobWith(cleanupJobId, () => CleanupTask.DeleteFile(file.FullName));
            }

            _logger.LogDebug("{ParserName}: Enqueued {NewFilesCount} files for parsing...", options.ParserName, newFiles.Count);
        }
    }
}