namespace Parsnet.FileWatchers.WriteTimeWatcher.Data
{
    public class WriteTimeWatcherData
    {
        public int Id { get; set; }
        public string ParserName { get; set; }
        public long LastWriteTimeUtc { get; set; }
    }
}
