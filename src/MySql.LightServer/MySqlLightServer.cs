using System.IO;

namespace MySql.LightServer
{
    /// <summary>
    /// A class controlling test database initializing and cleanup
    /// </summary>
    public class MySqlLightServer
    {
        private readonly Server.Server _server;

        private const int DefaultPort = 3306;

        /// <summary>
        /// Port of the server
        /// </summary>
        public int ServerPort => _server.GetPort();
        /// <summary>
        /// Connection String useful for connect to the server if is running
        /// </summary>
        public string ConnectionString => _server.GetConnectionString();
        /// <summary>
        /// You can check if server is running with this property
        /// </summary>
        public bool Running => _server.IsRunning();

        public MySqlLightServer(int port = DefaultPort, string rootPath = null)
        {
            _server = ServerFactory.GetServer(port, rootPath ?? GetDefaultRootPath());
        }

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

        private string GetDefaultRootPath()
        {
            return Path.Combine(Path.GetTempPath(), "MySqlLightServer");
        }

        ~MySqlLightServer()
        {
            ShutDown();
        }
    }
}
