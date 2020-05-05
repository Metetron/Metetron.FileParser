using Microsoft.Extensions.DependencyInjection;
using Parsnet.FileWatchers.CreationTimeWatcher.Data;

namespace Parsnet.FileWatchers.CreationTimeWatcher
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCreationTimeWatchers(this IServiceCollection services)
        {
            services.AddTransient<ICreationTimeWatcherRepository, CreationTimeWatcherRepository>();
            services.AddTransient(typeof(CreationTimeWatcherTask<>));

            return services;
        }
    }
}
