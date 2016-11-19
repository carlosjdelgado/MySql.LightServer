using MySql.Data.MySqlClient;
using System.Diagnostics;
using NUnit.Framework;
using System;

namespace MySql.LightServer.Tests
{
    [TestFixture]
    public class MySqlLightServerTests
    {
        [TestCase]
        public void KillProcess()
        {
            var previousProcessCount = Process.GetProcessesByName("mysqld").Length;
            var database = new MySqlLightServer();

            database.StartServer();
            database.ShutDown();

            Assert.AreEqual(previousProcessCount, Process.GetProcessesByName("mysqld").Length, "should kill the running process");
        }

        [TestCase]
        public void StartServerOnSpecifiedPort()
        {
            var server = new MySqlLightServer(3306);
            server.StartServer();
            MySqlHelper.ExecuteNonQuery(server.ConnectionString, "CREATE DATABASE testserver; USE testserver;");
            server.ShutDown();
        }

        [TestCase]
        public void MultipleProcessesInARow()
        {
            var dbServer = new MySqlLightServer();
            dbServer.StartServer();
            dbServer.ShutDown();
            dbServer.StartServer();
            dbServer.ShutDown();
        }
    }
}
