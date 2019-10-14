using Metetron.FileParser.FileCreatedWatcher;
using Metetron.FileParser.FileTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Metetron.FileParser
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFileParser(this IServiceCollection services)
        {
            services.AddTransient<IFileWorker, FileWorker>();
            services.AddTransient<IFileChecker, FileChecker>();
            services.AddTransient<IWatcherDataRepository, WatcherDataRepository>();
            services.AddTransient(typeof(FileCreatedWatcherTask<>));
        }
    }
}