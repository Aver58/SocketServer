using System.Collections.Generic;
using Google.Protobuf;

namespace TeddyServer.Shared {
    // 管理 proto 回调的注册和调用
    public partial class ProtoManager {
        private Dictionary<int, IHandler> listenerMap = new Dictionary<int, IHandler>();

        public void AddListener(int id, IHandler callback) {
            listenerMap.Add(id, callback);
        }

        public void RemoveListener(int id) {
            if (listenerMap.ContainsKey(id)) {
                listenerMap.Remove(id);
            }
        }

        public void Dispatch(int id, IMessage message) {
            if (listenerMap.ContainsKey(id)) {
                listenerMap[id].Handle(message);
            }
        }

        public void HandleMsg(byte[] buffer) {
            // 解析数据

            // 从数据中获取 id
            // 获取 message
            // 调用 Dispatch(id, message)
        }
    }

    public interface IHandler {
        void Handle(IMessage message);
    }
}