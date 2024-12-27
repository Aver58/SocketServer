using System;
using System.Collections.Generic;
using System.Text;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace TeddyServer.Framework.Service {
    class Gateway : ServiceContext {
        private Dictionary<string, Method> m_socketMethods = new Dictionary<string, Method>();

        protected override void Init() {
            base.Init();

            RegisterSocketMethods("SocketAccept", SocketAccept);
            RegisterSocketMethods("SocketData", SocketData);
            RegisterSocketMethods("SocketError", SocketError);
        }

        protected override void OnSocketCommand(Message msg) {
            Method method = null;
            bool isExist = m_socketMethods.TryGetValue(msg.Method, out method);
            if (isExist) {
                method(msg.Source, msg.RPCSession, msg.Method, msg.Data);
            } else {
                LoggerHelper.Info(m_serviceAddress, $"unknow method {msg.Method}");
            }
        }

        protected virtual void SocketAccept(int source, int session, string method, byte[] param) {
        }

        protected virtual void SocketError(int source, int session, string method, byte[] param) {
        }

        protected virtual void SocketData(int source, int session, string method, byte[] param) {
            NetSprotoType.SocketData data = new NetSprotoType.SocketData(param);
            byte[] tempParam = Convert.FromBase64String(data.buffer);
            LoggerHelper.Info(source, $"SocketData session:{session},method:{method},param:{Encoding.ASCII.GetString(param, 0, param.Length)} connection {data.connection}");
            // 解析协议

            // 推送到逻辑服务器

            // 怎么知道自己是不是在线程里处理
        }

        private void RegisterSocketMethods(string methodName, Method method) {
            m_socketMethods.Add(methodName, method);
        }
    }
}