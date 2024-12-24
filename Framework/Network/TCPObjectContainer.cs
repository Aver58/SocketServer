using System.Collections.Generic;

namespace TeddyServer.Framework.Network {
    // This class is not thread safe
    public class TCPObjectContainer {
        private Dictionary<int, TCPObject> m_tcpObjectDict = new Dictionary<int, TCPObject>();
        private int m_totalObjectId = 0;

        public int Add(TCPObject tcpObject) {
            int id = ++m_totalObjectId;
            tcpObject.SetObjectId(id);
            m_tcpObjectDict.Add(id, tcpObject);

            return id;
        }

        public TCPObject Get(int tcpObjectId) {
            TCPObject tcpObject = null;
            m_tcpObjectDict.TryGetValue(tcpObjectId, out tcpObject);
            return tcpObject;
        }

        public void Remove(int tcpObjectId) {
            m_tcpObjectDict.Remove(tcpObjectId);
        }
    }
}