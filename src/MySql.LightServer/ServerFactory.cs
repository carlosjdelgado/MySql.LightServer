using MySql.LightServer.Enums;
using MySql.LightServer.Server;
using System;
using System.Runtime.InteropServices;

namespace MySql.LightServer
{
    internal static class ServerFactory
    {
        public static Server.Server GetServer(int port, string rootPath)
        {
            var platform = GetOsPlatform();
            switch (platform)
            {
                case OperatingSystem.Windows:
                    return new WindowsServer(rootPath, port);
                case OperatingSystem.Linux:
                    return new LinuxServer(rootPath, port);
            }

            throw new Exception("Current operating system is not supported");
        }

        private static OperatingSystem? GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystem.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OperatingSystem.Linux;
            }

            return null;
        }
    }
}
