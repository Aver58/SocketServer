using System.Threading;
using Server.Framework.Network;

namespace Server.Framework {
    public delegate void BootServices();
    public class Server {
        private TCPServer m_tcpGate;
        private int m_workerNum = 8;

        public void Run(string bootConf, BootServices customBoot) {
            InitConfig(bootConf);
            Boot(customBoot);
            Loop();
        }

        private void InitConfig(string bootConf) {
        }


        private void Boot(BootServices customBoot) {
            // m_tcpGate = new TCPServer();
            // m_tcpGate.Start(m_gateIp, m_gatePort, 30, gatewayId, OnSessionError, OnReadPacketComplete, OnAcceptComplete);

            // 工作线程
            // for (int i = 0; i < m_workerNum; i++) {
            //     Thread thread = new Thread(new ThreadStart(ThreadWorker));
            //     thread.Start();
            // }

            // timer线程
            // Thread timerThread = new Thread(new ThreadStart(ThreadTimer));
            // timerThread.Start();
        }

        private void Loop() {
            m_tcpGate.Loop();
        }

    }
}