using System.Threading.Tasks;

namespace Parsnet.FileWatchers.CreationTimeWatcher.Data
{
    public interface ICreationTimeWatcherRepository
    {
        Task<CreationTimeWatcherData> GetWatcherDataAsync(string parserName);

        Task AddWatcherDataAsync(CreationTimeWatcherData watcherData);

        Task UpdateLastCreationTimeAsync(string parserName, long ticks);
    }
}