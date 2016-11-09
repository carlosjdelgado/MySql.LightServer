using MySql.LightServer.Services;
using MySql.LightServer.Mappers;
using MySql.LightServer.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace MySql.LightServer
{
    /// <summary>
    /// A singleton class controlling test database initializing and cleanup
    /// </summary>
    public class MySqlLightServer
    {
        private FileSystemService _fileSystemService;
        private ServerService _serverService;

        private ServerInfo _serverInfo;
        private Process _process;
        private static MySqlLightServer _instance;

        private const int DefaultServerPort = 3306;
        private const string TempServerFolder = "tempServer";
        private const string DataFolder = "data";
        private const string RunningInstancesFile = "running_instances";

        public int ServerPort => _serverInfo.Port;
        public int? ProcessId => GetProcessId();
        public string ConnectionString => _serverInfo.ConnectionString;
        public static MySqlLightServer Instance => GetInstance();

        /// <summary>
        /// Starts the server and creates all files and folders necessary
        /// </summary>
        public void StartServer()
        {
            if (_process != null && !_process.HasExited)
            {
                return;
            }

            _fileSystemService.CreateDirectories(ServerInfoMapper.ToDirectoryList(_serverInfo));
            _serverService.Extract(_serverInfo.ServerDirectory);
            _process = _serverService.Start(_serverInfo);
            _serverInfo.ProcessId = _process.Id;

            File.WriteAllText(_serverInfo.RunningInstancesFilePath, _serverInfo.ProcessId.ToString());
        }

        /// <summary>
        /// Start the server on a specified port number
        /// </summary>
        /// <param name="serverPort">The port on which the server should listen</param>
        public void StartServer(int serverPort)
        {
            _serverInfo.Port = serverPort;
            StartServer();
        }

        /// <summary>
        /// Kill previous instances of MySqlServer
        /// </summary>
        public void KillPreviousProcesses()
        {
            _serverService.KillPreviousProcesses(_serverInfo);
        }

        /// <summary>
        /// Shuts down the server and removes all files related to it
        /// </summary>
        public void ShutDown()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
                _process = null;
            }

            _fileSystemService.RemoveDirectories(ServerInfoMapper.ToDirectoryList(_serverInfo), 10);
            _fileSystemService.RemoveFiles(_serverInfo.RunningInstancesFilePath);
        }

        private MySqlLightServer()
        {
            _fileSystemService = new FileSystemService();
            _serverService = new ServerService();

            _serverInfo = new ServerInfo
            {
                ServerGuid = Guid.NewGuid(),
                Port = DefaultServerPort,
                ServerDirectory = Path.Combine(_fileSystemService.GetBaseDirectory(), TempServerFolder),
                DataRootDirectory = Path.Combine(_fileSystemService.GetBaseDirectory(), DataFolder),
                RunningInstancesFilePath = Path.Combine(_fileSystemService.GetBaseDirectory(), RunningInstancesFile),
            };
        }

        private static MySqlLightServer GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MySqlLightServer();
            }

            return _instance;
        }

        private int? GetProcessId()
        {
            if (_process.HasExited)
            {
                _serverInfo.ProcessId = null;
            }

            return _serverInfo.ProcessId;
        }

        ~MySqlLightServer()
        {
            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }

            if (_instance != null)
            {
                _instance.ShutDown();
                _instance = null;
            }
        }
    }
}
