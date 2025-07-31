using GTA.Math;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GangWarSandbox.Peds;
using GangWarSandbox;
using System.ComponentModel;
using System.Runtime.Serialization;
using GTA.Native;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using LemonUI;

namespace GangWarSandbox.Peds
{
    // This class is a list of helper methods used by squads for a variety of purposes. Such as navigation, decision making, etc.

    public partial class Squad
    {
        public void EnsureCorrectRotationTowardTarget()
        {
            Vector3 dirToTarget = Game.Player.Character.Position - SquadVehicle.Position;
            float headingToTarget = (float)(Math.Atan2(dirToTarget.Y, dirToTarget.X) * (180.0 / Math.PI));

            SquadVehicle.Heading = headingToTarget;
        }
    }

}
