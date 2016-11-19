using System.Diagnostics;

namespace MySql.LightServer.Server
{
    internal interface IServer
    {
        void Extract();
        void Start();
        void ShutDown();
        bool IsRunning();
        void Clear();
        string GetConnectionString();
        int GetPort();
    }
}
