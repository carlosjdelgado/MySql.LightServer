using System;

namespace MySql.LightServer.Models
{
    internal class ServerProperties
    {
        private const string ConnectionStringPattern = "server=127.0.0.1;uid=root;port={0};";

        public Guid Guid { get; set; }
        public int Port { get; set; }
        public string RootPath { get; set; }
        public string InstancePath { get; set; }
        public string BinaryPath { get; set; }
        public string SharePath { get; set; }
        public string DataPath { get; set; }
        public string ExecutablePath { get; set; }
        public string RunningInstancesFilePath { get; set; }
        public string PidFilePath { get; set; }
        public string SocketFilePath { get; set; }
        public string ErrorLogFilePath { get; set; }
        public string ConnectionString => string.Format(ConnectionStringPattern, Port);
    }
}
