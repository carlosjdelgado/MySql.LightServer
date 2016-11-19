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
        private ServerProperties _properties;

        private const string RunningInstancesFile = "running_instances";
        private const string MysqldPidFile = "mysql-light-server.pid";
        private const string MysqldSocketFile = "mysql-light-server.sock";
        private const string ErrorLogFile = "error.log";

        private const string LightServerAssemblyName = "MySql.LightServer";
        private const string ServerFilesResourceName = "MySql.LightServer.ServerFiles.mysql-lightserver-linux.zip";

        public LinuxServer(string rootPath, int port)
        {
            _fileSystemService = new FileSystemService();
            _properties = BuildProperties(rootPath, port);
        }

        public void Extract()
        {
            if (!ServerIsDeployed())
            {
                var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

                var serverFilesCompressed = new ZipArchive(assembly.GetManifestResourceStream(ServerFilesResourceName));
                Directory.CreateDirectory(_properties.InstancePath);
                serverFilesCompressed.ExtractToDirectory(_properties.InstancePath);
                Console.WriteLine($"Instance {_properties.Guid} created");
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

        public Process Start()
        {
            KillPreviousProcesses();
            var arguments = new List<string>()
            {
                $"--port={_properties.Port}",
                $"--ledir=\"{_properties.BinaryPath}\"",
                $"--lc-messages-dir=\"{_properties.SharePath}\"",
                $"--socket=\"{_properties.SocketFilePath}\"",
                $"--basedir=\"{_properties.InstancePath}\"",
                $"--datadir=\"{_properties.DataPath}\"",
                $"--pid-file=\"{_properties.PidFilePath}\"",
                $"--log-error=\"{_properties.ErrorLogFilePath}\""
            };
            _process = StartProcess(_properties.ExecutablePath, arguments);
            WaitForStartup();
            WriteRunningInstancesFile();
            return _process;
        }

        private ServerProperties BuildProperties(string rootPath, int port)
        {
            var serverGuid = Guid.NewGuid();
            return new ServerProperties
            {
                Port = port,
                RootPath = rootPath,
                Guid = serverGuid,
                InstancePath = Path.Combine(rootPath, serverGuid.ToString()),
                BinaryPath = Path.Combine(rootPath, serverGuid.ToString(), "bin"),
                DataPath = Path.Combine(rootPath, serverGuid.ToString(), "data"),
                SharePath = Path.Combine(rootPath, serverGuid.ToString(), "share"),
                ExecutablePath = Path.Combine(rootPath, serverGuid.ToString(), "bin", "mysqld_safe"),
                RunningInstancesFilePath = Path.Combine(rootPath, RunningInstancesFile),
                PidFilePath = Path.Combine(rootPath, MysqldPidFile),
                SocketFilePath = Path.Combine(rootPath, MysqldSocketFile),
                ErrorLogFilePath = Path.Combine(rootPath, ErrorLogFile)
            };
        }

        private void WriteRunningInstancesFile()
        {
            var processIds = new List<string>
            {
                _process.Id.ToString(),
            };

            using (var fs = File.OpenText(_properties.PidFilePath))
            {
                processIds.Add(fs.ReadLine());
            }

            File.WriteAllLines(_properties.RunningInstancesFilePath, processIds);
        }

        private void KillPreviousProcesses()
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

            _fileSystemService.RemoveFiles(_properties.RunningInstancesFilePath);
        }

        private void WaitForStartup()
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
                if (!File.Exists(_properties.RunningInstancesFilePath))
                {
                    return;
                }

                var runningInstancesIds = File.ReadAllLines(_properties.RunningInstancesFilePath);
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
                File.Delete(_properties.RunningInstancesFilePath);
            }
        }

        public bool IsRunning()
        {
            if (!File.Exists(_properties.RunningInstancesFilePath))
            {
                return false;
            }

            var runningInstancesIds = File.ReadAllLines(_properties.RunningInstancesFilePath);
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

        public string GetConnectionString()
        {
            return _properties.ConnectionString;
        }

        public void Clear()
        {
            if (!this.IsRunning())
            {
                DeleteDirectoryAndFiles(_properties.InstancePath);
            }
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
    }
}
