using System.IO;
using System.Threading;

namespace MySql.LightServer.Services
{
    public static class FileSystemService
    {
        public static void CreateDirectories(params string[] directoryNames)
        {
            foreach (string directoryName in directoryNames)
            {
                var directoryInfo = new DirectoryInfo(directoryName);
                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                }

                directoryInfo.Create();
            }
        }

        public static void RemoveDirectories(int retries, params string[] directoryNames)
        {
            foreach (string directoryName in directoryNames)
            {
                var directoryInfo = new DirectoryInfo(directoryName);
                if (directoryInfo.Exists)
                {
                    var retryCount = 0;
                    while (retryCount < retries)
                    {
                        try
                        {
                            directoryInfo.Delete(true);
                            return;
                        }
                        catch
                        {
                            retryCount++;
                            Thread.Sleep(50);
                        }
                    }
                }
            }
        }

        public static void RemoveFiles(params string[] fileNames)
        {
            foreach(var fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        public static void CopyStreamToFile(Stream stream, string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }

        public static string GetBaseDirectory()
        {
            return new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName.ToString();
        }        
    }
}
