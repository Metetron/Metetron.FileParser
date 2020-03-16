using System.IO;

namespace Parsnet.FileTasks
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

        /// <summary>
        /// Delete a directory and all containing files and subdirectories
        /// </summary>
        /// <param name="folderPath">The path to the folder</param>
        public static void DeleteFolder(string folderPath)
        {
            Directory.Delete(folderPath, true);
        }
    }
}