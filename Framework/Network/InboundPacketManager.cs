using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Server.Framework.Network {
    /// <summary>
    /// 数据包接收管理器
    /// 处理接收到的数据包，在应用层的数据包收齐后，会通过回调函数通知业务层
    /// </summary>
    /*
         分包： 由于 TCP 缓冲区的大小限制，发送的数据可能会被拆分成多个数据包进行传输，接收端可能会接收到一个不完整的消息。
         粘包： 由于 TCP 流是连续的，多个小的消息会被组合成一个大的数据包。接收端无法知道什么时候一条消息结束，什么时候另一条消息开始。
         解决： 消息头 + 消息体：在每个消息的开头添加一个固定长度的消息头，消息头包含消息体的长度。接收方首先读取消息头，获得消息体的长度，再根据该长度读取完整的消息体。
                收齐多少个字节后，再将整包转给业务模块。
     */
    public class InboundPacketManager {
        // inbound buffer、outbound buffer and memory buffer is 64k
        private int BUFFER_SIZE = 64 * 1024;

        private Queue<Buffer> m_readBuffers = new Queue<Buffer>();
        private Buffer m_lastBuffer;

        private int m_packetSize = 0;
        private int m_readNum = 0;
        private int m_readHeader = 0;

        private BufferPool m_bufferPool;
        public byte[] InboundBuffer { get; set; }
        public byte[] MemoryBuffer { get; set; }

        private ReadCompleteHandle m_onReadPacketComplete;
        private SessionErrorHandle m_onSessionError;
        private long m_sessionId = 0;
        private int m_serverId = 0;

        public InboundPacketManager() {
            InboundBuffer = new byte[BUFFER_SIZE];
            MemoryBuffer = new byte[BUFFER_SIZE];
        }

        public void Init(int opaque, long sessionId, BufferPool bufferPool, ReadCompleteHandle readCallback, SessionErrorHandle errorCallback) {
            m_sessionId = sessionId;
            m_serverId = opaque;
            m_bufferPool = bufferPool;
            m_onReadPacketComplete = readCallback;
            m_onSessionError = errorCallback;
        }

        public void ProcessPacket(byte[] inboundBytes, int transferedBytes) {
            int sourceIndex = 0; // 用于跟踪传输字节的当前索引位置

            // 当还有字节需要处理时
            while (transferedBytes > 0) {
                // 处理第一个字节，读取包头
                // 只有在没有读取包头（m_readNum == 0），且包大小和包头都没有被确定时才进入这个分支
                if (transferedBytes == 1 && m_readNum == 0 && m_packetSize == 0) {
                    m_readHeader = inboundBytes[sourceIndex]; // 读取包头的第一个字节
                    m_readNum = -1; // 标记接下来进入包头解析阶段
                    transferedBytes -= 1; // 已经处理了一个字节，减少传输字节数量
                    sourceIndex++; // 移动到下一个字节
                } else {
                    // 如果包头已经读取过（m_readNum == -1），那么接下来读取包大小
                    if (m_readNum == -1) {
                        // 获取包大小：包头的第一个字节和第二个字节组合成包的大小
                        m_packetSize = m_readHeader << 8 | inboundBytes[sourceIndex];
                        m_readNum = 0; // 开始读取包体
                        transferedBytes -= 1; // 读取了一个字节，减少传输字节数量
                        sourceIndex++; // 移动到下一个字节

                        // 如果包的大小超过了最大限制，认为这是非法连接
                        if (m_packetSize > Session.MaxPacketSize) {
                            m_onSessionError(m_serverId, m_sessionId, (int)SocketError.Disconnecting, "May be this is a illegal connection");
                            break;
                        }
                    }
                    // 当包体还没有读取完成时（m_readNum == 0），或者我们正在处理包体的过程中
                    else if (m_readNum == 0) {
                        // 如果包体大小为 0（可能的边界情况），则重新计算包大小
                        if (m_packetSize == 0) {
                            m_packetSize = inboundBytes[sourceIndex] << 8 | inboundBytes[sourceIndex + 1];
                            transferedBytes -= 2; // 读取了两个字节，减少传输字节数量
                            sourceIndex += 2; // 移动到下一个字节

                            // 检查包的大小是否有效
                            if (m_packetSize > Session.MaxPacketSize) {
                                m_onSessionError(m_serverId, m_sessionId, (int)SocketError.Disconnecting, "May be this is a illegal connection");
                                break;
                            }
                        }

                        // 如果当前接收到的数据包足够大，可以一次性读取完整包
                        if (transferedBytes >= m_packetSize) {
                            // 将收到的数据拷贝到内存缓存中
                            Array.Copy(inboundBytes, sourceIndex, MemoryBuffer, 0, m_packetSize);

                            // 创建一个新的缓冲区以供应用处理
                            byte[] transferBuffer = new byte[m_packetSize];
                            Array.Copy(MemoryBuffer, 0, transferBuffer, 0, m_packetSize);

                            // 调用回调函数处理接收到的包
                            m_onReadPacketComplete(m_serverId, m_sessionId, transferBuffer, m_packetSize);

                            // 更新剩余的字节数和当前位置
                            transferedBytes -= m_packetSize;
                            sourceIndex += m_packetSize;

                            // 重置读取状态
                            m_readHeader = 0;
                            m_readNum = 0;
                            m_packetSize = 0;
                        } else {
                            // 如果数据不足以读取完整包，处理不完整的包（分包问题）
                            ProcessUncomplete(ref inboundBytes, ref transferedBytes, ref sourceIndex);
                        }
                    }
                    // 处理剩余数据，如果包体的内容大于一个数据块，我们需要通过缓冲区进行分块处理
                    else {
                        // 判断当前接收到的数据是否足够填满一个包
                        if (m_readNum + transferedBytes >= m_packetSize) {
                            int leftBytes = m_packetSize - m_readNum; // 剩余需要读取的字节数
                            Buffer head = m_readBuffers.Dequeue(); // 从缓存队列中取出一个缓冲区
                            int destIndex = 0;

                            // 继续从缓冲区复制数据
                            while (head != null) {
                                Array.Copy(head.Memory, head.Begin, MemoryBuffer, destIndex, head.End - head.Begin);
                                destIndex += head.End - head.Begin;
                                m_bufferPool.Push(head); // 将缓冲区推回池中

                                // 检查缓存队列中是否还有其他缓冲区
                                if (m_readBuffers.Count > 0) {
                                    head = m_readBuffers.Dequeue();
                                } else {
                                    head = null;
                                }
                            }

                            // 将当前收到的数据拷贝到内存缓冲区中
                            Array.Copy(inboundBytes, sourceIndex, MemoryBuffer, destIndex, leftBytes);

                            // 创建一个新的缓冲区以供应用处理
                            byte[] transferBuffer = new byte[m_packetSize];
                            Array.Copy(MemoryBuffer, 0, transferBuffer, 0, m_packetSize);

                            // 调用回调函数处理接收到的包
                            m_onReadPacketComplete(m_serverId, m_sessionId, transferBuffer, m_packetSize);

                            // 更新剩余的字节数和当前位置
                            transferedBytes -= leftBytes;
                            sourceIndex += leftBytes;

                            // 重置读取状态
                            m_packetSize = 0;
                            m_readHeader = 0;
                            m_readNum = 0;
                        } else {
                            // 如果数据不足，继续缓冲剩余的数据
                            int freeBytes = m_lastBuffer.Memory.Length - m_lastBuffer.End;
                            if (freeBytes > 0) {
                                // 填充缓冲区
                                int fillBytes = Math.Min(transferedBytes, freeBytes);
                                Array.Copy(inboundBytes, sourceIndex, m_lastBuffer.Memory, m_lastBuffer.End, fillBytes);

                                m_lastBuffer.End += fillBytes;
                                m_readNum += fillBytes;

                                sourceIndex += fillBytes;
                                transferedBytes -= fillBytes;
                            }

                            // 如果传输的字节还没全部处理完，继续处理剩余部分
                            if (transferedBytes > 0) {
                                ProcessUncomplete(ref inboundBytes, ref transferedBytes, ref sourceIndex);
                            }
                        }
                    }
                }
            }
        }

        public void Stop() {
            if (m_readBuffers.Count <= 0)
                return;

            Buffer buf = m_readBuffers.Dequeue();
            while (buf != null) {
                m_bufferPool.Push(buf);

                if (m_readBuffers.Count > 0)
                    buf = m_readBuffers.Dequeue();
                else
                    buf = null;
            }
        }

        private void ProcessUncomplete(ref byte[] inboundBytes, ref int transferedBytes, ref int sourceIndex) {
            int needBlocks = (int)Math.Ceiling((float)transferedBytes / (float)BUFFER_SIZE);
            for (int i = 0; i < needBlocks; i++) {
                Buffer buf = m_bufferPool.Pop();
                int copyBlockSize = Math.Min(transferedBytes, buf.Memory.Length);
                Array.Copy(inboundBytes, sourceIndex, buf.Memory, buf.End, copyBlockSize);
                buf.End += copyBlockSize;

                m_readBuffers.Enqueue(buf);
                m_lastBuffer = buf;
                m_readNum += copyBlockSize;

                transferedBytes -= copyBlockSize;
                sourceIndex += copyBlockSize;
            }
        }
    }
}