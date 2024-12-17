// author:manistein
// since: 2019.03.15
// desc:  TCP session

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server.Framework.Network {
    // 具体负责连接管理，数据包收发处理，错误接收并抛给业务层的类，我们会为每个连接创建一个Session实例
    public class Session {
        public static int MaxPacketSize = 64 * 1024; // max packet size is 64k
        private const int PacketHeaderSize = 2;
        private byte[] m_writeCache;

        private long m_sessionId = 0;
        private int m_opaque = 0;
        private Socket m_socket;

        // Packet process
        private InboundPacketManager m_inboundPacketManager;
        private OutboundPacketManager m_outboundPacketManager;

        // session type:client or server
        private SessionType m_type;

        private SocketAsyncEventArgs m_writeEvent = new SocketAsyncEventArgs();
        private SocketAsyncEventArgs m_readEvent = new SocketAsyncEventArgs();

        private SessionErrorHandle m_onSessionError;
        private ConnectCompleteHandle m_onConnectCompleteHandle; // only for client session
        private IPEndPoint m_remoteEndPoint;
        private string m_errorText = "";
        private ConnectionStatus m_connectionStatus = ConnectionStatus.Invalid;

        private void Init(Socket socket, int opaque, long sessionId, BufferPool bufferPool, SessionErrorHandle errorCallback, ReadCompleteHandle readCallback, UserToken userToken) {
            m_socket = socket;
            m_opaque = opaque;
            m_sessionId = sessionId;

            m_inboundPacketManager = new InboundPacketManager();
            m_outboundPacketManager = new OutboundPacketManager();

            m_inboundPacketManager.Init(opaque, sessionId, bufferPool, readCallback, errorCallback);
            m_outboundPacketManager.Init(bufferPool);
            m_writeCache = new byte[MaxPacketSize + PacketHeaderSize];

            m_onSessionError = errorCallback;

            m_writeEvent.Completed += IO_Complete;
            m_writeEvent.UserToken = userToken;

            m_readEvent.Completed += IO_Complete;
            m_readEvent.UserToken = userToken;
        }

        protected void Stop() {
            m_connectionStatus = ConnectionStatus.Disconnected;
            m_onSessionError(m_opaque, m_sessionId, (int)SessionSocketError.Disconnected, m_errorText); // remove session from tcp_container

            // m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
            m_writeEvent.Dispose();
            m_readEvent.Dispose();
            m_inboundPacketManager.Stop();
            m_outboundPacketManager.Stop();
        }

        public void Close() {
            if (m_outboundPacketManager.HeadBuffer == null) {
                Stop();
            } else {
                // if buffers do not complete write, then turn it to half close status
                m_connectionStatus = ConnectionStatus.Disconnecting;
            }
        }

        public SessionType GetSessionType() {
            return m_type;
        }

        public IPEndPoint GetRemoteEndPoint() {
            return m_remoteEndPoint;
        }

        #region Server

        public void StartAsServer(Socket socket, int opaque, long sessionId, IPEndPoint remoteEndPoint, BufferPool bufferPool, SessionErrorHandle errorCallback, ReadCompleteHandle readCallback, UserToken userToken) {
            Init(socket, opaque, sessionId, bufferPool, errorCallback, readCallback, userToken);
            m_type = SessionType.Server;
            m_connectionStatus = ConnectionStatus.Connected;
            m_remoteEndPoint = remoteEndPoint;

            BeginRecv();
        }

        #region 接受消息

        private void BeginRecv() {
            if (m_connectionStatus != ConnectionStatus.Connected) {
                return;
            }

            m_readEvent.SetBuffer(m_inboundPacketManager.InboundBuffer, 0, m_inboundPacketManager.InboundBuffer.Length);
            bool willRaiseEvent = m_socket.ReceiveAsync(m_readEvent);
            if (!willRaiseEvent) {
                OnRecvComplete(m_readEvent);
            }
        }

        private void OnRecvComplete(object o) {
            if (m_connectionStatus != ConnectionStatus.Connected) {
                return;
            }

            SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
            if (args.SocketError == SocketError.Success) {
                if (args.BytesTransferred == 0) {
                    // 调用socket.Shutdown的时候，对端会收到0字节消息包,需要关闭连接
                    m_onSessionError(m_opaque, m_sessionId, (int)SocketError.Disconnecting, m_errorText);
                } else {
                    m_inboundPacketManager.ProcessPacket(m_inboundPacketManager.InboundBuffer, args.BytesTransferred);
                }
                // main thread开始处理其他事情，main thread不会发生阻塞
                BeginRecv();
            } else {
                m_onSessionError(m_opaque, m_sessionId, (int)args.SocketError, m_errorText);
            }
        }

        #endregion

        #endregion

        #region Client

        public void StartAsClient(Socket socket, int opaque, long sessionId, BufferPool bufferPool, IPEndPoint remoteEndPoint, SessionErrorHandle errorCallback, ReadCompleteHandle readCallback, ConnectCompleteHandle connectCallback, UserToken userToken) {
            Init(socket, opaque, sessionId, bufferPool, errorCallback, readCallback, userToken);
            m_type = SessionType.Client;
            m_remoteEndPoint = remoteEndPoint;
            m_onConnectCompleteHandle = connectCallback;
            m_connectionStatus = ConnectionStatus.Connecting;

            m_errorText = remoteEndPoint.ToString();

            BeginConnect();
        }

        private void BeginConnect() {
            m_writeEvent.RemoteEndPoint = m_remoteEndPoint;
            bool willRaiseEvent = m_socket.ConnectAsync(m_writeEvent);
            if (!willRaiseEvent) {
                OnConnectComplete(m_writeEvent);
            }
        }

        private void OnConnectComplete(object o) {
            if (m_connectionStatus != ConnectionStatus.Connecting) {
                return;
            }

            SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
            if (args.SocketError == SocketError.Success) {
                m_writeEvent.RemoteEndPoint = null;
                UserToken userToken = args.UserToken as UserToken;
                m_onConnectCompleteHandle(m_opaque, m_sessionId, userToken.IP, userToken.Port);
                m_connectionStatus = ConnectionStatus.Connected;

                BeginRecv();
            } else {
                m_onSessionError(m_opaque, m_sessionId, (int)args.SocketError, m_errorText);
            }
        }

        private void OnDisconnectComplete(object o) {
            m_onSessionError(m_opaque, m_sessionId, (int)SocketError.Disconnecting, m_errorText);
        }

        #endregion

        // socket thread 负责生产IO事件
        private void IO_Complete(object sender, object o) {
            SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
            switch (args.LastOperation) {
                case SocketAsyncOperation.Connect:
                    TCPSynchronizeContext.GetInstance().Post(OnConnectComplete, o);
                    break;
                case SocketAsyncOperation.Disconnect:
                    TCPSynchronizeContext.GetInstance().Post(OnDisconnectComplete, o);
                    break;
                case SocketAsyncOperation.Receive:
                    TCPSynchronizeContext.GetInstance().Post(OnRecvComplete, o);
                    break;
                case SocketAsyncOperation.Send:
                    TCPSynchronizeContext.GetInstance().Post(OnWriteComplete, o);
                    break;
                default:
                    throw new Exception("socket error: " + args.LastOperation);
            }
        }

        #region 发送消息

        public bool Write(byte[] buffer) {
            if (m_connectionStatus != ConnectionStatus.Connected) {
                return false;
            }

            // 检查传入的buffer（数据包）的大小是否超出了最大允许的包大小
            if (buffer.Length > MaxPacketSize) {
                return false;
            }

            int packetSize = buffer.Length;
            // 使用一个ushort来存储数据包的大小，因为一个ushort是2个字节，可以存储的最大值是65535
            // gpt的写法是 BitConverter.GetBytes((ushort)buffer.Length);
            // 设置包头大小（使用大端字节序，即高字节在前）
            // 第一个字节存储数据包的高字节（packetSize的高8位）
            m_writeCache[0] = (byte)(packetSize >> 8); // 高字节（8位）
            // 第二个字节存储数据包的低字节（packetSize的低8位）
            m_writeCache[1] = (byte)(packetSize & 0xff); // 低字节（8位）

            // 将数据包内容拷贝到 m_writeCache 中，从包头之后的位置开始存放
            // PacketHeaderSize 是包头的大小，通常是2个字节（高字节和低字节）
            buffer.CopyTo(m_writeCache, PacketHeaderSize);

            m_outboundPacketManager.ProcessPacket(m_writeCache, packetSize + PacketHeaderSize);

            // 如果OutboundPacketManager没有头部缓冲区，说明没有更多的数据需要立即写入
            if (m_outboundPacketManager.HeadBuffer == null) {
                BeginWrite();
            }

            return true;
        }

        private void BeginWrite() {
            if (m_outboundPacketManager.HeadBuffer == null) {
                m_outboundPacketManager.NextBuffer();
            }

            if (m_outboundPacketManager.HeadBuffer == null) {
                if (m_connectionStatus == ConnectionStatus.Disconnecting) {
                    Stop();
                }

                return;
            }

            Buffer buf = m_outboundPacketManager.HeadBuffer;
            // 将上一步处理的头一个buffer指定为应用层的write buffer
            m_writeEvent.SetBuffer(buf.Memory, buf.Begin, buf.End - buf.Begin);
            bool willRaiseEvent = m_socket.SendAsync(m_writeEvent);
            if (!willRaiseEvent) {
                OnWriteComplete(m_writeEvent);
            }
        }

        private void OnWriteComplete(object o) {
            SocketAsyncEventArgs args = o as SocketAsyncEventArgs;
            if (args.SocketError == SocketError.Success) {
                if (args.BytesTransferred == 0) {
                    m_onSessionError(m_opaque, m_sessionId, (int)SocketError.Disconnecting, m_errorText);
                } else {
                    Buffer buf = m_outboundPacketManager.HeadBuffer;
                    buf.Begin += args.BytesTransferred;
                    if (buf.Begin >= buf.End) {
                        m_outboundPacketManager.NextBuffer();
                    }

                    // 客户端main thread继续执行其他逻辑
                    BeginWrite();
                }
            } else {
                if (m_connectionStatus == ConnectionStatus.Disconnecting) {
                    Stop();
                } else {
                    m_onSessionError(m_opaque, m_sessionId, (int)args.SocketError, m_errorText);
                }
            }
        }

        #endregion
    }

    public enum SessionSocketError {
        Disconnected = 90001,

    }

    public enum ConnectionStatus {
        Invalid,
        Connecting,
        Connected,
        Disconnecting, // Half close
        Disconnected,
    }

    public enum SessionType {
        Client = 0,
        Server = 1,
    }

    public class UserToken {
        public string IP { get; set; }
        public int Port { get; set; }
    }
}