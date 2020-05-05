using Microsoft.Extensions.DependencyInjection;
using Parsnet.FileWatchers.WriteTimeWatcher.Data;

namespace Parsnet.FileWatchers.WriteTimeWatcher
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWriteTimeWatchers(this IServiceCollection services)
        {
            services.AddTransient<IWriteTimeWatcherRepository, WriteTimeWatcherRepository>();
            services.AddTransient(typeof(WriteTimeWatcherTask<>));

            return services;
        }
    }
}
