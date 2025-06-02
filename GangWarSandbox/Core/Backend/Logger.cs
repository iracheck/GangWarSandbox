using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Core.Backend
{
    static class Logger
    {
        private const String LOG_FILE_PATH = "scripts/GangWarSandbox.log"; // Path to the log file

        public static void Log(String data, String logType = "DEBUG")
        {
            if (!File.Exists(LOG_FILE_PATH))
            {
                File.Create(LOG_FILE_PATH).Close(); // Create the file if it doesn't exist
            }

            File.AppendAllText(LOG_FILE_PATH, $"[{logType}] {data}\n");
        }

        public static void LogError(String data)
        {
            Log(data, "ERROR");
        }

        public static void LogDebug(String data)
        {
            Log(data, "DEBUG");
        }

        public static void LogEvent(String data)
        {
            Log(data, "EVENT");
        }
    }
}
