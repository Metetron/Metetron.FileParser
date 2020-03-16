using Metetron.FileParser.Abstractions;
using Metetron.FileParser.FileCreatedWatcher;
using Metetron.FileParser.FileTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Metetron.FileParser
{
    public static class ServiceCollectionExtensions
    {
        public static void AddFileParser(this IServiceCollection services)
        {
            services.UseHangFireFileQueue();
            services.AddTransient<IFileChecker, FileChecker>();
            services.AddTransient<IWatcherDataRepository, WatcherDataRepository>();
            services.AddTransient(typeof(FileCreatedWatcherTask<>));
        }

        private static void UseHangFireFileQueue(this IServiceCollection services)
        {
            services.AddSingleton<IFileQueue, HangFireFileQueue>();
        }

        public static void UseCustomFileQueue(this IServiceCollection services, IFileQueue fileQueue)
        {
            services.AddSingleton<IFileQueue>(fileQueue);
        }
    }
}