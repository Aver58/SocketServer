using System;
using System.Text;
using System.Threading;
using NetSprotoType;
using Newtonsoft.Json.Linq;
using TeddyServer.Framework.MessageQueue;
using TeddyServer.Framework.Network;
using TeddyServer.Framework.Service;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Framework {
    /// <summary>
    /// 流程见：Picture/thread_model.png
    ///
    /// </summary>
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
        private string m_clusterServerIp;
        private int m_clusterServerPort = 0;
        private TCPServer m_clusterTCPServer;
        private TCPClient m_clusterTCPClient;

        public void Run(string bootConf, BootServices customBoot) {
            InitConfig(bootConf);
            Boot(customBoot);
            Loop();
        }

        private void InitConfig(string bootConf) {
            string bootConfigText = ConfigHelper.LoadFromFile(bootConf);
            m_bootConfig = JObject.Parse(bootConfigText);

            if (m_bootConfig.ContainsKey("ClusterConfig"))
            {
                string clusterNamePath = m_bootConfig["ClusterConfig"].ToString();
                string clusterNameText = ConfigHelper.LoadFromFile(clusterNamePath);
                JObject clusterConfig = JObject.Parse(clusterNameText);

                string clusterName = m_bootConfig["ClusterName"].ToString();
                string ipEndpoint = clusterConfig[clusterName].ToString();

                string[] ipResult = ipEndpoint.Split(':');
                m_clusterServerIp = ipResult[0];
                m_clusterServerPort = Int32.Parse(ipResult[1]);
            }

            if (m_bootConfig.TryGetValue("Gateway", out var gatewayJson)) {
                m_gateIp = gatewayJson["Host"].ToString();
                m_gatePort = Int32.Parse(gatewayJson["Port"].ToString());
            }

            if (m_bootConfig.TryGetValue("ThreadNum", out var value)) {
                int threadNum = (int)value;
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

            if (m_bootConfig.ContainsKey("ClusterConfig")) {
                InitCluster();
            }

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

        private void InitCluster() {
            m_clusterTCPServer = new TCPServer();
            m_tcpObjectContainer.Add(m_clusterTCPServer);

            m_clusterTCPClient = new TCPClient();
            m_tcpObjectContainer.Add(m_clusterTCPClient);

            ClusterServer_Init clusterServerInit = new ClusterServer_Init();
            clusterServerInit.tcp_server_id = m_clusterTCPServer.GetObjectId();
            int clusterServerId = SparkServerUtility.NewService("TeddyServer.Framework.Service.ClusterServer",
                "clusterServer",
                clusterServerInit.encode());

            ClusterClient_Init clusterClientInit = new ClusterClient_Init();
            clusterClientInit.tcp_client_id = m_clusterTCPClient.GetObjectId();
            clusterClientInit.cluster_config = m_bootConfig["ClusterConfig"].ToString();
            int clusterClientId = SparkServerUtility.NewService("TeddyServer.Framework.Service.ClusterClient",
                "clusterClient",
                clusterClientInit.encode());

            m_clusterTCPServer.Start(m_clusterServerIp, m_clusterServerPort, 30, clusterServerId, OnSessionError, OnReadPacketComplete, OnAcceptComplete);
            m_clusterTCPClient.Start(clusterClientId, OnSessionError, OnReadPacketComplete, OnConnectedComplete);
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
            bool isInitCluster = m_bootConfig.ContainsKey("ClusterConfig");
            bool isInitGateway = m_bootConfig.ContainsKey("Gateway");
            while (true) {
                if (isInitCluster) {
                    m_clusterTCPServer.Loop();
                    m_clusterTCPClient.Loop();
                }

                if (isInitGateway) {
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

        private void OnSessionError(int opaque, long sessionId, string remoteEndPoint, int errorCode, string errorText) {
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

        // 收到 client 包回调
        private void OnReadPacketComplete(int opaque, long sessionId, byte[] buffer, int packetSize) {
            SocketData data = new SocketData();
            data.connection = sessionId;
            data.buffer = Convert.ToBase64String(buffer);
            LoggerHelper.Info(0, $"OnReadPacketComplete :GetString {Encoding.ASCII.GetString(buffer, 0, packetSize)} ToBase64String {data.buffer}");

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

        // client 连接回调
        private void OnAcceptComplete(int opaque, long sessionId, string ip, int port) {
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

        private void OnConnectedComplete(int opaque, long sessionId, string ip, int port) {
            ClusterClientSocketConnected connected = new ClusterClientSocketConnected();
            connected.connection = sessionId;
            connected.ip = ip;
            connected.port = port;

            Message msg = new Message();
            msg.Source = 0;
            msg.Destination = opaque;
            msg.Method = "SocketConnected";
            msg.Data = connected.encode();
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
                        if (socketMessage is ConnectMessage conn) {
                            TCPClient tcpObject = (TCPClient)m_tcpObjectContainer.Get(conn.TcpObjectId);
                            if (tcpObject == null) {
                                LoggerHelper.Info(0, $"TcpObject is null, TcpObjectId:{conn.TcpObjectId}");
                                break;
                            }

                            tcpObject.Connect(conn.IP, conn.Port);
                        } else {
                            LoggerHelper.Info(0, $"socketMessage 转 ConnectMessage 失败！");
                        }
                    }
                        break;
                    case SocketMessageType.Disconnect: {
                        if (socketMessage is DisconnectMessage conn) {
                            TCPObject tcpObject = m_tcpObjectContainer.Get(conn.TcpObjectId);
                            if (tcpObject == null) {
                                LoggerHelper.Info(0, $"TcpObject is null, TcpObjectId:{conn.TcpObjectId}");
                                break;
                            }

                            tcpObject.Disconnect(conn.ConnectionId);
                        } else {
                            LoggerHelper.Info(0, $"socketMessage 转 DisconnectMessage 失败！");
                        }
                    }
                        break;
                    case SocketMessageType.DATA: {
                        if (socketMessage is NetworkPacket netpack) {
                            TCPObject tcpObject = m_tcpObjectContainer.Get(netpack.TcpObjectId);
                            if (tcpObject == null) {
                                LoggerHelper.Info(0, $"TcpObject is null, TcpObjectId:{netpack.TcpObjectId}");
                                break;
                            }

                            Session session = tcpObject.GetSessionBy(netpack.ConnectionId);
                            if (session != null) {
                                for (int i = 0; i < netpack.Buffers.Count; i++) {
                                    session.Write(netpack.Buffers[i]);
                                }
                            } else {
                                LoggerHelper.Info(0, $"Opaque:{tcpObject.GetOpaque()} ConnectionId:{netpack.ConnectionId} ErrorText:Connection disconnected ");
                            }
                        } else {
                            LoggerHelper.Info(0, $"socketMessage 转 NetworkPacket 失败！");
                        }
                    }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public delegate void BootServices();
}