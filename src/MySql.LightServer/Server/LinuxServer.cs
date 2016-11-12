using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MySql.LightServer.Models;
using System.Reflection;
using System.IO.Compression;
using MySql.LightServer.Services;
using System.IO;
using MySql.Data.MySqlClient;
using MySql.LightServer.Mappers;

namespace MySql.LightServer.Server
{
    internal class LinuxServer : IServer
    {
        private readonly FileSystemService _fileSystemService;
        private Process _process;
        private string _serverDirectory;

        private const string RunningInstancesFile = "running_instances";
        private const string MysqldPidFile = "mysql-light-server.pid";
        private const string LightServerAssemblyName = "MySql.LightServer";
        private const string ServerFilesResourceName = "MySql.LightServer.ServerFiles.mysql-lightserver-linux.zip";

        public LinuxServer()
        {
            _fileSystemService = new FileSystemService();
        }

        public void Extract(string serverDirectory)
        {
            if (!ServerIsDeployed(serverDirectory))
            {
                var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

                var serverFilesCompressed = new ZipArchive(assembly.GetManifestResourceStream(ServerFilesResourceName));
                serverFilesCompressed.ExtractToDirectory(serverDirectory);
                _serverDirectory = serverDirectory;
            }
        }

        private bool ServerIsDeployed(string serverDirectory)
        {
            if (Directory.Exists(Path.Combine(serverDirectory, "bin")))
            {
                return (Directory.GetFiles(Path.Combine(serverDirectory, "bin"), "mysqld_safe").Length > 0);
            }

            return false;
        }

        public Process Start(ServerInfo serverInfo)
        {
            KillPreviousProcesses(serverInfo);
            var arguments = new List<string>()
            {
                $"--port={serverInfo.Port}",
                $"--ledir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "bin")}\"",
                $"--lc-messages-dir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "share")}\"",
                $"--socket=\"{Path.Combine(serverInfo.ServerDirectory, "mysql-light-server.sock")}\"",
                $"--basedir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString())}\"",
                $"--datadir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "data")}\"",
                $"--pid-file=\"{Path.Combine(serverInfo.ServerDirectory, MysqldPidFile)}\"",
                $"--log-error=\"{Path.Combine(serverInfo.ServerDirectory, "error.log")}\""
            };
            _process = StartProcess(Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "bin", "mysqld_safe"), arguments);
            WaitForStartup(serverInfo);
            WriteRunningInstancesFile();
            return _process;
        }

        private void WriteRunningInstancesFile()
        {
            File.WriteAllText(Path.Combine(_serverDirectory, RunningInstancesFile), _process.Id.ToString());
            File.AppendAllText(Path.Combine(_serverDirectory, RunningInstancesFile), File.ReadAllText(Path.Combine(_serverDirectory, MysqldPidFile)));
        }

        private void KillPreviousProcesses(ServerInfo serverInfo)
        {
            if (!File.Exists(serverInfo.RunningInstancesFilePath))
                return;

            var runningInstancesIds = File.ReadAllLines(Path.Combine(serverInfo.ServerDirectory, RunningInstancesFile));
            foreach (var runningInstanceId in runningInstancesIds)
            {
                var process = Process.GetProcessById(int.Parse(runningInstanceId));
                process.Kill();
            }

            _fileSystemService.RemoveDirectories(ServerInfoMapper.ToDirectoryList(serverInfo), 10);
            _fileSystemService.RemoveFiles(serverInfo.RunningInstancesFilePath);
        }

        private void WaitForStartup(ServerInfo serverInfo)
        {
            var totalTimeToWait = TimeSpan.FromSeconds(10);
            var startup = DateTime.Now;

            var testConnection = new MySqlConnection(serverInfo.ConnectionString);
            while (totalTimeToWait > (DateTime.Now - startup))
            {
                try
                {
                    testConnection.Open();
                    Console.WriteLine("Database connection established after " + (DateTime.Now - startup));
                    testConnection.ClearAllPoolsAsync();
                    testConnection.Close();
                    testConnection.Dispose();
                    return;
                }
                catch { }
            }
            throw new Exception("Server could not be started.");
        }

        private Process StartProcess(string executablePath, List<string> arguments)
        {
            var process = new Process();
            process.StartInfo.FileName = Path.Combine(executablePath);
            process.StartInfo.Arguments = string.Join(" ", arguments);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            return process;
        }

        public void ShutDown()
        {
            if (this.IsRunning())
            {
                var runningInstancesIds = File.ReadAllLines(Path.Combine(_serverDirectory, RunningInstancesFile));
                foreach (var runningInstanceId in runningInstancesIds)
                {
                    var process = Process.GetProcessById(int.Parse(runningInstanceId));
                    if (process != null && !process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                        process = null;
                    }
                }
            }
        }

        public bool IsRunning()
        {
            var runningInstancesIds = File.ReadAllLines(Path.Combine(_serverDirectory, RunningInstancesFile));
            foreach (var runningInstanceId in runningInstancesIds)
            {
                var process = Process.GetProcessById(int.Parse(runningInstanceId));
                if (process == null || process.HasExited)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
