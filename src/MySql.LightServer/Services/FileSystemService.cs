using System;
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

        public void RemoveDirectories(List<string> directoryNames, int retries)
        {
            foreach (string directoryName in directoryNames)
            {
                var directoryInfo = new DirectoryInfo(directoryName);
                if (directoryInfo.Exists)
                {
                    RemoveDirectory(directoryName, retries);
                }
            }
        }

        private void RemoveDirectory(string directoryName, int retries)
        {
            var directoryInfo = new DirectoryInfo(directoryName);
            for (var retryCount = 1; retryCount <= retries; retryCount++)
            {
                try
                {
                    directoryInfo.Delete(true);
                    return;
                }
                catch { }
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
