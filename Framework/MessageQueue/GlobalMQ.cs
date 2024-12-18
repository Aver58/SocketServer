using System.Collections.Concurrent;

namespace Server.Framework.MessageQueue {
    public class GlobalMQ : Singleton<GlobalMQ> {
        private ConcurrentQueue<int> m_serviceQueue = new ConcurrentQueue<int>();

        public void Push(int serviceId) {
            m_serviceQueue.Enqueue(serviceId);
        }

        public int Pop() {
            int serviceId = 0;
            if (!m_serviceQueue.TryDequeue(out serviceId)) {
                return 0;
            }

            return serviceId;
        }
    }
}