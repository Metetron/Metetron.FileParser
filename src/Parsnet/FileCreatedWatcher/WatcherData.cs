namespace Parsnet.FileCreatedWatcher
{
    public class WatcherData
    {
        public int Id { get; set; }
        public string ParserName { get; set; }
        public long LastFileCreationInTicks { get; set; }
    }
}