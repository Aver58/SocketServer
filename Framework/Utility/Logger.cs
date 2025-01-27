using System;

namespace TeddyServer.Framework.Utility {
    public class Logger {
        public static void Log(string message) {
            Console.WriteLine("[Log]" + message);
        }

        public static void LogError(string message) {
            Console.WriteLine("[Error]" + message);
        }

        public static void LogWarning(string message) {
            Console.WriteLine("[Warning]" + message);
        }
    }
}