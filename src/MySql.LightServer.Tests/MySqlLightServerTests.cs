using MySql.Data.MySqlClient;
using System.Diagnostics;
using NUnit.Framework;

namespace MySql.LightServer.Tests
{
    [TestFixture]
    public class MySqlLightServerTests
    {
        [TestCase]
        public void UsingServer()
        {
            var dbServer = new MySqlLightServer();
            dbServer.StartServer();

            MySqlHelper.ExecuteNonQuery(dbServer.ConnectionString, string.Format("CREATE DATABASE {0};USE {0};", "testserver"));

            MySqlHelper.ExecuteNonQuery(dbServer.ConnectionString, "CREATE TABLE testTable (`id` INT NOT NULL, `value` CHAR(150) NULL,  PRIMARY KEY (`id`)) ENGINE = MEMORY;");

            MySqlHelper.ExecuteNonQuery(dbServer.ConnectionString, "INSERT INTO testTable (`id`,`value`) VALUES (1, 'some value')");
            MySqlHelper.ExecuteNonQuery(dbServer.ConnectionString, "INSERT INTO testTable (`id`, `value`) VALUES (2, 'test value')");

            using (MySqlDataReader reader = MySqlHelper.ExecuteReader(dbServer.ConnectionString, "select * from testTable WHERE id = 2"))
            {
                reader.Read();
                Assert.AreEqual("test value", reader.GetString("value"), "Inserted and read string should match");
            }

            dbServer.ShutDown();
        }

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
