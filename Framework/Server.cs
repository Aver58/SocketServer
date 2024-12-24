using System;
using System.Threading;
using NetSprotoType;
using Newtonsoft.Json.Linq;
using TeddyServer.Framework.MessageQueue;
using TeddyServer.Framework.Network;
using TeddyServer.Framework.Service;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Framework {
    public delegate void BootServices();
    public class Server {
        private string m_gateIp;
        private int m_gatePort = 0;
        private JObject m_bootConfig;
        private TCPServer m_tcpGate;
        private int m_workerNum = 8;
        private GlobalMQ m_globalMQ;
        private ServiceSlots m_serviceSlots;
        private NetworkPacketQueue m_netpackQueue;
        private SSTimer m_timer;
        private TCPObjectContainer m_tcpObjectContainer;

        public void Run(string bootConf, BootServices customBoot) {
            InitConfig(bootConf);
            Boot(customBoot);
            Loop();
        }

        private void InitConfig(string bootConf) {
            string bootConfigText = ConfigHelper.LoadFromFile(bootConf);
            m_bootConfig = JObject.Parse(bootConfigText);

            if (m_bootConfig.ContainsKey("Gateway")) {
                var gatewayJson = m_bootConfig["Gateway"];
                m_gateIp = gatewayJson["Host"].ToString();
                m_gatePort = Int32.Parse(gatewayJson["Port"].ToString());
            }

            if (m_bootConfig.ContainsKey("ThreadNum")) {
                int threadNum = (int)m_bootConfig["ThreadNum"];
                if (threadNum > 0)
                    m_workerNum = threadNum;
            }
        }

        private void Boot(BootServices customBoot) {
            m_globalMQ = GlobalMQ.Instance;
            m_serviceSlots = ServiceSlots.Instance;
            m_netpackQueue = NetworkPacketQueue.Instance;
            m_timer = SSTimer.Instance;
            NetProtocol.GetInstance();
            m_tcpObjectContainer = new TCPObjectContainer();

            if (m_bootConfig.ContainsKey("Gateway")) {
                InitGateway();
            }

            customBoot();
            LoggerHelper.Info(0, "Start SparkServer Server...");
            // 工作线程
            for (int i = 0; i < m_workerNum; i++) {
                Thread thread = new Thread(new ThreadStart(ThreadWorker));
                thread.Start();
            }

            // timer线程
            Thread timerThread = new Thread(new ThreadStart(ThreadTimer));
            timerThread.Start();
        }

        private void InitGateway() {
            string gatewayClass = m_bootConfig["Gateway"]["Class"].ToString();
            string gatewayName = m_bootConfig["Gateway"]["Name"].ToString();

            m_tcpGate = new TCPServer();
            m_tcpObjectContainer.Add(m_tcpGate);

            int gatewayId = SparkServerUtility.NewService(gatewayClass, gatewayName);
            m_tcpGate.Start(m_gateIp, m_gatePort, 30, gatewayId, OnSessionError, OnReadPacketComplete, OnAcceptComplete);
        }

        private void Loop() {
            while (true) {
                if (m_tcpGate != null) {
                    m_tcpGate.Loop();
                }

                ProcessOutbound();
                Thread.Sleep(1);
            }
        }

        private void ThreadWorker() {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            while (true) {
                int serviceId = m_globalMQ.Pop();
                if (serviceId == 0) {
                    autoResetEvent.WaitOne(1);
                } else {
                    ServiceContext service = m_serviceSlots.Get(serviceId);
                    Message msg = service.Pop();
                    if (msg != null) {
                        service.Callback(msg);
                        m_globalMQ.Push(service.GetId());
                    }
                }
            }
        }

        private void ThreadTimer() {
            while (true) {
                m_timer.Loop();
                Thread.Sleep(1);
            }
        }

        private void OnSessionError(int opaque, long sessionId, string remoteEndPoint, int errorCode, string errorText)
        {
            SocketError sprotoSocketError = new SocketError();
            sprotoSocketError.errorCode = errorCode;
            sprotoSocketError.errorText = errorText;
            sprotoSocketError.connection = sessionId;
            sprotoSocketError.remoteEndPoint = remoteEndPoint;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketError";
            msg.Data = sprotoSocketError.encode();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.Instance.Get(opaque);
            service.Push(msg);
        }

        private void OnReadPacketComplete(int opaque, long sessionId, byte[] buffer, int packetSize)
        {
            SocketData data = new SocketData();
            data.connection = sessionId;
            data.buffer = Convert.ToBase64String(buffer);

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketData";
            msg.Data = data.encode();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.Instance.Get(opaque);
            service.Push(msg);
        }

        private void OnAcceptComplete(int opaque, long sessionId, string ip, int port)
        {
            SocketAccept accept = new SocketAccept();
            accept.connection = sessionId;
            accept.ip = ip;
            accept.port = port;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketAccept";
            msg.Data = accept.encode();
            msg.RPCSession = 0;
            msg.Type = MessageType.Socket;

            ServiceContext service = ServiceSlots.Instance.Get(opaque);
            service.Push(msg);
        }

        private void ProcessOutbound() {
            while (true) {
                SocketMessage socketMessage = m_netpackQueue.Pop();
                if (socketMessage == null)
                    break;

                switch (socketMessage.Type) {
                    case SocketMessageType.Connect: {
                        ConnectMessage conn = socketMessage as ConnectMessage;
                        TCPClient tcpClient = (TCPClient)m_tcpObjectContainer.Get(conn.TcpObjectId);
                        tcpClient.Connect(conn.IP, conn.Port);
                    }
                        break;
                    case SocketMessageType.Disconnect: {
                        DisconnectMessage conn = socketMessage as DisconnectMessage;
                        TCPObject tcpObject = m_tcpObjectContainer.Get(conn.TcpObjectId);
                        tcpObject.Disconnect(conn.ConnectionId);
                    }
                        break;
                    case SocketMessageType.DATA: {
                        NetworkPacket netpack = socketMessage as NetworkPacket;
                        TCPObject tcpObject = m_tcpObjectContainer.Get(netpack.TcpObjectId);
                        Session session = tcpObject.GetSessionBy(netpack.ConnectionId);
                        if (session != null) {
                            for (int i = 0; i < netpack.Buffers.Count; i++) {
                                session.Write(netpack.Buffers[i]);
                            }
                        } else {
                            LoggerHelper.Info(0, string.Format("Opaque:{0} ConnectionId:{1} ErrorText:{2}", tcpObject.GetOpaque(), netpack.ConnectionId, "Connection disconnected"));
                        }
                    }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}