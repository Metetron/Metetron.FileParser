using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Parsnet.Options;
using Parsnet.WatcherConfiguration;

namespace Parsnet.Abstractions
{
    public interface IFileChecker
    {
        /// <summary>
        /// Search for newly created files for a given parser
        /// </summary>
        /// <param name="options">Options object that contains the necessary information to find new files</param>
        /// <returns>A list of new files</returns>
        IEnumerable<IFileInfo> GetNewlyCreatedFiles(FileCheckOptions options);
    }
}