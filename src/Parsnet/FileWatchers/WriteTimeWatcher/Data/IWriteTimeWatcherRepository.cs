using System.Threading.Tasks;

namespace Parsnet.FileWatchers.WriteTimeWatcher.Data
{
    public interface IWriteTimeWatcherRepository
    {
        Task<WriteTimeWatcherData> GetWatcherDataAsync(string parserName);

        Task AddWatcherDataAsync(WriteTimeWatcherData watcherData);

        Task UpdateLastWriteTimeAsync(string parserName, long ticks);
    }
}
