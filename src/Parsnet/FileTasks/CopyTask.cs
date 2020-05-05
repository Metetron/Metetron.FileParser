using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Parsnet.FileTasks
{
    internal class CopyTask
    {
        /// <summary>
        /// Copy a file to a directory
        /// </summary>
        /// <param name="filePath">The path of the file to copy</param>
        /// <param name="directoryPath">The directory to copy the file to</param>
        public static async Task CopyFileToDirectoryAsync(string filePath, string directoryPath)
        {
            CreateDirectoryIfItDoesNotExist(directoryPath);

            var file = new FileInfo(filePath);

            if (!IsFileAccessible(file))
                await Task.Delay(1000);

            //Do not catch exceptions, so that the task will be retried by HangFire
            file.CopyTo($"{directoryPath}\\{file.Name}", true);
        }

        /// <summary>
        /// Check if the given directory exist, if not create it
        /// </summary>
        /// <param name="directoryPath">The path to the directory</param>
        private static void CreateDirectoryIfItDoesNotExist(string directoryPath)
        {
            var doesExist = Directory.Exists(directoryPath);

            if (!doesExist)
                Directory.CreateDirectory(directoryPath);
        }

        /// <summary>
        /// Check whether the file is accessible for copying ot not
        /// </summary>
        /// <param name="file">The file to check</param>
        /// <returns>True if the file can be copied, false if it is locked</returns>
        private static bool IsFileAccessible(FileInfo file)
        {
            try
            {
                using (file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
