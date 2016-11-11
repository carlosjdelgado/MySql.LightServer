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
                Path.Combine(serverInfo.ServerDirectory, serverInfo.ServerGuid.ToString())
            };
        }


    }
}
