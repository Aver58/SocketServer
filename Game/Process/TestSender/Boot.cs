using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace SparkServer.Game.Process.TestSender {
    class Boot : ServiceContext {
        protected override void Init() {
            base.Init();
            for (int i = 0; i < 10; i++) {
                SparkServerUtility.NewService("SparkServer.Game.Process.TestSender.Sender", "Sender");
            }
        }
    }
}