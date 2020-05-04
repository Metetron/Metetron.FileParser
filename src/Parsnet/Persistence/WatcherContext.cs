using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Parsnet.FileWatchers.CreationTimeWatcher.Data;
using Parsnet.FileWatchers.WriteTimeWatcher.Data;

namespace Parsnet.Persistence
{
    public class WatcherContext : DbContext
    {
        public DbSet<CreationTimeWatcherData> CreationTimeWatchers { get; set; }
        public DbSet<WriteTimeWatcherData> WriteTimeWatchers { get; set; }

        public WatcherContext(DbContextOptions<WatcherContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
