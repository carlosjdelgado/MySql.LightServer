using MySql.Data.MySqlClient;
using MySql.LightServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MySql.LightServer.Server
{
    internal abstract class Server
    {
        protected Process _process;
        protected ServerProperties _properties;

        protected const string RunningInstancesFile = "running_instances";
        protected const string MysqldPidFile = "mysql-light-server.pid";
        protected const string MysqldSocketFile = "mysql-light-server.sock";
        protected const string ErrorLogFile = "error.log";

        protected abstract string ServerFilesResourceName { get; }

        private const string LightServerAssemblyName = "MySql.LightServer";
        
        public abstract void Start();
        public abstract void ShutDown();
        public abstract bool IsRunning();

        public virtual void Extract()
        {
            using (var fileStream = new FileStream(GetResourcePath(ServerFilesResourceName), FileMode.Open))
            {
                var serverFilesCompressed = new ZipArchive(fileStream);
                Directory.CreateDirectory(_properties.InstancePath);
                serverFilesCompressed.ExtractToDirectory(_properties.InstancePath);
            }
        }

        private string GetResourcePath(string serverFilesResourceName)
        {
            var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));
            string assemblyPath = Path.GetDirectoryName(assembly.Location);
            
            if(File.Exists(Path.Combine(assemblyPath, serverFilesResourceName)))
            {
                return Path.Combine(assemblyPath, serverFilesResourceName);
            }

            if (File.Exists(Path.Combine(assemblyPath, "contentReferences.json")))
            {
                var contentReferencesFile = JObject.Parse(File.ReadAllText(Path.Combine(assemblyPath, "contentReferences.json")));
                var relativePath = contentReferencesFile[serverFilesResourceName].ToString();
                return Path.Combine(assemblyPath, relativePath);
            }

            return null;
        }

        public virtual void Clear()
        {
            if (!this.IsRunning())
            {
                DeleteDirectoryAndFiles(_properties.InstancePath);
            }
        }

        public virtual string GetConnectionString()
        {
            return _properties.ConnectionString;
        }

        public virtual int GetPort()
        {
            return _properties.Port;
        }

        protected Process StartProcess(string executablePath, List<string> arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = string.Join(" ", arguments);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            return process;
        }

        protected void WaitForStartup()
        {
            var totalTimeToWait = TimeSpan.FromSeconds(30);
            var startup = DateTime.Now;

            var testConnection = new MySqlConnection(_properties.ConnectionString);
            while (totalTimeToWait > (DateTime.Now - startup))
            {
                try
                {
                    testConnection.Open();
                    Console.WriteLine("Database connection established after " + (DateTime.Now - startup));
#if NETSTANDARD1_6
                    testConnection.ClearAllPoolsAsync();
#endif
                    testConnection.Close();
                    testConnection.Dispose();
                    return;
                }
                catch { }
            }
            throw new Exception("Server could not be started.");
        }

        protected void KillPreviousProcesses()
        {
            if (!File.Exists(_properties.RunningInstancesFilePath))
                return;

            var runningInstancesIds = File.ReadAllLines(_properties.RunningInstancesFilePath);
            foreach (var runningInstanceId in runningInstancesIds)
            {
                var process = Process.GetProcessById(int.Parse(runningInstanceId));
                process.Kill();
                process.WaitForExit();
            }

            File.Delete(_properties.RunningInstancesFilePath);
        }

        private void DeleteDirectoryAndFiles(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                foreach (string directory in Directory.GetDirectories(path))
                {
                    DeleteDirectoryAndFiles(directory);
                }
                Directory.Delete(path, true);
            }
        }

        private bool ServerIsDeployed()
        {
            if (Directory.Exists(_properties.BinaryPath))
            {
                return (Directory.GetFiles(_properties.ExecutablePath).Length > 0);
            }

            return false;
        }
    }
}
