using GangWarSandbox.Core;
using GangWarSandbox.Utilities;
using GTA;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GangWarSandbox.Core
{
    static partial class ConfigParser
    {
        public static void LoadConfiguration()
        {
            Logger.Parser("Loading mod config file...");
            try
            {
                string path = ModFiles.ConfigPath;

                var lines = File.ReadLines(path);
                if (lines == null || lines.Count() == 0)
                {
                    NotificationHandler.Send("~r~Warning:~w~ The Configuration file could not be found, or does not exist. Using default values.");
                }

                foreach (var line in lines)
                {
                    
                }
            }
            catch (Exception e)
            {
                Logger.ParserError("Failed to parse mod configuration file. Error: " + e.ToString());
            }
        }

    }
}
