using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Core
{
    static class ModFiles
    {
        public static string ModPath = "scripts/GangWarSandbox";
        public static string ConfigPath = "scripts/GangWarSandbox/Configuration.ini";
        public static string VehicleSetPath = ModPath + "/VehicleSets";
        public static string FactionsPath = ModPath + "/Factions";
        public const String LOG_FILE_PATH = "scripts/GangWarSandbox/GWS.log"; // Path to the log file

        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(ModPath);
            Directory.CreateDirectory(VehicleSetPath);
            Directory.CreateDirectory(FactionsPath);

            File.Create(LOG_FILE_PATH).Close(); // Ensure the log file exists

            // Ensure the log file is empty at mod start
            if (File.Exists(LOG_FILE_PATH))
            {
                File.WriteAllText(LOG_FILE_PATH, string.Empty); // Clear the log file
            }
        }

    }
}
