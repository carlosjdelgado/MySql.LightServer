using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Server.Models
{
    internal class ServerInfo
    {

        private const string ConnectionStringPattern = "Server=127.0.0.1;Port={0};Protocol=pipe;";
        private const string ConnectionStringWithDatabasePattern = "Server=127.0.0.1;Port={0};Protocol=pipe;Database={1};";

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
            if (string.IsNullOrEmpty(DatabaseSelected))
            {
                return string.Format(ConnectionStringPattern, Port);
            }

            return string.Format(ConnectionStringWithDatabasePattern, Port, DatabaseSelected);
        }
    }
}
