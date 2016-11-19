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
        void Extract();
        Process Start();
        void ShutDown();
        bool IsRunning();
        void Clear();
        string GetConnectionString();
    }
}
