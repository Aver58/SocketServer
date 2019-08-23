using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;

namespace Server
{
    class Program
    {
        private const int port = 8088;
        private static string IpStr = "127.0.0.1";

        static void Main(string[] args)
        {
            SocketServer.Instance.InitSocket(IpStr, port);
        }
    }
}
