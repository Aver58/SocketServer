using System.IO;

namespace TeddyServer.Framework.Utility {
    public class ConfigHelper {
        public static string LoadFromFile(string path) {
            if (File.Exists(path)) {
                return File.ReadAllText(path);
            } else {
                return "";
            }
        }
    }
}