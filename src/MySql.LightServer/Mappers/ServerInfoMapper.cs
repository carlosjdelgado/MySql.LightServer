using MySql.Server.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Server.Mappers
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
