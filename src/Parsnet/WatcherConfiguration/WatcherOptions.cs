using System;
using System.Collections.Generic;
using System.Linq;

namespace Parsnet.WatcherConfiguration
{
    public class WatcherOptions
    {
        /// <value>The distinct name of the parser</value>
        public string ParserName { get; set; }
        /// <value>The path to the directory the parser should watch</value>
        public string DirectoryToWatch { get; set; }
        /// <value>The regex- pattern for subdirectories that should be watched by the parser</value>
        public string SubDirectorySearchPattern { get; set; }
        /// <value>The regex- pattern for files that the parser should watch</value>
        public string FileSearchPattern { get; set; }
        /// <value>The interval in which the parser checks the directory for new data</value>
        public int PollingInterval { get; set; }
        /// <value>The working directory of the parser. Files are copied there before they are parsed.</value>
        public string WorkingDirectoryPath { get; set; }
        /// <value>The backup directory of the parser. Files are copied there after they are parsed.</value>
        public string BackupDirectoryPath { get; set; }
        /// <value>Wether the parser should also watch the main directory, when a subdirectory pattern is defined.</value>
        public bool CheckMainDirectory { get; set; }
        /// <value>Whether the original file should be removed after parsing.</value>
        public bool DeleteSourceFileAfterParsing { get; set; }

        public IList<string> ErrorMessages { get; set; }

        internal bool AreOptionsValid()
        {
            var validator = new WatcherOptionsValidator();

            var validationResult = validator.Validate(this);

            ErrorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

            return validationResult.IsValid;
        }

        internal bool IsParserUnique(IEnumerable<WatcherOptions> registeredParsers)
        {
            return !registeredParsers.Any(rp =>
                rp.DirectoryToWatch.Equals(DirectoryToWatch, StringComparison.OrdinalIgnoreCase) &&
                rp.FileSearchPattern.Equals(FileSearchPattern) &&
                rp.SubDirectorySearchPattern.Equals(SubDirectorySearchPattern));
        }
    }
}