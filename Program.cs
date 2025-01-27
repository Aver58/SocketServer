using System;
using System.Linq;

namespace TeddyServer {
    class Program {
        public static int Main(string[] args) {
            // InitUDPServer(args);
            InitTCPServer(args);
            return 0;
        }

        public static void InitTCPServer(string[] args) {
            var tcpServer = new TcpServer();
            tcpServer.Start("127.0.0.1", 8090);
        }

        public static void InitUDPServer(string[] args) {
            KcpUDPServer.Instance.StartWithSocket();
        }

        public static int InitHttpServer(string[] args) {
            if (args.Contains("-h")) {
                Console.WriteLine("Ussage:\n  " + args[0] + "\n\t-h print this message" + "\n\t-d specify root directory(default: ./WWW_Root/)" + "\n\t-p specify listen port(default: 8090)" + "\n\t-l specify listen ip(default: 127.0.0.1)" + "\n");
                return 0;
            }

            string path = GetOption(args, "d");
            if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
                RequestHandler.WebRoot = path;

            string portstr = GetOption(args, "p");
            int port = 8090;
            if (!string.IsNullOrEmpty(portstr))
                int.TryParse(portstr, out port);

            string ip = GetOption(args, "l");
            ip = string.IsNullOrEmpty(ip)? "127.0.0.1" : ip;

            HttpServer.Instance.StartServer(ip, port);
            return 0;
        }

        private static string GetOption(string[] args, string opt) {
            using (var it = (args as System.Collections.Generic.IEnumerable<string>).GetEnumerator()) {
                while (it.MoveNext()) {
                    if (it.Current != null && it.Current[0] == '-' && it.Current.Substring(1) == opt && it.MoveNext()) {
                        return it.Current;
                    }
                }

                return string.Empty;
            }
        }
    }
}
