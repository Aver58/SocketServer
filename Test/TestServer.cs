using System;
using System.Net.Sockets;
using Server.Framework.Network;

namespace Server.Test {
    public class TestServer {
        private TCPServer server;
        public void InitTCPServer() {
            var ip = "127.0.0.1";
            var port = 8090;
            server = new TCPServer();
            server.Start(ip, port, 30, 0, OnSocketError, OnSocketData, OnSocketAccept);
        }

        private void OnSocketAccept(int opaque, long sessionId, string ip, int port) {
            Console.WriteLine($"OnSocketAccept: sessionId:{sessionId} ip {ip} port {port}");
            Session session = server.GetSessionBy(sessionId);

            // session.Write()
        }

        private void OnSocketData(int opaque, long sessionId, byte[] bytes, int packetSize) {
            Console.WriteLine($"OnSocketData: sessionId:{sessionId} bytes.Length {bytes.Length} packetSize {packetSize}");
        }

        private void OnSocketError(int opaque, long sessionId, string remoteendpoint, int errorCode, string errorText) {
            Console.WriteLine("OnSessionError sessionId:{0} errorCode:{1} errorText:{2}", sessionId, errorCode, errorText);
        }
    }
}