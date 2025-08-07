using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Utilities
{
    static class NotificationHandler
    {
        public static void Send(string message)
        {
            string prefixedMessage = "~y~[GangWarSandbox " + GWS_Metadata.Version + "] ~w~" + message;
            GTA.UI.Notification.Show(prefixedMessage, false);
        }
    }
}
