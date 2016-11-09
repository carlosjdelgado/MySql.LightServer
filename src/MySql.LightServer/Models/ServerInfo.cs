using System;

namespace MySql.LightServer.Models
{
    internal class ServerInfo
    {
        private const string ConnectionStringPattern = "Server=127.0.0.1;Port={0};Protocol=pipe;";

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
