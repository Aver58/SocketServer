using System;
using System.Collections.Generic;
using TeddyServer.Framework.MessageQueue;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Test.Gateway {
    // TestCases 反射调用
    class GatewayCase : Framework.Service.Gateway {
        protected override void SocketAccept(int source, int session, string method, byte[] param) {
            LoggerHelper.Info(m_serviceAddress, $"SocketAccept session:{session},method:{method},param:{param}");
        }

        protected override void SocketError(int source, int session, string method, byte[] param) {
            LoggerHelper.Info(m_serviceAddress, $"SocketError session:{session},method:{method},param:{param}");
        }

        protected override void SocketData(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketData data = new NetSprotoType.SocketData(param);

            LoggerHelper.Info(m_serviceAddress, $"SocketData session:{session},method:{method},param:{param} {data.connection},{data.buffer}");

            NetworkPacket message = new NetworkPacket();
            message.Type = SocketMessageType.DATA;
            message.TcpObjectId = this.GetTcpObjectId();
            message.ConnectionId = data.connection;

            List<byte[]> buffList = new List<byte[]>();
            buffList.Add(Convert.FromBase64String(data.buffer));
            message.Buffers = buffList;

            NetworkPacketQueue.Instance.Push(message);
        }
    }
}