using System;
using System.Collections.Generic;

namespace Server.Framework.Network {
    /// <summary>
    /// 数据包发送管理器
    /// 处理发送的数据包，在应用层的数据包发完以后，会从buffer队列中取出下一个buffer，让socket发送
    /// </summary>
    public class OutboundPacketManager {
        private Queue<Buffer> m_outboundBuffers = new Queue<Buffer>();
        private Buffer m_lastBuffer;
        private BufferPool m_bufferPool;

        public Buffer HeadBuffer { get; set; }

        public void Init(BufferPool buffer) {
            m_bufferPool = buffer;
        }

        public void ProcessPacket(byte[] buffer, int transferedBytes) {
            int sourceIndex = 0;

            while (transferedBytes > 0) {
                // 判断当前最后一个缓存（m_lastBuffer）是否已经满或者为null
                if (m_lastBuffer == null || m_lastBuffer.End >= m_lastBuffer.Memory.Length) {
                    // 如果缓存满了或者没有缓存，需要从buffer池中获取一个新的缓冲区
                    Buffer newBuffer = m_bufferPool.Pop();
                    m_outboundBuffers.Enqueue(newBuffer);
                    m_lastBuffer = newBuffer;
                }

                // 判断剩余要处理的字节数是否大于当前缓冲区剩余的空间
                if (transferedBytes > m_lastBuffer.Memory.Length - m_lastBuffer.End) {
                    // 如果剩余字节数超过当前缓冲区的剩余空间，先把当前缓冲区填满
                    Array.Copy(buffer, sourceIndex, m_lastBuffer.Memory, m_lastBuffer.End, m_lastBuffer.Memory.Length - m_lastBuffer.End);
                    // 更新源数据的位置
                    sourceIndex += m_lastBuffer.Memory.Length - m_lastBuffer.End;
                    // 更新剩余字节数
                    transferedBytes -= m_lastBuffer.Memory.Length - m_lastBuffer.End;
                    // 缓存区已满，更新End索引
                    m_lastBuffer.End = m_lastBuffer.Memory.Length;
                } else {
                    // 如果剩余的字节数不超过当前缓冲区剩余的空间，直接将数据拷贝到缓冲区
                    Array.Copy(buffer, sourceIndex, m_lastBuffer.Memory, m_lastBuffer.End, transferedBytes);

                    // 更新源数据的位置
                    sourceIndex += transferedBytes;
                    // 更新缓冲区的End索引
                    m_lastBuffer.End += transferedBytes;
                    // 所有数据已经处理完，设置transferedBytes为0，退出循环
                    transferedBytes = 0;
                }
            }
        }

        public void NextBuffer() {
            Buffer oldBuffer = HeadBuffer;

            if (m_outboundBuffers.Count > 0) {
                HeadBuffer = m_outboundBuffers.Dequeue();
                // m_lastbuffer will be wrote in tcp thread
                // HeadBuffer will be wrote in system socket thread
                // so, we must avoid different threads write data in the same buffer
                if (HeadBuffer == m_lastBuffer) {
                    m_lastBuffer = null;
                }
            } else {
                HeadBuffer = null;
                m_lastBuffer = null;
            }

            if (oldBuffer != null) {
                m_bufferPool.Push(oldBuffer);
            }
        }

        public void Stop() {
            if (m_outboundBuffers.Count <= 0)
                return;

            Buffer buf = m_outboundBuffers.Dequeue();
            while (buf != null) {
                m_bufferPool.Push(buf);

                if (m_outboundBuffers.Count > 0)
                    buf = m_outboundBuffers.Dequeue();
                else
                    buf = null;
            }
        }
    }
}