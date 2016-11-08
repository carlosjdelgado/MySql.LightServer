using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MySql.LightServer.Services
{
    internal class FileSystemService
    {
        public void CreateDirectories(List<string> directoryNames)
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

        public void RemoveDirectories(int retries, List<string> directoryNames)
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

        public void RemoveFiles(params string[] fileNames)
        {
            foreach(var fileName in fileNames)
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        public void CopyStreamToFile(Stream stream, string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }

        public string GetBaseDirectory()
        {
            return Path.Combine(Path.GetTempPath(), "MySqlLightServer");
        }        
    }
}
