using GangWarSandbox.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox
{
    static class Logger
    {
        private static GangWarSandbox ModData = GangWarSandbox.Instance; // Reference to the main mod instance

        public static void Log(String data, String logType = "LOG")
        {
            if (!File.Exists(ModFiles.LOG_FILE_PATH))
            {
                File.Create(ModFiles.LOG_FILE_PATH).Close(); // Create the file if it doesn't exist
            }

            File.AppendAllText(ModFiles.LOG_FILE_PATH, $"[{logType}] {data}\n");
        }

        public static void LogError(String data)
        {
            Log(data, "ERROR");
        }

        public static void LogDebug(String data)
        {
            if (ModData.DEBUG == 1)
                Log(data, "DEBUG");
        }

        public static void LogEvent(String data)
        {
            Log(data, "LOG");
        }

        public static void ParserError(String data)
        {
            Log(data, "PARSER");
        }   
    }
}
