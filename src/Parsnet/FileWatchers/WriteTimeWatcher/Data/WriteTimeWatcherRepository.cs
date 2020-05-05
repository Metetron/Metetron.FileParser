using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parsnet.Persistence;

namespace Parsnet.FileWatchers.WriteTimeWatcher.Data
{
    public class WriteTimeWatcherRepository : IWriteTimeWatcherRepository
    {
        private readonly WatcherContext _context;

        public WriteTimeWatcherRepository(WatcherContext context)
        {
            _context = context;
        }

        public async Task AddWatcherDataAsync(WriteTimeWatcherData watcherData)
        {
            _context.WriteTimeWatchers.Add(watcherData);
            await _context.SaveChangesAsync();
        }

        public Task<WriteTimeWatcherData> GetWatcherDataAsync(string parserName)
        {
            return _context.WriteTimeWatchers.SingleOrDefaultAsync(w => w.ParserName.Equals(parserName));
        }

        public async Task UpdateLastWriteTimeAsync(string parserName, long ticks)
        {
            var watcherData = await GetWatcherDataAsync(parserName);

            watcherData.LastWriteTimeUtc = ticks;
            await _context.SaveChangesAsync();
        }
    }
}
