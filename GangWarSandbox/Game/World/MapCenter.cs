using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox
{
    static class MapCenter
    {
        static Vector3 Location;

        static float KillRadius;
        static float Radius;

        static bool IsVisible;

        static void OnBattleStart()
        {
            
            SetFocalPoint();

        }

        static void OnBattleEnd()
        {
            Function.Call(Hash.SET_FOCUS_ENTITY, Game.Player.Character.Handle);
        }


        static void SetFocalPoint()
        {
            Function.Call(Hash.SET_FOCUS_POS_AND_VEL, Location.X, Location.Y, Location.Z, 0, 0, 0);
        }
    }
}
