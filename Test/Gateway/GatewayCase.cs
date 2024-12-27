using System;
using System.Collections.Generic;
using System.Text;
using TeddyServer.Framework.MessageQueue;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Test.Gateway {
    // TestCases 反射调用
    class GatewayCase : Framework.Service.Gateway {
        protected override void Init() {
            base.Init();
        }

        protected override void SocketAccept(int source, int session, string method, byte[] param) {
            LoggerHelper.Info(m_serviceAddress, $"SocketAccept session:{session},method:{method},param:{Encoding.ASCII.GetString(param, 0, param.Length)}");
        }

        protected override void SocketError(int source, int session, string method, byte[] param) {
            LoggerHelper.Info(m_serviceAddress, $"SocketError session:{session},method:{method},param:{Encoding.ASCII.GetString(param, 0, param.Length)}");
        }

        // 收到客户端消息
        protected override void SocketData(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketData data = new NetSprotoType.SocketData(param);

            LoggerHelper.Info(m_serviceAddress, $"SocketData session:{session},method:{method},param:{Encoding.ASCII.GetString(param, 0, param.Length)} connection {data.connection}");

            NetworkPacket message = new NetworkPacket();
            message.Type = SocketMessageType.DATA;
            message.TcpObjectId = GetId();
            message.ConnectionId = data.connection;

            List<byte[]> buffList = new List<byte[]>();
            buffList.Add(Convert.FromBase64String(data.buffer));
            message.Buffers = buffList;

            NetworkPacketQueue.Instance.Push(message);
        }
    }
}