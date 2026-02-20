using GangWarSandbox.Utilities;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        Vector3 Pos1Second = Vector3.Zero;
        Vector3 Pos2Seconds = Vector3.Zero;

        bool IsStuck()
        {
            // 10 cycles is "roughly" 2 seconds, assuming the squad updates 5 times per second.
            if (!SquadLeader.IsInVehicle() || CyclesAlive < 10) return false;

            float distanceToTarget = SquadLeader.CurrentVehicle.Position.DistanceTo(Waypoints.Last());

            if (distanceToTarget <= 30f || SquadLeader.IsInCombat) return false;

            if (Pos1Second.DistanceTo(Pos2Seconds) >= 2f) return false;

            if (Pos2Seconds.DistanceTo(SquadLeader.CurrentVehicle.Position) >= 2f)
            {
                return false;
            }
            else return true;
        }

        void UpdateStuckDetection()
        {
            // not in vehicle? then we dont care
            if (!SquadLeader.IsInVehicle()) return;

            // Update the positions over the last two seconds
            Pos1Second = Pos2Seconds;
            Pos2Seconds = SquadLeader.CurrentVehicle.Position;

            if (IsStuck())
            {
                Logger.LogDebug(">> STUCK? << Squad is potentially stuck at location: " + SquadVehicle.Position);

                // Reset positions, so that Stuck Detection does not constantly try to "save" the given squad
                Pos1Second = Vector3.Zero;
                Pos2Seconds = Vector3.Zero;
            }
        }
    }
}
