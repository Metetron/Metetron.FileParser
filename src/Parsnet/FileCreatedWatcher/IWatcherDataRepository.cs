using System.Threading.Tasks;

namespace Parsnet.FileCreatedWatcher
{
    public interface IWatcherDataRepository
    {
        /// <summary>
        /// Gets the stored data for a specific parser
        /// </summary>
        /// <param name="parserName">The name of the parser to look for</param>
        /// <returns>The watcher data object or null if the parser does not exist</returns>
        Task<WatcherData> GetWatcherDataAsync(string parserName);

        /// <summary>
        /// Stores a watcher data object
        /// </summary>
        /// <param name="watcherData">The object to store</param>
        /// <returns>Task to await</returns>
        Task InsertWatcherDataAsync(WatcherData watcherData);

        /// <summary>
        /// Updates the stored data of a parser
        /// </summary>
        /// <param name="watcherData">The watcher data with the updated values</param>
        /// <returns>Task to await</returns>
        Task UpdateWatcherDataAsync(WatcherData watcherData);
    }
}