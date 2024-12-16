using System.Collections.Generic;

namespace Server.Framework.Network {
    public class Buffer
    {
        // buffer's size is 4k
        public const int Size = 4096;

        public Buffer()
        {
            Memory = new byte[Size];
            Begin = 0;
            End = 0;
        }

        public byte[] Memory;
        public int Begin;
        public int End;
    }

    public class BufferPool
    {
        private const int InitBufferCount = 1024;
        private Queue<Buffer> m_queue;

        public BufferPool()
        {
            m_queue = new Queue<Buffer>(InitBufferCount);
        }

        public Buffer Pop()
        {
            Buffer buffer = null;
            if (m_queue.Count <= 0)
            {
                buffer = new Buffer();
            }
            else
            {
                buffer = m_queue.Dequeue();
            }

            buffer.Begin = 0;
            buffer.End = 0;
            return buffer;
        }

        public void Push(Buffer buffer)
        {
            buffer.Begin = 0;
            buffer.End = 0;
            m_queue.Enqueue(buffer);
        }
    }
}