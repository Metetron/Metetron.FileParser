using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Hangfire;
using Parsnet.Abstractions;
using Parsnet.WatcherConfiguration;
using Microsoft.Extensions.Logging;

namespace Parsnet.FileTasks
{
    public class HangFireFileQueue : IFileQueue
    {
        private readonly ILogger<HangFireFileQueue> _logger;

        public HangFireFileQueue(ILogger<HangFireFileQueue> logger)
        {
            _logger = logger;
        }

        public void EnqueueNewFilesForProcessing<T>(WatcherOptions options, IList<IFileInfo> newFiles) where T : IParser, new()
        {
            foreach (var file in newFiles)
            {
                var workingPath = $"{options.WorkingDirectoryPath}\\{Guid.NewGuid()}";
                var workingFile = $"{workingPath}\\{file.Name}";
                var backupPath = $"{options.BackupDirectoryPath}\\{DateTime.Today:yyyy}\\{DateTime.Today:MMMM}";

                var copyJobId = BackgroundJob.Enqueue(() => CopyTask.CopyFileToDirectory(file.FullName, workingPath));
                var parserJobId = BackgroundJob.ContinueJobWith(copyJobId, () => new T().ParseFile(workingFile));
                var backupJobId = BackgroundJob.ContinueJobWith(parserJobId, () => CopyTask.CopyFileToDirectory(workingFile, backupPath));
                var cleanupJobId = BackgroundJob.ContinueJobWith(backupJobId, () => CleanupTask.DeleteFolder(workingPath));

                if (options.DeleteSourceFileAfterParsing)
                    BackgroundJob.ContinueJobWith(cleanupJobId, () => CleanupTask.DeleteFile(file.FullName));
            }

            _logger.LogInformation("{ParserName}: Enqueued {NewFilesCount} files for parsing...", options.ParserName, newFiles.Count);
        }
    }
}