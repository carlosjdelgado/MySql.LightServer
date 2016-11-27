using System;
using System.Collections.Generic;
using System.Diagnostics;
using MySql.LightServer.Models;
using System.IO;

namespace MySql.LightServer.Server
{
    internal class LinuxServer : Server
    {
        protected override string ServerFilesResourceName => "mysql-lightserver-linux.zip";

        public LinuxServer(string rootPath, int port)
        {
            _properties = BuildProperties(rootPath, port);
        }

        public override void Start()
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
        }

        public override void ShutDown()
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

        public override bool IsRunning()
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
    }
}
