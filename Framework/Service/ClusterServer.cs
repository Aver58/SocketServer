using System;
using System.Collections.Generic;
using System.Text;
using NetSprotoType;
using TeddyServer.Framework.MessageQueue;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Framework.Service {
    class ClusterServer : ServiceContext {
        private int m_tcpObjectId = 0;
        private SkynetPacketManager m_skynetPacketManager = new SkynetPacketManager();
        Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();

        protected override void Init(byte[] param) {
            base.Init();

            ClusterServer_Init init = new ClusterServer_Init(param);
            SetTCPObjectId((int)init.tcp_server_id);

            RegisterSocketMethods("SocketAccept", SocketAccept);
            RegisterSocketMethods("SocketError", SocketError);
            RegisterSocketMethods("SocketData", SocketData);
        }

        private void SetTCPObjectId(int tcpObjectId) {
            m_tcpObjectId = tcpObjectId;
        }

        protected override void OnSocketCommand(Message msg) {
            base.OnSocketCommand(msg);

            Method method = null;
            bool isExist = m_socketMethods.TryGetValue(msg.Method, out method);
            if (isExist) {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            } else {
                LoggerHelper.Info(m_serviceAddress, $"Unknow socket command {msg.Method}");
            }
        }

        private void SocketAccept(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketAccept accept = new NetSprotoType.SocketAccept(param);
            LoggerHelper.Info(m_serviceAddress, $"ClusterServer accept new connection ip = {accept.ip}, port = {accept.port}, connection = {accept.connection}");
        }

        private void SocketError(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketError error = new NetSprotoType.SocketError(param);
            LoggerHelper.Info(m_serviceAddress, $"ClusterServer socket error connection:{error.connection} errorCode:{error.errorCode} errorText:{error.errorText}");
        }

        private void SocketData(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketData socketData = new NetSprotoType.SocketData(param);
            long connectionId = socketData.connection;
            byte[] tempParam = Convert.FromBase64String(socketData.buffer);

            SkynetClusterRequest req = m_skynetPacketManager.UnpackSkynetRequest(tempParam);
            if (req == null) {
                return;
            }

            NetProtocol instance = NetProtocol.GetInstance();
            int tag = instance.GetTag("RPC");
            RPCParam sprotoRequest = (RPCParam)instance.Protocol.GenRequest(tag, req.Data);
            byte[] targetParam = Convert.FromBase64String(sprotoRequest.param);

            if (req.Session > 0) {
                SSContext context = new SSContext();
                context.IntegerDict["RemoteSession"] = req.Session;
                context.LongDict["ConnectionId"] = connectionId;

                Call(req.ServiceName, sprotoRequest.method, targetParam, context, TransferCallback);
            } else {
                Send(req.ServiceName, sprotoRequest.method, targetParam);
            }
        }

        private void TransferCallback(SSContext context, string method, byte[] param, RPCError error) {
            if (error == RPCError.OK) {
                int tag = NetProtocol.GetInstance().GetTag("RPC");
                RPCParam rpcParam = new RPCParam();
                rpcParam.method = method;
                rpcParam.param = Convert.ToBase64String(param);

                int remoteSession = context.IntegerDict["RemoteSession"];
                long connectionId = context.LongDict["ConnectionId"];

                List<byte[]> bufferList = m_skynetPacketManager.PackSkynetResponse(remoteSession, tag, rpcParam.encode());

                NetworkPacket rpcMessage = new NetworkPacket();
                rpcMessage.Type = SocketMessageType.DATA;
                rpcMessage.TcpObjectId = m_tcpObjectId;
                rpcMessage.Buffers = bufferList;
                rpcMessage.ConnectionId = connectionId;

                NetworkPacketQueue.Instance.Push(rpcMessage);
            } else {
                int remoteSession = context.IntegerDict["RemoteSession"];
                long connectionId = context.LongDict["ConnectionId"];

                List<byte[]> bufferList = m_skynetPacketManager.PackErrorResponse(remoteSession, Encoding.ASCII.GetString(param));

                NetworkPacket rpcMessage = new NetworkPacket();
                rpcMessage.Type = SocketMessageType.DATA;
                rpcMessage.TcpObjectId = m_tcpObjectId;
                rpcMessage.Buffers = bufferList;
                rpcMessage.ConnectionId = connectionId;

                NetworkPacketQueue.Instance.Push(rpcMessage);

                LoggerHelper.Info(m_serviceAddress, $"Service:ClusterServer Method:TransferCallback errorCode:{(int)error} errorText:{Encoding.ASCII.GetString(param)}");
            }
        }

        private void RegisterSocketMethods(string methodName, Method method) {
            m_socketMethods.Add(methodName, method);
        }
    }
}