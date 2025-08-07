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
using GangWarSandbox.Core;
using GangWarSandbox.Utilities;

namespace GangWarSandbox.Peds
{
    public class PedAI
    {
        // Helper classes
        static GangWarSandbox ModData = GangWarSandbox.Instance;
        static Random rand = new Random();


        public static void GoToFarAway(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 1.5f, -1, 5.0f, 8, 0.0f);
        }

        public static void RunToFarAway(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 2f, -1, 5.0f, 8, 0.0f);
        }

        public static void DefendArea(Ped ped, Vector3 point)
        {
            // Set defensive area for fallback logic
            Function.Call(Hash.SET_PED_SPHERE_DEFENSIVE_AREA, ped.Handle, point.X, point.Y, point.Z, 20f, false, false);

            // Give guard order
            Function.Call(Hash.TASK_GUARD_SPHERE_DEFENSIVE_AREA,
                ped.Handle,
                point.X, point.Y, point.Z,
                0f,        // heading
                10f,       // patrol radius
                -1,        // infinite guard time
                point.X, point.Y, point.Z,
                20f        // outer guard radius
            );
        }

        public static void PushLocation(Ped ped, Vector3 targetPos, Vector3 enemyPos)
        {
            Function.Call(Hash.TASK_GO_TO_COORD_WHILE_AIMING_AT_COORD,
                ped.Handle,
                targetPos.X, targetPos.Y, targetPos.Z,
                enemyPos.X, enemyPos.Y, enemyPos.Z,
                2.0f,       // move speed
                true,       // shoot if sees enemy
                0.5f,       // unknown, usually 0-1
                0.5f,       // unknown, usually 0-1
                true,       // walk
                0,          // firing pattern, 0 = default
                false,      // unknown
                0           // flags?
            );
        }

        public static bool IsNavmeshLoaded(Vector3 pos, float offset = 1f)
        {
            bool navmeshLoaded = Function.Call<bool>(Hash.IS_NAVMESH_LOADED_IN_AREA,
                pos.X - offset, pos.Y - offset, pos.Z - 1f,
                pos.X + offset, pos.Y + offset, pos.Z + 1f
            );

            return navmeshLoaded;
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

        public static void EnterVehicle(Ped ped, Vehicle target)
        {
            ped.Task.EnterVehicle(target);
        }

        public static void DriveTo(Ped ped, Vehicle vehicle, Vector3 target)
        {
            ped.Task.DriveTo(vehicle, target, 20f, 40f);
        }

        public static void DriveBy(Ped ped, Ped target)
        {
            Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, ped.Handle, target.Handle, 10000, 180f);
        }


        public static bool HasLineOfSight(Ped source, Ped target)
        {
            // Use bone positions instead of just position + offset
            Vector3 sourcePos = source.Bones[Bone.SkelHead].Position + new Vector3(0,0,1);
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

        // This method makes a progressievly larger circle around the coordinate until it finds a safe position.
        // This method, while functional, relatively frequently results in strange behavior, so it's not going to be used for now.
        public static Vector3 FindSafePositionNearCoord(Vector3 coord)
        {
            List<float> distances = new List<float> { 2f, 5f, 10f, 15f, 20f, 25f };

            foreach (var dist in distances)
            {
                List<Vector3> offsets = new List<Vector3>
                {
                    new Vector3(dist, 0f, 0f),
                    new Vector3(-dist, 0f, 0f),
                    new Vector3(0f, dist, 0f),
                    new Vector3(0f, -dist, 0f),
                };

                foreach (var offset in offsets)
                {
                    Vector3 testPos = coord + offset;
                    Logger.LogDebug("Squad tried register this waypoint: " + testPos);

                    testPos.Z = World.GetGroundHeight(testPos + (new Vector3(0,0,2)));

                    if (testPos != Vector3.Zero && testPos.Z != 0)
                    {
                        Logger.LogDebug("A valid coordinate was found " + testPos);
                        return testPos;
                    }
                }

            }

            return Vector3.Zero;
        }



        // The following are all helper methods to help peds reach locations, or find destinations. They do not directly control peds.

        public static List<Vector3> GetIntermediateWaypoints(Vector3 start, Vector3 end, bool hasVehicle = false)
        {
            List<Vector3> points = new List<Vector3>();

            float maxStepSize = 50f;

            if (hasVehicle) maxStepSize = 80f;

            // Convert END to a temporary road-based destination
            Vector3 endRoad = World.GetNextPositionOnStreet(end);

            // If the end road is a valid target, use that instead, to help with routing around buildings.
            Vector3 navTarget = endRoad != Vector3.Zero ? endRoad : end;

            Vector3 direction = navTarget - start;
            float distance = direction.Length();
            direction.Normalize();

            int numSteps = (int)(distance / maxStepSize);

            for (int i = 1; i < numSteps; i++)
            {
                Vector3 step = start + direction * (i * maxStepSize);
                Vector3 safeStep = World.GetSafeCoordForPed(step, true);

                // Fallback 2: Use nearest road
                if (safeStep == Vector3.Zero || hasVehicle)
                {
                    safeStep = World.GetNextPositionOnStreet(step);
                }

                if (safeStep != Vector3.Zero)
                {
                    points.Add(safeStep);
                }
                else
                {
                    Logger.LogDebug($"[WAYPOINT ERROR] No valid waypoint at step {i}: {step}");
                    GTA.UI.Screen.ShowSubtitle($"Pathfinding failed at step {i}");
                }
            }

            // If we routed to the road near the end, add final leg to the real endpoint
            if (endRoad != Vector3.Zero)
            {
                points.Add(end);
            }

            return points;
        }

        public static Vector3 FindRandomEnemySpawnpoint(Team team)
        {
            List<Team> temp = ModData.Teams.Where(t => t != team && t.SpawnPoints.Count > 0).ToList();

            if (temp.Count == 0) return Vector3.Zero; // no enemy teams with spawnpoints

            int randomIndex = rand.Next(temp.Count);

            return temp[randomIndex].SpawnPoints[rand.Next(0, temp[randomIndex].SpawnPoints.Count)]; // get a random spawnpoint from the enemy team
        }

        public static Vector3 FindRandomCapturePoint(Team team, bool hostileOnly = false)
        {
            List<CapturePoint> temp = ModData.CapturePoints;


            if (temp.Count == 0) return Vector3.Zero; // no capture points avaliable

            int randomIndex = rand.Next(temp.Count);

            if (hostileOnly)
            {
                while (temp[randomIndex].Owner == team)
                    randomIndex = rand.Next(temp.Count);
            }


            return temp[randomIndex].Position; // get a random spawnpoint from the enemy team
        }

        public static Dictionary<Team, int> GetNearbyPeds(Vector3 Location, float Radius)
        {
            Dictionary<Team, int> PedsNearby = new Dictionary<Team, int>();

            foreach (var team in ModData.Teams)
            {
                PedsNearby[team] = 0; // Initialize count for each team

                List<Ped> allTeamPeds = team.GetAllPeds(); // Get all peds for this team

                if (ModData.PlayerTeam != -1 && ModData.Teams[ModData.PlayerTeam] == team)
                {
                    allTeamPeds.Add(Game.Player.Character); // Add player character to the list if they are on this team
                }

                foreach (var ped in allTeamPeds)
                {
                    if (ped.IsDead || !ped.IsAlive) continue; // Skip dead or non-alive peds

                    float distanceSq = ped.Position.DistanceToSquared(Location);

                    if (distanceSq <= Radius * Radius)
                    {
                        PedsNearby[team]++; // Increment count for this team if within radius
                    }
                }
            }

            return PedsNearby;
        }

        public static Vector3 GenerateRandomOffset()
        {
            float offsetX = 0;
            float offsetY = 0;

            while (Math.Abs(offsetX) < 1 && Math.Abs(offsetY) < 1) // ensure the offset is not too small
            {
                offsetX = rand.Next(-5, 6);
                offsetY = rand.Next(-5, 6);
            }

            return new Vector3(offsetX, offsetY, 0);
        }

    }
}
