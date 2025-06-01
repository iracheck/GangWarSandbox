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
using GTA;
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

        public static void GoToFarAway(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 2.0f, -1, 5.0f, 0, 0.0f);
        }

        public static void PushLocation(Ped ped, Vector3 location)
        {
            Function.Call(Hash.TASK_GO_TO_COORD_AND_AIM_AT_HATED_ENTITIES_NEAR_COORD, ped, location.X, location.Y, location.Z, location.X, location.Y, location.Z, 2.0f,                                               // walk speed (1.0 = walk, 2.0 = jog, 3.0 = run)
            true,                                               // shootAtEnemies
            5.0f,                                               // distance to stop at
            0f,                                                 // noRoadsDistance (set to 0 unless needed)
            true,                                               // useNavMesh
            0,                                                  // navFlags (0 for default)
            16,                                                  // taskFlags (0 unless overriding behavior)
            Game.GenerateHash("FIRING_PATTERN_FULL_AUTO")       // firing pattern
);
        }

        public static void AttackEnemy(Ped ped, Ped enemy)
        {
            Function.Call(Hash.TASK_COMBAT_PED, ped, enemy, 0, 16);
        }

        public static void AttackNearbyEnemies(Ped ped, float attackRange)
        {
            Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped, attackRange, 0);
        }

        public static void SeekCover(Ped ped, int timeInMS = 15000)
        {
            Vector3 position = ped.Position;

            Function.Call(Hash.TASK_SEEK_COVER_FROM_POS, ped.Handle, position.X, position.Y, position.Z, 15000, false);
        }

        public static bool HasLineOfSight(Ped source, Ped target)
        {
            // Use bone positions instead of just position + offset
            Vector3 sourcePos = source.Bones[Bone.SkelHead].Position;
            Vector3 targetPos = target.Bones[Bone.SkelHead].Position;

            RaycastResult result = World.Raycast(
                sourcePos,
                targetPos,
                IntersectFlags.Everything,
                source // ignore the shooter
            );

            // Ensure that the thing we hit is actually the intended target
            return result.DidHit && result.HitEntity != null && result.HitEntity == target;
        }

        public static List<Vector3> GetIntermediateWaypoints(Vector3 start, Vector3 end, float maxStepSize = 100f)
        {
            List<Vector3> points = new List<Vector3>();

            Vector3 direction = end - start;
            direction.Normalize(); // convert it to a normal vector, so we know which direction to count in

            float distance = direction.Length();

            int numSteps = (int)(distance / maxStepSize);

            if (distance > maxStepSize && numSteps > 0) {


                for (int i = 0; i < numSteps; i++)
                {
                    Vector3 step = start + direction * (i * maxStepSize);
                    points.Add(step);
                }
            }

            points.Add(end);
            return points;
        }

    }
}
