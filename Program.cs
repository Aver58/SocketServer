using System;
using System.Linq;
using TeddyServer.Test;

namespace TeddyServer {
    class Program {
        public static int Main(string[] args)
        {
            string inputMode = args[0];
            int mode = 0;
            if (inputMode == "TestCases") {
                mode = 1;
            } else if (inputMode == "SparkServer") {
                mode = 2;
            } else {
                Console.WriteLine("Unknow input mode {0}", inputMode);
                return 0;
            }

            switch (mode) {
                case 1: {
                    string caseName = args[1];

                    TestCases testCases = new TestCases();
                    testCases.Run(caseName);
                }
                    break;
                case 2: {
                    // string bootService = args[1];
                    // string bootPath = args[2];
                    // string bootServiceName = "";
                    //
                    // if (args.Length >= 4) {
                    //     bootServiceName = args[3];
                    // }
                    //
                    // BootServices startFunc = delegate() {
                    //     SparkServerUtility.NewService(bootService, bootServiceName);
                    // };
                    //
                    // Server battleServer = new Server();
                    // battleServer.Run(bootPath, startFunc);
                }
                    break;
                default: {
                    Console.WriteLine("Mode:{0} not supported", mode);
                }
                    break;
            }

            // InitUDPServer(args);
            return 0;
        }

        public static void InitTCPServer(string[] args)
        {
            TcpServerDemo.Instance.StartTcpServer();
        }

        public static void InitUDPServer(string[] args)
        {
            KcpUDPServer.Instance.StartWithSocket();
        }

        public static int InitHttpServer(string[] args)
        {
            if (args.Contains("-h"))
            {
                Console.WriteLine("Ussage:\n  " + args[0]
                                  + "\n\t-h print this message"
                                  + "\n\t-d specify root directory(default: ./WWW_Root/)"
                                  + "\n\t-p specify listen port(default: 8090)"
                                  + "\n\t-l specify listen ip(default: 127.0.0.1)"
                                  + "\n");
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
            ip = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;

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
