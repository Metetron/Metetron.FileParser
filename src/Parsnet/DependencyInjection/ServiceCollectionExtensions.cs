using System.IO.Abstractions;
using AutoMapper;
using Hangfire;
using Hangfire.LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Parsnet.Abstractions;
using Parsnet.ParserWorker;
using Parsnet.FileTasks;
using Parsnet.FileWatchers.CreationTimeWatcher;
using Parsnet.FileWatchers.WriteTimeWatcher;

namespace Parsnet.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddParsnet(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddTransient<IFileQueue, HangFireFileQueue>();
            services.AddTransient<ParserWorkerService>();

            services.AddWriteTimeWatchers();
            services.AddCreationTimeWatchers();

            new BackgroundJobServer(new BackgroundJobServerOptions { WorkerCount = 10 });
        }
    }
}
