using Parsnet.Abstractions;
using Parsnet.FileCreatedWatcher;
using Parsnet.FileTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Parsnet
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