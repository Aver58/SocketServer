using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Server.Framework.Network {
    // 我们的IO响应函数，首先会在socket线程内调用，
    // 这个类是个单例，内部有个函数对象（Action实例）的加锁队列，IO响应函数会将想调用的函数包装成函数对象，
    // 并且插入这个队列中，运行在主线程的TCPServer或TCPClient实例会不断从这个加锁队列中，pop出函数队列，并且执行，
    // 这样做的目的是所有的IO处理都在主线程上执行，从而降低整体设计的复杂度
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