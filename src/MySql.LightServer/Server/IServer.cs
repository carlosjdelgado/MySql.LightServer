using MySql.LightServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.LightServer.Server
{
    internal interface IServer
    {
        void Extract(string serverDirectory);
        Process Start(ServerInfo serverInfo);
        void ShutDown();
        bool IsRunning();
    }
}
