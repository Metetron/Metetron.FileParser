using System;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Parsnet.FileCreatedWatcher
{
    public class WatcherDataRepository : IWatcherDataRepository
    {
        private readonly string _tableName = $"{AppContext.BaseDirectory}\\FileCreatedWatchers";

        public Task<WatcherData> GetWatcherDataAsync(string parserName)
        {
            return Task.Run(() =>
            {
                using (var repository = new LiteRepository(_tableName))
                {
                    return repository.FirstOrDefault<WatcherData>(d => d.ParserName.Equals(parserName, StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        public Task InsertWatcherDataAsync(WatcherData watcherData)
        {
            return Task.Run(() =>
            {
                using (var repository = new LiteRepository(_tableName))
                {
                    repository.Insert(watcherData);
                }
            });
        }

        public Task UpdateWatcherDataAsync(WatcherData watcherData)
        {
            return Task.Run(() =>
            {
                using (var repository = new LiteRepository(_tableName))
                {
                    repository.Update(watcherData);
                }
            });
        }
    }
}