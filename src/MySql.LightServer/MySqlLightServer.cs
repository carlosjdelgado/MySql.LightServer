using MySql.LightServer.Services;
using MySql.LightServer.Mappers;
using MySql.LightServer.Models;
using System;
using System.Diagnostics;
using System.IO;
using MySql.LightServer.Server;

namespace MySql.LightServer
{
    /// <summary>
    /// A singleton class controlling test database initializing and cleanup
    /// </summary>
    public class MySqlLightServer
    {
        private FileSystemService _fileSystemService;
        private IServer _server;

        private ServerInfo _serverInfo;
        private static MySqlLightServer _instance;

        private const int DefaultServerPort = 3306;
        private const string RunningInstancesFile = "running_instances";

        public int ServerPort => _serverInfo.Port;
        //public int? ProcessId => GetProcessId();
        public string ConnectionString => _serverInfo.ConnectionString;
        public static MySqlLightServer Instance => GetInstance();

        /// <summary>
        /// Starts the server and creates all files and folders necessary
        /// </summary>
        public void StartServer()
        {
            if (_server.IsRunning())
            {
                return;
            }

            _fileSystemService.CreateDirectories(ServerInfoMapper.ToDirectoryList(_serverInfo));
            _server.Extract(Path.Combine(_serverInfo.ServerDirectory, _serverInfo.ServerGuid.ToString()));
            _server.Start(_serverInfo);
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
        /// Shuts down the server and removes all files related to it
        /// </summary>
        public void ShutDown()
        {
            _server.ShutDown();
            _fileSystemService.RemoveDirectories(ServerInfoMapper.ToDirectoryList(_serverInfo), 10);
            _fileSystemService.RemoveFiles(_serverInfo.RunningInstancesFilePath);
        }

        private MySqlLightServer()
        {
            _fileSystemService = new FileSystemService();

            _serverInfo = new ServerInfo
            {
                ServerGuid = Guid.NewGuid(),
                Port = DefaultServerPort,
                ServerDirectory = Path.Combine(_fileSystemService.GetBaseDirectory()),
                //RunningInstancesFilePath = Path.Combine(_fileSystemService.GetBaseDirectory(), RunningInstancesFile),
            };

            _server = ServerFactory.GetServer(_serverInfo);
        }

        private static MySqlLightServer GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MySqlLightServer();
            }

            return _instance;
        }

        ~MySqlLightServer()
        {
            //if (_process != null)
            //{
            //    _process.Kill();
            //    _process.Dispose();
            //    _process = null;
            //}

            if (_instance != null)
            {
                _instance.ShutDown();
                _instance = null;
            }
        }
    }
}
