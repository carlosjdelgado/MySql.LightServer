using MySql.Data.MySqlClient;
using System.Diagnostics;
using NUnit.Framework;

namespace MySql.LightServer.Tests
{
    [TestFixture]
    public class MySqlLightServerTests
    {
        [TestCase]
        public void KillProcess()
        {
            var previousProcessCount = Process.GetProcessesByName("mysqld").Length;
            
            var database = MySqlLightServer.Instance;
            database.StartServer();
            database.ShutDown();
            
            Assert.AreEqual(previousProcessCount, Process.GetProcessesByName("mysqld").Length, "should kill the running process");
        }

        [TestCase]
        public void StartServerOnSpecifiedPort()
        {
            var server = MySqlLightServer.Instance;
            server.StartServer(3366);
            MySqlHelper.ExecuteNonQuery(server.ConnectionString, "CREATE DATABASE testserver; USE testserver;");
            server.ShutDown();
        }

        [TestCase]
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
