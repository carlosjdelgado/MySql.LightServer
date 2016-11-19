using MySql.LightServer.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MySql.LightServer.Server
{
    internal class WindowsServer : Server
    {
        protected override string ServerFilesResourceName => "MySql.LightServer.ServerFiles.mysql-lightserver-win32.zip";

        public WindowsServer(string rootPath, int port)
        {
            _properties = BuildProperties(rootPath, port);
        }

        public override void Start()
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

        public override void ShutDown()
        {
            if (this.IsRunning())
            {
                _process.Kill();
                _process.WaitForExit();
                _process = null;
                File.Delete(_properties.RunningInstancesFilePath);
            }
        }

        public override bool IsRunning()
        {
            return (_process != null);
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
                RunningInstancesFilePath = Path.Combine(rootPath, RunningInstancesFile)
            };
        }
    }
}
