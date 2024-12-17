using System;
using System.Linq;
using System.Text;
using Server.Framework.Network;

namespace Server.Test {
    public class TestClient {
        private UserData m_userData;
        private bool m_isConnected;
        private int m_receiveCount = 0;
        private int m_hashCode = 0;

        public void InitTCPClient() {
            var client = new TCPClient();
            client.Start(0, OnSessionError, OnReadPacketComplete, ConnectCompleteHandle);
            var ip = "127.0.0.1";
            var port = 8090;
            client.Connect(ip, port);

            Session session = client.GetSessionBy(m_userData.SessionId);
            if (session != null) {
                char[] arr = { 'H', 'e', 'l', 'l', 'o' };
                string content = "";
                Random rand = new Random(DateTime.Now.Millisecond);
                int contentSize = rand.Next(5, 15);
                for (int i = 0; i < contentSize; i++)
                {
                    content += arr[i % arr.Count()];
                }
                m_hashCode = content.GetHashCode();
                byte[] buffers = Encoding.ASCII.GetBytes(content);
                session.Write(buffers);
            }
        }

        private void ConnectCompleteHandle(int opaque, long sessionId, string ip, int port) {
            if (m_userData != null) {
                Console.WriteLine("sessionId:{0} is already exist", sessionId);
            } else {
                m_userData = new UserData();
                m_userData.SessionId = sessionId;
                m_userData.IP = ip;
                m_userData.Port = port;
                m_isConnected = true;
                Console.WriteLine("new session:{0} connected, ip:{1}, port:{2}", sessionId, ip, port);
            }
        }

        private void OnReadPacketComplete(int opaque, long sessionId, byte[] bytes, int packetSize) {
            m_receiveCount++;

            Console.WriteLine("OnReadPacketComplete sessionId:{0} content:{1} hashCode:{2} packetSize:{3} timestamp:{4} receiveCount:{5}",
                sessionId,
                Encoding.ASCII.GetString(bytes, 0, packetSize),
                Encoding.ASCII.GetString(bytes, 0, packetSize).GetHashCode() == m_hashCode,
                packetSize,
                DateTime.Now,
                m_receiveCount);
        }

        private void OnSessionError(int opaque, long sessionId, string remoteendpoint, int errorCode, string errorText) {
            m_isConnected = false;
            Console.WriteLine("OnSessionError sessionId:{0} errorCode:{1} errorText:{2}", sessionId, errorCode, errorText);
        }

        private class UserData {
            public long SessionId { get; set; }
            public string IP { get; set; }
            public int Port { get; set; }
        }
    }
}