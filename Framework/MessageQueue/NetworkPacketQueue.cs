using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TeddyServer.Framework.MessageQueue {
    class NetworkPacketQueue : Singleton<NetworkPacketQueue> {
        private ConcurrentQueue<SocketMessage> m_netpackQueue = new ConcurrentQueue<SocketMessage>();

        public void Push(SocketMessage socketMessage) {
            m_netpackQueue.Enqueue(socketMessage);
        }

        public SocketMessage Pop() {
            SocketMessage socketMessage = null;
            m_netpackQueue.TryDequeue(out socketMessage);
            return socketMessage;
        }
    }

    enum SocketMessageType {
        Connect = 1,
        Disconnect = 2,
        DATA = 3,
    }

    class SocketMessage {
        public SocketMessageType Type { get; set; }
        public int TcpObjectId { get; set; }
    }

    class ConnectMessage : SocketMessage {
        public string IP { get; set; }
        public int Port { get; set; }
    }

    class DisconnectMessage : SocketMessage {
        public long ConnectionId { get; set; }
    }

    class NetworkPacket : SocketMessage {
        public long ConnectionId { get; set; }
        public List<byte[]> Buffers { get; set; }
    }
}