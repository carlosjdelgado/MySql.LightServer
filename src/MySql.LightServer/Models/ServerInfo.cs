using System;

namespace MySql.LightServer.Models
{
    internal class ServerInfo
    {
        private const string ConnectionStringPattern = "server=127.0.0.1;uid=root;port={0};";

        public Guid ServerGuid { get; set; }
        public int Port { get; set; }
        public int? ProcessId { get; set; }
        public string ServerDirectory { get; set; }
        public string DataRootDirectory { get; set; }
        public string RunningInstancesFilePath { get; set; }
        public string DatabaseSelected { get; set; }
        public string ConnectionString => GetConnectionString();

        private string GetConnectionString()
        {
            return string.Format(ConnectionStringPattern, Port);
        }
    }
}
