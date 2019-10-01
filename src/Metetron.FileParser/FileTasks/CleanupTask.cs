using System.IO;

namespace Metetron.FileParser.FileTasks
{
    public class CleanupTask
    {
        /// <summary>
        /// Delete a file
        /// </summary>
        /// <param name="filePath">The file to delete</param>
        public static void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }
    }
}