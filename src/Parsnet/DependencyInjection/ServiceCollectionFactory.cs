using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parsnet.Persistence;

namespace Parsnet.DependencyInjection
{
    public class ServiceCollectionFactory
    {
        public static IServiceCollection CreateDefaultServiceCollection(ILoggerFactory loggerFactory)
        {
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            services.AddLogging();

            services.AddDbContext<WatcherContext>(options => options.UseSqlite("Filename=WatcherDatabase.db"));
            services.AddParsnet();

            return services;
        }
    }
}
