using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Server.Framework.Network {
    public class TCPSynchronizeContext : SynchronizationContext {
        private static TCPSynchronizeContext m_instance;
        private int m_threadId = Thread.CurrentThread.ManagedThreadId;

        private ConcurrentQueue<Action> m_concurrentQueue = new ConcurrentQueue<Action>();
        private Action action;

        public TCPSynchronizeContext() { }

        // this function must call in tcp thread first
        public static TCPSynchronizeContext GetInstance() {
            if (m_instance == null) {
                m_instance = new TCPSynchronizeContext();
            }

            return m_instance;
        }

        public void Loop() {
            while (true) {
                if (m_concurrentQueue.TryDequeue(out action)) {
                    action();
                } else {
                    break;
                }
            }
        }

        public override void Post(SendOrPostCallback callback, object state) {
            m_concurrentQueue.Enqueue(() => {
                callback(state);
            });
        }
    }
}