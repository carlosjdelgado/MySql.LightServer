using MySql.LightServer.Models;
using System.Collections.Generic;
using System.IO;

namespace MySql.LightServer.Mappers
{
    internal static class ServerInfoMapper
    {
        public static List<string> ToDirectoryList(ServerInfo serverInfo)
        {
            return new List<string>()
            {
                serverInfo.ServerDirectory,
                serverInfo.DataRootDirectory,
                Path.Combine(serverInfo.DataRootDirectory, serverInfo.ServerGuid.ToString())
            };
        }


    }
}
