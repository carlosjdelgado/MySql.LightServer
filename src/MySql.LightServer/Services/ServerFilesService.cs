using MySql.LightServer.Enums;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MySql.LightServer.Services
{
    public static class ServerFilesService
    {
        private const string LightServerAssemblyName = "MySql.LightServer";

        private const string Win32MySqlFileName = "mysqld.exe";
        private const string LinuxMySqlFileName = "mysqld";
        private const string ErrmsgFileName = "errmsg.sys";

        private const string ErrmsgResourceName = "MySql.LightServer.ServerFiles.errmsg.sys";
        private const string Win32MySqlResourceName = "MySql.LightServer.ServerFiles.Win32.mysqld.exe";
        private const string LinuxMySqlResourceName = "MySql.LightServer.ServerFiles.Linux.mysqld";

        public static void Extract(string serverDirectory)
        {
            var platform = GetOsPlatform();
            if (!ServerIsDeployed(serverDirectory, platform))
            {
                switch(platform)
                {
                    case OperatingSystem.Linux:
                        ExtractLinuxServer(serverDirectory);
                        break;
                    case OperatingSystem.Windows:
                        ExtractWindowsServer(serverDirectory);
                        break;
                }
            }
        }

        private static bool ServerIsDeployed(string serverDirectory, OperatingSystem platform)
        {
            switch (platform)
            {
                case OperatingSystem.Windows:
                    return new FileInfo(Path.Combine(serverDirectory, Win32MySqlFileName)).Exists;
                case OperatingSystem.Linux:
                    return new FileInfo(Path.Combine(serverDirectory, LinuxMySqlFileName)).Exists;
            }

            return false;
        }

        private static void ExtractWindowsServer(string serverDirectory)
        {
            var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

            var errmsg = assembly.GetManifestResourceStream(ErrmsgResourceName);
            var mysqld = assembly.GetManifestResourceStream(Win32MySqlResourceName);

            FileSystemService.CopyStreamToFile(errmsg, Path.Combine(serverDirectory, ErrmsgFileName));
            FileSystemService.CopyStreamToFile(mysqld, Path.Combine(serverDirectory, Win32MySqlFileName));
        }

        private static void ExtractLinuxServer(string serverDirectory)
        {
            var assembly = Assembly.Load(new AssemblyName(LightServerAssemblyName));

            var errmsg = assembly.GetManifestResourceStream(ErrmsgResourceName);
            var mysqld = assembly.GetManifestResourceStream(LinuxMySqlResourceName);

            FileSystemService.CopyStreamToFile(errmsg, Path.Combine(serverDirectory, ErrmsgFileName));
            FileSystemService.CopyStreamToFile(mysqld, Path.Combine(serverDirectory, LinuxMySqlFileName));
        }

        private static OperatingSystem GetOsPlatform()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystem.Windows;
            }

            return OperatingSystem.Linux;
        }
    }
}
