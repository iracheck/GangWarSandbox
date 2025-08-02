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
        public bool IsSpawnPositionCrowded(Vector3 pos, float minDistance = 5f)
        {
            var nearbyPeds = World.GetAllPeds().Where(p => p.Exists() && p.Position.DistanceTo(pos) < minDistance);

            if (nearbyPeds.Count() > 10)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the proper road direction for the heading and considering the target position of the vehicle. TRUTHFULLY? I understand the math but found it online.
        /// So if anyone is reading this and doesn't understand it-- sorry!
        /// </summary>
        /// <param name="position"></param>
        /// <param name="targetPos"></param>
        /// <param name="roadPosition"></param>
        /// <returns></returns>
        public static float GetRoadDirectionByHeading(Vector3 position, Vector3 targetPos, out Vector3 roadPosition)
        {
            float heading = 0f;
            OutputArgument outPos = new OutputArgument();
            OutputArgument outHeading = new OutputArgument();

            Function.Call(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,
                position.X, position.Y, position.Z,
                outPos, outHeading, 1, 3, 0);

            roadPosition = outPos.GetResult<Vector3>();
            heading = outHeading.GetResult<float>();

            Vector3 direction = targetPos - roadPosition;
            direction.Z = 0;
            direction.Normalize();

            float headingRad = heading * ((float)Math.PI / 180f);

            // Swap sin/cos if needed based on your vehicle orientation
            Vector3 roadHeadingDir = new Vector3((float)Math.Sin(headingRad), (float)Math.Cos(headingRad), 0f);

            float angle = (float)(Math.Atan2(roadHeadingDir.Y, roadHeadingDir.X) - Math.Atan2(direction.Y, direction.X));
            if (angle < 0) angle += 2 * (float)Math.PI;

            if (angle > Math.PI) // if more than 180 degrees difference, flip heading
            {
                heading = (heading + 180f) % 360f;
            }

            return heading;
        }


    }

}
