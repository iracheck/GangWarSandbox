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
        static List<WeaponHash> Pistols = new List<WeaponHash> { WeaponHash.APPistol, WeaponHash.CombatPistol, WeaponHash.DoubleActionRevolver, 
            WeaponHash.FlareGun, WeaponHash.HeavyPistol, WeaponHash.RevolverMk2, WeaponHash.Revolver, WeaponHash.Pistol50, WeaponHash.PistolMk2, 
            WeaponHash.Pistol, WeaponHash.SNSPistol, WeaponHash.SNSPistolMk2, WeaponHash.UpNAtomizer, WeaponHash.VintagePistol, WeaponHash.StunGun};
        static List<WeaponHash> SMGs = new List<WeaponHash> { WeaponHash.AssaultSMG, WeaponHash.CombatPDW, WeaponHash.MicroSMG, WeaponHash.MiniSMG, WeaponHash.SMGMk2, WeaponHash.SMG };

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
            Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped, 90f, 0);
        }

        public static void SeekCover(Ped ped, int timeInMS = 15000)
        {
            Vector3 position = ped.Position + ped.ForwardVector * 5f; // Get a position in front of the ped

            Function.Call(Hash.TASK_SEEK_COVER_FROM_POS, ped.Handle, position.X, position.Y, position.Z, 15000, false);
        }

        public static bool HasLineOfSight(Ped ped, Ped nearbyEnemy)
        {
            return Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, ped.Handle, nearbyEnemy.Handle, 17);
        }

        public static float GetIdealWeaponsDistance(Ped ped)
        {
            float idealDistance = 80f;

            if (ped.Weapons.IsWeaponValid(ped.Weapons.Current.Hash))
            {
                WeaponHash weaponHash = ped.Weapons.Current.Hash;

                if (Pistols.Contains(weaponHash) || SMGs.Contains(weaponHash))
                {
                    idealDistance = 50f;
                }
            }

            return idealDistance;
        }

    }
}
