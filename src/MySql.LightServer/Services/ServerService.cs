using MySql.LightServer.Enums;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System;
using MySql.LightServer.Models;
using System.Collections.Generic;
using MySql.LightServer.Mappers;
using MySql.Data.MySqlClient;

namespace MySql.LightServer.Services
{
    internal class ServerService
    {
        private FileSystemService _fileSystemService;
        private OperatingSystem _platform;

        private const string LightServerAssemblyName = "MySql.LightServer";

        private const string Win32MySqlFileName = "mysqld.exe";
        private const string LinuxMySqlFileName = "mysqld";
        private const string ErrmsgFileName = "errmsg.sys";

        private const string ErrmsgResourceName = "MySql.LightServer.ServerFiles.errmsg.sys";
        private const string Win32MySqlResourceName = "MySql.LightServer.ServerFiles.Win32.mysqld.exe";
        private const string LinuxMySqlResourceName = "MySql.LightServer.ServerFiles.Linux.mysqld";

        public ServerService()
        {
            _platform = GetOsPlatform();
            _fileSystemService = new FileSystemService();

        }

        public void Extract(string serverDirectory)
        {
            if (!ServerIsDeployed(serverDirectory))
            {
                switch (_platform)
                {
                    case OperatingSystem.Linux:
                        ExtractLinuxServer(serverDirectory);
                        break;
                    case OperatingSystem.Windows:
                        ExtractWindowsServer(serverDirectory);
                        break;
                }
            }
        }

        public Process Start(ServerInfo serverInfo)
        {
            KillPreviousProcesses(serverInfo);
            switch(_platform)
            {
                case OperatingSystem.Windows:
                    return StartWindowsServer(serverInfo);
                case OperatingSystem.Linux:
                    return StartLinuxServer(serverInfo);
            }

            return null;
        }

        public void KillPreviousProcesses(ServerInfo serverInfo)
        {
            if (!File.Exists(serverInfo.RunningInstancesFilePath))
                return;

            var runningInstancesIds = File.ReadAllLines(serverInfo.RunningInstancesFilePath);
            foreach (var runningInstanceId in runningInstancesIds)
            {
                var process = Process.GetProcessById(int.Parse(runningInstanceId));
                process.Kill();
            }

            _fileSystemService.RemoveDirectories(10, ServerInfoMapper.ToDirectoryList(serverInfo));
            _fileSystemService.RemoveFiles(serverInfo.RunningInstancesFilePath);
        }

        private Process StartLinuxServer(ServerInfo serverInfo)
        {
            var arguments = new List<string>()
            {
                $"--console",
                $"--basedir=\"{serverInfo.ServerDirectory}\"",
                $"--lc-messages-dir=\"{serverInfo.ServerDirectory}\"",
                $"--datadir=\"{Path.Combine(serverInfo.DataRootDirectory, serverInfo.ServerGuid.ToString())}\"",
                $"--skip-grant-tables",
                $"--port={serverInfo.Port}",
                $"--innodb_fast_shutdown=2",
                $"--innodb_doublewrite=OFF",
                $"--innodb_log_file_size=1048576",
                $"--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };
            var process = StartProcess(Path.Combine(serverInfo.ServerDirectory, "mysqld"), arguments);
            WaitForStartup(serverInfo);
            return process;
        }


        private Process StartWindowsServer(ServerInfo serverInfo)
        {
            var arguments = new List<string>()
            {
                $"--console",
                $"--standalone",
                $"--enable-named-pipe",               
                $"--basedir=\"{serverInfo.ServerDirectory}\"",
                $"--lc-messages-dir=\"{serverInfo.ServerDirectory}\"",
                $"--datadir=\"{Path.Combine(serverInfo.DataRootDirectory, serverInfo.ServerGuid.ToString())}\"",
                $"--skip-grant-tables",
                $"--port={serverInfo.Port}",
                $"--innodb_fast_shutdown=2",
                $"--innodb_doublewrite=OFF",
                $"--innodb_log_file_size=1048576",
                $"--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };
            var process = StartProcess(Path.Combine(serverInfo.ServerDirectory, "mysqld.exe"), arguments);
            WaitForStartup(serverInfo);
            return process;
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
            switch (_platform)
            {
                case OperatingSystem.Windows:
                    return new FileInfo(Path.Combine(serverDirectory, Win32MySqlFileName)).Exists;
                case OperatingSystem.Linux:
                    return new FileInfo(Path.Combine(serverDirectory, LinuxMySqlFileName)).Exists;
            }

            return false;
        }

        private void ExtractWindowsServer(string serverDirectory)
        {
            var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

            var errmsg = assembly.GetManifestResourceStream(ErrmsgResourceName);
            var mysqld = assembly.GetManifestResourceStream(Win32MySqlResourceName);

            _fileSystemService.CopyStreamToFile(errmsg, Path.Combine(serverDirectory, ErrmsgFileName));
            _fileSystemService.CopyStreamToFile(mysqld, Path.Combine(serverDirectory, Win32MySqlFileName));
        }

        private void ExtractLinuxServer(string serverDirectory)
        {
            var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

            var errmsg = assembly.GetManifestResourceStream(ErrmsgResourceName);
            var mysqld = assembly.GetManifestResourceStream(LinuxMySqlResourceName);

            _fileSystemService.CopyStreamToFile(errmsg, Path.Combine(serverDirectory, ErrmsgFileName));
            _fileSystemService.CopyStreamToFile(mysqld, Path.Combine(serverDirectory, LinuxMySqlFileName));
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

        private OperatingSystem GetOsPlatform()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystem.Windows;
            }

            return OperatingSystem.Linux;
        }
    }
}
