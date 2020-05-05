using System.IO.Abstractions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Parsnet.Abstractions;
using Parsnet.FileTasks;
using Parsnet.FileWatchers.CreationTimeWatcher;
using Parsnet.FileWatchers.WriteTimeWatcher;
using Parsnet.Persistence;
using Hangfire.Storage.SQLite;
using Parsnet.WorkerService;

namespace Parsnet.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddParsnet(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IFileQueue, HangFireFileQueue>();
            services.AddTransient<ParserWorker>();

            services.AddWriteTimeWatchers();
            services.AddCreationTimeWatchers();
            services.EnsureDatabaseIsCreated();

            CreateHangfireServer();
        }

        private static void EnsureDatabaseIsCreated(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<WatcherContext>();
            context.Database.EnsureCreated();
        }

        private static void CreateHangfireServer()
        {
            GlobalConfiguration.Configuration.UseSQLiteStorage("Hangfire.db");
            new BackgroundJobServer(new BackgroundJobServerOptions { WorkerCount = 10 });
        }
    }
}
