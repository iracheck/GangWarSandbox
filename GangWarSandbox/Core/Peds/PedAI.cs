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
        // Helper classes
        static GangWarSandbox ModData = GangWarSandbox.Instance;
        static Random rand = new Random();

        public static void RunTo(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 2f, -1, 5.0f, 0, 0.0f);
        }
        public static void GoToFarAway(Ped ped, Vector3 coord)
        {
            Function.Call(Hash.TASK_FOLLOW_NAV_MESH_TO_COORD, ped, coord.X, coord.Y, coord.Z, 1.5f, -1, 5.0f, 0, 0.0f);
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

        public static List<Vector3> GetIntermediateWaypoints(Vector3 start, Vector3 end, float maxStepSize = 50f, bool followRoads = true)
        {
            List<Vector3> points = new List<Vector3>();


            Vector3 direction = end - start;

            float distance = direction.Length();
            direction.Normalize(); // convert it to a normal vector, so we know which direction to count in


            int numSteps = (int)(distance / maxStepSize);

            //points.Add(start);

            if (distance > maxStepSize && numSteps > 0) {
                for (int i = 1; i < numSteps; i++)
                {
                    Vector3 step = start + direction * (i * maxStepSize);
                    step = World.GetSafeCoordForPed(step, followRoads);

                    if (step != Vector3.Zero)
                    {
                        step += GenerateRandomOffset(); // Let's add a slight randomization to the route, just so it doesn't look like they're going in a straight line

                        points.Add(step);
                    }
                }
            }

            points.Add(end);

            return points;
        }





        // The following are all helper methods to help peds reach locations, or find destinations. They do not directly control peds.



        public static Vector3 FindRandomEnemySpawnpoint(Team team)
        {
            List<Team> temp = ModData.Teams.Where(t => t != team && t.SpawnPoints.Count > 0).ToList();

            if (temp.Count == 0) return Vector3.Zero; // no enemy teams with spawnpoints

            int randomIndex = rand.Next(temp.Count);

            return temp[randomIndex].SpawnPoints[rand.Next(0, temp[randomIndex].SpawnPoints.Count)]; // get a random spawnpoint from the enemy team
        }

        public static Vector3 FindRandomCapturePoint(Team team)
        {
            List<CapturePoint> temp = ModData.CapturePoints;


            if (temp.Count == 0) return Vector3.Zero; // no capture points avaliable

            int randomIndex = rand.Next(temp.Count);

            return temp[randomIndex].Position; // get a random spawnpoint from the enemy team
        }

        public static Dictionary<Team, int> GetNearbyPeds(Vector3 Location, float Radius)
        {
            Dictionary<Team, int> PedsNearby = new Dictionary<Team, int>();

            foreach (var team in ModData.Teams)
            {
                if (team.SpawnPoints.Count == 0) continue;
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
