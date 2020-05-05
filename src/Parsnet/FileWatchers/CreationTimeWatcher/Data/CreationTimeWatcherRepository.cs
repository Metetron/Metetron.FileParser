using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parsnet.Persistence;

namespace Parsnet.FileWatchers.CreationTimeWatcher.Data
{
    public class CreationTimeWatcherRepository : ICreationTimeWatcherRepository
    {
        private readonly WatcherContext _context;

        public CreationTimeWatcherRepository(WatcherContext context)
        {
            _context = context;
        }

        public async Task AddWatcherDataAsync(CreationTimeWatcherData watcherData)
        {
            _context.CreationTimeWatchers.Add(watcherData);
            await _context.SaveChangesAsync();
        }

        public Task<CreationTimeWatcherData> GetWatcherDataAsync(string parserName)
        {
            return _context.CreationTimeWatchers.SingleOrDefaultAsync(w => w.ParserName.Equals(parserName));
        }

        public async Task UpdateLastCreationTimeAsync(string parserName, long ticks)
        {
            var watcherData = await GetWatcherDataAsync(parserName);

            watcherData.LastCreationTimeUtc = ticks;
            await _context.SaveChangesAsync();
        }
    }
}