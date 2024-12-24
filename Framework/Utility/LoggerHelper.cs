using System;

namespace TeddyServer.Framework.Utility {
    public class LoggerHelper {
        public static void Info(int source, string msg) {
            // string logger = "logger";
            // LoggerService loggerService = (LoggerService)ServiceSlots.GetInstance().Get(logger);
            //
            // Message message = new Message();
            // message.Method = "OnLog";
            // message.Data = Encoding.ASCII.GetBytes(msg);
            // message.Destination = loggerService.GetId();
            // message.Source = source;
            // message.Type = MessageType.ServiceRequest;
            // loggerService.Push(message);
            Console.WriteLine($"source {source} msg {msg}");
        }
    }
}