using GTA;
using GTA.Native;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LemonUI;
using LemonUI.Menus;
using GangWarSandbox;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Runtime.Serialization;
using static GangWarSandbox.Squad;

namespace GangWarSandbox
{
    public class PedAI
    {

        // re-added a couple functions from SHVDN-- these have shown to be more reliable than those (for some reason)

        public static void RunTo(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 2.0f, -1, 0.0f, false, 0.0f);
        }

        public static void WalkTo(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 1.0f, -1, 0.0f, false, 0.0f);
        }

        public static void JogTo(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 1.5f, -1, 0.0f, false, 0.0f);
        }


        public static void AttackEnemy(Ped ped, Ped enemy)
        {
            Function.Call(Hash.TASK_COMBAT_PED, ped, enemy, 0, 16);
        }

        public static void AttackNearbyEnemies(Ped ped)
        {
            Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped, 200f, 0);
        }

        public static void SeekCover(Ped ped, int timeInMS = 15000)
        {
            Vector3 position = ped.Position + ped.ForwardVector * 5f; // Get a position in front of the ped

            Function.Call(Hash.TASK_SEEK_COVER_FROM_POS, ped.Handle, position.X, position.Y, position.Z, 15000, false);
        }

        public static void ThrowGrenade(Ped ped, Ped targetPed)
        {
            WeaponHash savedWeapon = ped.Weapons.Current.Hash;
            Vector3 targetPos = targetPed.Position + targetPed.ForwardVector * 2f; // Get a position in front of the target

            ped.Weapons.Give(WeaponHash.Grenade, 1, true, true);
            Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped.Handle, (uint)WeaponHash.Grenade, true);
            Script.Wait(300);

            Function.Call(Hash.TASK_LOOK_AT_COORD, ped.Handle, targetPos.X, targetPos.Y, targetPos.Z, 1000, 0, 2, 0);
            Script.Wait(300);

            Function.Call(Hash.TASK_THROW_PROJECTILE, ped.Handle, targetPos.X, targetPos.Y, targetPos.Z, 0, 0, 0, 0, 0, 0, 0);
            Script.Wait(300);

            // give old weapon back
            Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped.Handle, (uint)savedWeapon, true);

        }

        public static bool HasLineOfSight(Ped ped, Ped nearbyEnemy)
        {
            return Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, ped.Handle, nearbyEnemy.Handle, 17);
        }
    }
}
