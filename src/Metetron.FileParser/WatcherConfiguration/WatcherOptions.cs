using System;
using System.Collections.Generic;
using System.Linq;

namespace Metetron.FileParser.WatcherConfiguration
{
    public class WatcherOptions
    {
        public string ParserName { get; set; }
        public string DirectoryToWatch { get; set; }
        public string SubDirectorySearchPattern { get; set; }
        public string FileSearchPattern { get; set; }
        public int PollingInterval { get; set; }
        public string WorkingDirectoryPath { get; set; }
        public string BackupDirectoryPath { get; set; }
        public bool CheckMainDirectory { get; set; }
        public bool DeletesourceFileAfterParsing { get; set; }

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