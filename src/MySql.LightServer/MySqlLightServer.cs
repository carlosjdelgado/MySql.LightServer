using MySql.Data.MySqlClient;
using MySql.LightServer.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MySql.LightServer
{
    /// <summary>
    /// A singleton class controlling test database initializing and cleanup
    /// </summary>
    public class MySqlLightServer
    {
        private string _mysqlDirectory;
        private string _dataDirectory;
        private string _dataRootDirectory;
        private string _runningInstancesFile;
        private int _serverPort = 3306;

        private Process _process;
        private static MySqlLightServer _instance;

        private const string ConnectionStringPattern = "Server=127.0.0.1;Port={0};Protocol=pipe;";
        private const string ConnectionStringWithDatabasePattern = "Server=127.0.0.1;Port={0};Protocol=pipe;Database={1};";
        private const string TempServerFolder = "tempServer";
        private const string DataFolder = "data";
        private const string RunningInstancesFile = "running_instances";

        public int ServerPort => _serverPort;
        public int? ProcessId => GetProcessId();
        public static MySqlLightServer Instance => GetInstance();

        /// <summary>
        /// Get a connection string for the server (no database selected)
        /// </summary>
        /// <returns>A connection string for the server</returns>
        public string GetConnectionString()
        {
            return string.Format(ConnectionStringPattern, _serverPort.ToString());
        }

        /// <summary>
        /// Get a connection string for the server and a specified database
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <returns>A connection string for the server and database</returns>
        public string GetConnectionString(string databaseName)
        {
            return string.Format(ConnectionStringWithDatabasePattern, _serverPort.ToString(), databaseName);
        }

        /// <summary>
        /// Starts the server and creates all files and folders necessary
        /// </summary>
        public void StartServer()
        {
            if (_process != null && !_process.HasExited)
            {
                return;
            }

            KillPreviousProcesses();
            FileSystemService.CreateDirectories(_mysqlDirectory, _dataRootDirectory, _dataDirectory);
            ServerFilesService.Extract(_mysqlDirectory);

            var arguments = new[]
            {
                $"--standalone",
                $"--console",
                $"--basedir=\"{_mysqlDirectory}\"",
                $"--lc-messages-dir=\"{_mysqlDirectory}\"",
                $"--datadir=\"{_dataDirectory}\"",
                $"--skip-grant-tables",
                $"--enable-named-pipe",
                $"--port={_serverPort.ToString()}",
                $"--innodb_fast_shutdown=2",
                $"--innodb_doublewrite=OFF",
                $"--innodb_log_file_size=1048576",
                $"--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };

            _process = new Process();
            _process.StartInfo.FileName = Path.Combine(_mysqlDirectory, "mysqld");
            _process.StartInfo.Arguments = string.Join(" ", arguments);
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = false;

            _process.Start();
            File.WriteAllText(_runningInstancesFile, _process.Id.ToString());

            WaitForStartup();
        }

        /// <summary>
        /// Start the server on a specified port number
        /// </summary>
        /// <param name="serverPort">The port on which the server should listen</param>
        public void StartServer(int serverPort)
        {
            _serverPort = serverPort;
            StartServer();
        }

        /// <summary>
        /// Kill previous instances of MySqlServer
        /// </summary>
        public void KillPreviousProcesses()
        {
            if (!File.Exists(_runningInstancesFile))
                return;

            var runningInstancesIds = File.ReadAllLines(_runningInstancesFile);
            foreach(var runningInstanceId in runningInstancesIds)
            {
                var process = Process.GetProcessById(int.Parse(runningInstanceId));
                process.Kill();
            }

            FileSystemService.RemoveDirectories(10, _mysqlDirectory, _dataRootDirectory, _dataDirectory);
            FileSystemService.RemoveFiles(_runningInstancesFile);
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

            FileSystemService.RemoveDirectories(10, _mysqlDirectory, _dataRootDirectory, _dataDirectory);
            FileSystemService.RemoveFiles(_runningInstancesFile);
        }

        private MySqlLightServer()
        {
            _mysqlDirectory = Path.Combine(FileSystemService.GetBaseDirectory(), TempServerFolder);
            _dataRootDirectory = Path.Combine(FileSystemService.GetBaseDirectory(), DataFolder);
            _dataDirectory = Path.Combine(_dataRootDirectory, Guid.NewGuid().ToString());
            _runningInstancesFile = Path.Combine(FileSystemService.GetBaseDirectory(), RunningInstancesFile);
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
                return null;
            }
            return _process.Id;
        }

        private void WaitForStartup()
        {
            var totalWaitTime = TimeSpan.Zero;
            var sleepTime = TimeSpan.FromMilliseconds(100);

            var testConnection = new MySqlConnection(GetConnectionString());
            while (!testConnection.State.Equals(System.Data.ConnectionState.Open))
            {
                try
                {
                    testConnection.Open();
                }
                catch
                {
                    testConnection.Close();
                    Thread.Sleep(sleepTime);
                    totalWaitTime += sleepTime;
                }

                if (totalWaitTime > TimeSpan.FromSeconds(10))
                {
                    throw new Exception("Server could not be started.");
                }
            }

            Console.WriteLine("Database connection established after " + totalWaitTime.ToString());
            testConnection.ClearAllPoolsAsync();
            testConnection.Close();
            testConnection.Dispose();
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
