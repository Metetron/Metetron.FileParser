using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parsnet.DependencyInjection;
using Parsnet.Options;
using Parsnet.WorkerService;

namespace CreationTimeWatcherExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddConsole());
            var services = ServiceCollectionFactory.CreateDefaultServiceCollection(loggerFactory);
            var provider = services.BuildServiceProvider();

            var options = new WatcherOptions()
            {
                DirectoryToWatch = "D:\\ParserTest",
                FileSearchPattern = "hello*.cs",
                WorkingDirectoryPath = "D:\\WorkingDir",
                BackupDirectoryPath = "D:\\BackupDir",
                ParserName = "Parser_One",
                PollingInterval = 5000,
                DeleteSourceFileAfterParsing = false
            };

            var worker = provider.GetRequiredService<ParserWorker>();

            var parserId = worker.RegisterCreationTimeParser<Logic>(options);
            worker.StartParser(parserId);

            await Task.Delay(60000);

            worker.StopParser(parserId);
        }
    }
}
