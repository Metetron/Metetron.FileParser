using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parsnet.Abstractions;
using Parsnet.FileWatchers.CreationTimeWatcher;
using Parsnet.FileWatchers.WriteTimeWatcher;
using Parsnet.Options;

namespace Parsnet.WorkerService
{
    public class ParserWorker
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ParserWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<Guid, IFileWatcher> _registrations;

        public bool IsWorkerRunning { get; private set; }

        public ParserWorker(ILogger<ParserWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration = null)
        {
            _configuration = configuration;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _registrations = new Dictionary<Guid, IFileWatcher>();
        }

        public Guid RegisterCreationTimeParser<T>(WatcherOptions watcherOptions) where T : IParser, new()
        {
            CheckOptions(watcherOptions);

            var guid = Guid.NewGuid();
            var watcherTask = _serviceProvider.GetRequiredService<CreationTimeWatcherTask<T>>();
            watcherTask.SetOptions(watcherOptions);

            _registrations.Add(guid, watcherTask);

            return guid;
        }

        public Guid RegisterWriteTimeParser<T>(WatcherOptions watcherOptions) where T : IParser, new()
        {
            CheckOptions(watcherOptions);

            var guid = Guid.NewGuid();
            var watcherTask = _serviceProvider.GetRequiredService<WriteTimeWatcherTask<T>>();
            watcherTask.SetOptions(watcherOptions);

            _registrations.Add(guid, watcherTask);

            return guid;
        }

        public void StartParser(Guid guid)
        {
            var isParserRegistered = _registrations.TryGetValue(guid, out var registration);

            if (!isParserRegistered)
                throw new ParserIsNotRegisteredException();

            registration.Start();
        }

        public void StartAllParsers()
        {
            foreach (var registration in _registrations.Values)
            {
                registration.Start();
            }
        }

        public void StopParser(Guid guid)
        {
            var isParserRegistered = _registrations.TryGetValue(guid, out var registration);

            if (!isParserRegistered)
                throw new ParserIsNotRegisteredException();

            registration.Stop();
        }

        public void StopAllParsers()
        {
            foreach (var registration in _registrations.Values)
            {
                registration.Stop();
            }
        }

        private void CheckOptions(WatcherOptions watcherOptions)
        {
            if (IsWorkerRunning)
                throw new WorkerAlreadyStartedException();

            if (watcherOptions == null)
                throw new ArgumentNullException(nameof(watcherOptions));

            if (!watcherOptions.AreOptionsValid())
                throw new ArgumentException(watcherOptions.ErrorMessages.FirstOrDefault());

            if (!watcherOptions.IsParserUnique(_registrations.Values.Select(v => v.Options)))
                throw new ParserNotUniqueException();
        }
    }
}
