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
                case Enums.OperatingSystem.Windows:
                    return new WindowsServer(rootPath, port);
                case Enums.OperatingSystem.Linux:
                    return new LinuxServer(rootPath, port);
            }

            throw new Exception("Current operating system is not supported");
        }

#if NETSTANDARD1_6
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
#endif

#if NET452 || NET46 || NET461 || NET462
        private static Enums.OperatingSystem? GetOsPlatform()
        {
            if(Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return Enums.OperatingSystem.Linux;
            }
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Enums.OperatingSystem.Windows;
            }

            return null;
        }
#endif
    }
}
