using MySql.LightServer.Enums;
using MySql.LightServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MySql.LightServer
{
    internal static class ServerFactory
    {
        public static IServer GetServer()
        {
            var platform = GetOsPlatform();
            switch (platform)
            {
                case OperatingSystem.Windows:
                    return new WindowsServer();
                case OperatingSystem.Linux:
                    return new LinuxServer();
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
