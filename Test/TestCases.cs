using System.Collections.Generic;
using TeddyServer.Framework;
using TeddyServer.Test.Gateway;

namespace TeddyServer.Test {
    delegate void StartupTestCase();
    public class TestCases {
        private Dictionary<string, StartupTestCase> m_testCaseDict = new Dictionary<string, StartupTestCase>();

        public TestCases() {
            RegisterTestCase("GatewayCase", GatewayCase);
            RegisterTestCase("GatewayClientCase", GatewayClientCase);

            // RegisterTestCase("RPCTestServer", RPCTestServer);
            // RegisterTestCase("RPCTestClient", RPCTestClient);
        }

        public void Run(string caseName) {
            StartupTestCase startup = null;
            bool isExist = m_testCaseDict.TryGetValue(caseName, out startup);
            if (isExist) {
                startup();
            }
        }

        private void RegisterTestCase(string caseName, StartupTestCase startup) {
            m_testCaseDict.Add(caseName, startup);
        }

        private void GatewayCase() {
            BootServices boot = delegate() { };
            Server server = new Server();
            server.Run("../../Test/Gateway/Resource/Config/Startup.json", boot);
        }

        private void GatewayClientCase() {
            var gatewayClient = new GatewayClientCase();
            gatewayClient.Run("../../Test/Gateway/Resource/Config/Startup.json");
        }
    }
}