using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Parsnet.Persistence
{
    public class WatcherContextDesignTimeFactory : IDesignTimeDbContextFactory<WatcherContext>
    {
        public WatcherContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WatcherContext>();
            optionsBuilder.UseSqlite("Filename=Design.db");

            return new WatcherContext(optionsBuilder.Options);
        }
    }
}
