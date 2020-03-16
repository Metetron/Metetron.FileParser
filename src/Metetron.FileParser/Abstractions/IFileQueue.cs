using System.Collections.Generic;
using System.IO.Abstractions;
using Metetron.FileParser.Abstractions;
using Metetron.FileParser.WatcherConfiguration;

namespace Metetron.FileParser.Abstractions
{
    public interface IFileQueue
    {
        void EnqueueNewFilesForProcessing<T>(WatcherOptions options, IList<IFileInfo> newFiles) where T : IParser, new();
    }
}