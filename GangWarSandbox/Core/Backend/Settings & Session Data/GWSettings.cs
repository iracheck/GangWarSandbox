using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GangWarSandbox
{
    static class GWSettings
    {
        public static bool DEBUG = false;
        public static Keys OpenMenuKeybind = Keys.F10;

        // Performance values
        public static int AI_UPDATE_FREQUENCY = 150; // How often squad AI will be updated, in milliseconds
        public static int VEHICLE_AI_UPDATE_FREQUENCY = 75; // How often squad AI will be updated, in milliseconds

        public static int MAX_CORPSES = 25; // Maximum number of corpses to keep rendered
        public static int MAX_SQUADLESS_VEHICLES = 10; // Maximum number of squadless vehicles to have in the world

        public static bool SetOpenMenuKeybind(string keybind)
        {
            Keys parsedKey;
            if (Enum.TryParse(keybind, out parsedKey))
            {
                OpenMenuKeybind = parsedKey;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
