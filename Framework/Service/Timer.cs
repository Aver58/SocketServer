using System;
using System.Collections.Concurrent;

namespace Server.Framework.Service {
    class SSTimer : Singleton<SSTimer> {
        private ConcurrentQueue<SSTimerNode> m_timerNodeQueue = new ConcurrentQueue<SSTimerNode>();

        public void Loop() {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int count = m_timerNodeQueue.Count;
            for (int i = 0; i < count; i++) {
                SSTimerNode timerNode = null;
                if (m_timerNodeQueue.TryDequeue(out timerNode)) {
                    if (timestamp >= timerNode.TimeoutTimestamp) {
                        Message msg = new Message();
                        msg.Source = 0;
                        msg.Destination = timerNode.Opaque;
                        msg.Method = "";
                        msg.Data = null;
                        msg.RPCSession = timerNode.Session;
                        msg.Type = MessageType.Timer;

                        ServiceContext service = ServiceSlots.Instance.Get(timerNode.Opaque);
                        service.Push(msg);
                    } else {
                        m_timerNodeQueue.Enqueue(timerNode);
                    }
                } else {
                    break;
                }
            }
        }

        public void Add(SSTimerNode node) {
            m_timerNodeQueue.Enqueue(node);
        }
    }

    class SSTimerNode {
        public int Opaque { get; set; }
        public long TimeoutTimestamp { get; set; }
        public int Session { get; set; }
    }
}