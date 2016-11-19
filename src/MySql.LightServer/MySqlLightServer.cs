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
        private readonly IServer _server;
        private readonly FileSystemService _fileSystemService;
        private const int DefaultPort = 3306;

        //public int ServerPort => _serverInfo.Port;
        //public int? ProcessId => GetProcessId();
        public string ConnectionString => _server.GetConnectionString();
        //public static MySqlLightServer Instance => GetInstance();

        /// <summary>
        /// Starts the server and creates all files and folders necessary
        /// </summary>
        public void StartServer()
        {
            if (_server.IsRunning())
            {
                return;
            }
            _server.Extract();
            _server.Start();
        }

        /// <summary>
        /// Shuts down the server and removes all files related to it
        /// </summary>
        public void ShutDown()
        {
            _server.ShutDown();
            _server.Clear();
        }

        public MySqlLightServer(int port = DefaultPort, string rootPath = null)
        {
            _fileSystemService = new FileSystemService();
            _server = ServerFactory.GetServer(port, rootPath ?? GetDefaultRootPath());
        }

        private string GetDefaultRootPath()
        {
            return _fileSystemService.GetBaseDirectory();
        }

        ~MySqlLightServer()
        {
            ShutDown();
        }
    }
}
