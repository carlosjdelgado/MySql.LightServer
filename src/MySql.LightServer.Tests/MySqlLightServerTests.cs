using MySql.Data.MySqlClient;
using System.Diagnostics;

using Xunit;

namespace MySql.LightServer.Tests
{

    public class MySqlLightServerTests
    {
        [Fact]
        public void KillProcess()
        {
            var previousProcessCount = Process.GetProcessesByName("mysqld").Length;
            
            var database = MySqlLightServer.Instance;
            database.StartServer();
            database.ShutDown();
            
            Assert.Equal(previousProcessCount, Process.GetProcessesByName("mysqld").Length);
        }

        [Fact]
        public void StartServerOnSpecifiedPort()
        {
            var server = MySqlLightServer.Instance;
            server.StartServer(3366);
            MySqlHelper.ExecuteNonQuery(server.ConnectionString, "CREATE DATABASE testserver; USE testserver;");
            server.ShutDown();
        }

        [Fact]
        public void MultipleProcessesInARow()
        {
            var dbServer = MySqlLightServer.Instance;
            dbServer.StartServer();
            dbServer.ShutDown();
            dbServer.StartServer();
            dbServer.ShutDown();
        }
    }
}
