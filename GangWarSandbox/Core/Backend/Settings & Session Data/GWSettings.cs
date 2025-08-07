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
        public static readonly bool DEBUG = false;
        public static Keys OpenMenuKeybind = Keys.F10;

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
