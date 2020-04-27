using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Parsnet.DependencyInjection
{
    public class ServiceCollectionFactory
    {
        public static IServiceCollection CreateDefaultServiceCollection(ILoggerFactory loggerFactory)
        {
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            services.AddLogging();

            return services;
        }
    }
}
