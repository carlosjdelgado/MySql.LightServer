using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MySql.LightServer.Models;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using MySql.Data.MySqlClient;
using MySql.LightServer.Services;
using MySql.LightServer.Mappers;

namespace MySql.LightServer.Server
{
    internal class WindowsServer : IServer
    {
        private readonly FileSystemService _fileSystemService;
        private Process _process;

        private const string RunningInstancesFile = "running_instances";
        private const string LightServerAssemblyName = "MySql.LightServer";
        private const string ServerFilesResourceName = "MySql.LightServer.ServerFiles.mysql-lightserver-win32.zip";

        public WindowsServer(ServerInfo serverInfo)
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
            }
        }

        public Process Start(ServerInfo serverInfo)
        {
            KillPreviousProcesses(serverInfo);
            var arguments = new List<string>()
            {
                $"--console",
                $"--standalone",
                $"--explicit_defaults_for_timestamp=1",
                $"--enable-named-pipe",
                $"--basedir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString())}\"",
                $"--lc-messages-dir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "share")}\"",
                $"--datadir=\"{Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "data")}\"",
                $"--skip-grant-tables",
                $"--port={serverInfo.Port}",
                $"--innodb_fast_shutdown=2",
                $"--innodb_doublewrite=OFF",
                $"--innodb_log_file_size=4M",
                $"--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };
            _process = StartProcess(Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString(), "bin", "mysqld.exe"), arguments);
            WaitForStartup(serverInfo);
            File.WriteAllText(Path.Combine(serverInfo.ServerDirectory, RunningInstancesFile), _process.Id.ToString());
            return _process;
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

        private bool ServerIsDeployed(string serverDirectory)
        {
            if (Directory.Exists(Path.Combine(serverDirectory, "bin")))
            {
                return (Directory.GetFiles(Path.Combine(serverDirectory, "bin"), "mysqld.exe").Length > 0);
            }

            return false;
        }

        public void ShutDown()
        {
            if (this.IsRunning())
            {
                _process.Kill();
                _process.WaitForExit();
                _process = null;
            }
        }

        public bool IsRunning()
        {
            return (_process != null && !_process.HasExited);
        }
    }
}
