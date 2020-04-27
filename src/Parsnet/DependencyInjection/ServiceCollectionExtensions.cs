using System.IO.Abstractions;
using AutoMapper;
using Hangfire;
using Hangfire.LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Parsnet.Abstractions;
using Parsnet.FileCreatedWatcher;
using Parsnet.ParserWorker;
using Parsnet.FileTasks;

namespace Parsnet.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddParsnet(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();

            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new FileWatcherProfile(services.BuildServiceProvider().GetService<IFileSystem>()));
            }).CreateMapper());

            services.AddTransient(typeof(FileCreatedWatcherTask<>));
            services.AddTransient<IWatcherDataRepository, WatcherDataRepository>();
            services.AddTransient<IFileChecker, FileChecker>();
            services.AddTransient<IFileQueue, HangFireFileQueue>();
            services.AddTransient<ParserWorkerService>();

            GlobalConfiguration.Configuration.UseLiteDbStorage("Hangfire.db");

            new BackgroundJobServer(new BackgroundJobServerOptions { WorkerCount = 10 });
        }
    }
}
