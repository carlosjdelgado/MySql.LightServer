using MySql.Data.MySqlClient;
using MySql.LightServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace MySql.LightServer.Server
{
    internal class WindowsServer : IServer
    {
        private Process _process;
        private ServerProperties _properties;

        private const string RunningInstancesFile = "running_instances";
        private const string LightServerAssemblyName = "MySql.LightServer";
        private const string ServerFilesResourceName = "MySql.LightServer.ServerFiles.mysql-lightserver-win32.zip";

        public WindowsServer(string rootPath, int port)
        {
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
            }
        }

        public void Start()
        {
            KillPreviousProcesses();
            var arguments = new List<string>()
            {
                $"--console",
                $"--standalone",
                $"--explicit_defaults_for_timestamp=1",
                $"--enable-named-pipe",
                $"--basedir=\"{_properties.InstancePath}\"",
                $"--lc-messages-dir=\"{_properties.SharePath}\"",
                $"--datadir=\"{_properties.DataPath}\"",
                $"--skip-grant-tables",
                $"--port={_properties.Port}",
                $"--innodb_fast_shutdown=2",
                $"--innodb_doublewrite=OFF",
                $"--innodb_log_file_size=4M",
                $"--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };
            _process = StartProcess(_properties.ExecutablePath, arguments);
            WaitForStartup();
            File.WriteAllText(_properties.RunningInstancesFilePath, _process.Id.ToString());
        }

        public void ShutDown()
        {
            if (this.IsRunning())
            {
                _process.Kill();
                _process.WaitForExit();
                _process = null;
                File.Delete(_properties.RunningInstancesFilePath);
            }
        }

        public bool IsRunning()
        {
            return (_process != null);
        }

        public string GetConnectionString()
        {
            return _properties.ConnectionString;
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
                ExecutablePath = Path.Combine(rootPath, serverGuid.ToString(), "bin", "mysqld.exe"),
                RunningInstancesFilePath = Path.Combine(rootPath, "running_instances")
            };
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

            File.Delete(_properties.RunningInstancesFilePath);
        }

        private void WaitForStartup()
        {
            var totalTimeToWait = TimeSpan.FromSeconds(10);
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
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = string.Join(" ", arguments);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            return process;
        }

        private bool ServerIsDeployed()
        {
            if (Directory.Exists(_properties.BinaryPath))
            {
                return (Directory.GetFiles(_properties.ExecutablePath).Length > 0);
            }

            return false;
        }

        public void Clear()
        {
            if(!this.IsRunning())
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

        public int GetPort()
        {
            return _properties.Port;
        }
    }
}
