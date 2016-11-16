using MySql.LightServer.Enums;
using MySql.LightServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MySql.LightServer.Models;

namespace MySql.LightServer
{
    internal static class ServerFactory
    {
        public static IServer GetServer(ServerInfo serverInfo)
        {
            var platform = GetOsPlatform();
            switch (platform)
            {
                case OperatingSystem.Windows:
                    return new WindowsServer(serverInfo);
                case OperatingSystem.Linux:
                    return new LinuxServer(serverInfo);
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
