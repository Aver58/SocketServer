using NetSprotoType;
using TeddyServer.Framework.Service.Base;
using TeddyServer.Framework.Utility;

namespace SparkServer.Game.Process.Battle
{
    class BattleTaskConsumer : ServiceContext
    {
        protected override void Init()
        {
            base.Init();

            RegisterServiceMethods("OnBattleRequest", OnBattleRequest);
        }

        private void OnBattleRequest(int source, int session, string method, byte[] param)
        {
            BattleTaskConsumer_OnBattleRequest request = new BattleTaskConsumer_OnBattleRequest(param);

            // TODO Logic
            LoggerHelper.Info(m_serviceAddress, request.param);

            BattleTaskConsumer_OnBattleRequestResponse response = new BattleTaskConsumer_OnBattleRequestResponse();
            response.method = "OnBattleRequest";
            response.param = request.param;
            DoResponse(source, response.method, response.encode(), session);
        }
    }
}
