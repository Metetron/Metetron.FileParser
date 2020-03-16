using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Parsnet.Options
{
    public class FileCheckOptions
    {
        public string ParserName { get; set; }
        public Regex FileSearchPattern { get; set; }
        public Regex SubDirectorySearchPattern { get; set; }
        public IDirectoryInfo MainDirectory { get; set; }
        public long LastCreationTimeInTicks { get; set; }
        public bool CheckMainDirectory { get; set; }
    }
}