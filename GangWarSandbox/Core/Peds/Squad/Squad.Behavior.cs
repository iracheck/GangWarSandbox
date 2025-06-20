using GTA.Math;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GangWarSandbox.Peds;
using GangWarSandbox;

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        public enum SquadRole
        {
            Idle = 0,

            DefendCapturePoint = 1,
            AssaultCapturePoint = 2,
            ReinforceAllies = 3,
            SeekAndDestroy = 4,
            ChargeCapturePoint = 5,

            VehicleSupport = 21,

            //AirSupport = 31,
            //AirDrop = 32,


        }

        // Ped Assignnment -- what each ped is doing
        // the squad will behave in a similar way-- inheriting their leader's assignment
        public enum PedAssignment
        {
            None,
            AttackNearby,
            RunToPosition,
            DefendArea,
            FollowSquadLeader,
            PushLocation,


            GetIntoVehicle,
            DriveToPosition,
            AttackEnemiesInVehicle,
        }


        public void SetTarget(Vector3 target)
        {
            Waypoints = PedAI.GetIntermediateWaypoints(SpawnPos, target); // set the waypoints to the target position
        }

        // In cases where Strategy AI does not exist or cannot provide the squad with a target, we can auto generate one
        public Vector3 GetTarget()
        {
            List<CapturePoint> capturePoints = ModData.CapturePoints;
            Vector3 target = Vector3.Zero;

            if (Role == SquadRole.AssaultCapturePoint)
            {
                List<CapturePoint> unownedPoints = new List<CapturePoint>();

                for (int i = 0; i < capturePoints.Count; i++)
                {
                    if (capturePoints[i].Owner == null || capturePoints[i].Owner != Owner)
                    {
                        unownedPoints.Add(capturePoints[i]); // add the capture point to the list of unowned points
                    }
                }

                if (unownedPoints.Count > 0)
                {
                    TargetPoint = unownedPoints[rand.Next(unownedPoints.Count)]; // randomly select a capture point that is not owned by the squad's team
                    target = TargetPoint.Position; // set the target to the capture point's position
                }
                else
                {
                    TargetPoint = null;
                    target = Vector3.Zero;
                }
            }
            else if (Role == SquadRole.DefendCapturePoint)
            {
                for (int i = 0; i < capturePoints.Count; i++)
                {
                    if (capturePoints[i] != null && capturePoints[i].Owner == Owner)
                    {
                        TargetPoint = capturePoints[i]; // set the target point to the capture point owned by the squad's team
                        target = TargetPoint.Position;
                    }
                }
            }
            else if (Role == SquadRole.SeekAndDestroy)
            {
                {
                    target = PedAI.FindRandomEnemySpawnpoint(Owner);

                    if (target == Vector3.Zero && capturePoints.Count > 0)
                    {
                        target = PedAI.FindRandomCapturePoint(Owner);
                    }
                }
            }

            // Final failsafe: ensure a non-zero target is returned
            if (target == Vector3.Zero)
            {
                target = PedAI.FindRandomEnemySpawnpoint(Owner);

                // Still can't find one? Fallback solution
                if (target == Vector3.Zero)
                {
                    GTA.UI.Screen.ShowSubtitle("A squad failed to find a valid target. Please report this issue to the developers.", 1000);
                    target = SquadLeader.Position + PedAI.GenerateRandomOffset(); // generate a random offset from the spawn position if no target is found
                }
            }


            SetTarget(target);
            return target;
        }


        public bool SquadAIHandler()
        {
            if (IsEmpty())
            {
                Destroy();
                return false;
            }

            if (Waypoints.Count == 0) Waypoints.Add(Vector3.Zero);

            if (JustSpawned) JustSpawned = false;

            if (SquadLeader == null || SquadLeader.IsDead || !SquadLeader.Exists())
                PromoteLeader();

            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];

                if (ped == null || !ped.Exists() || !ped.IsAlive) continue;

                // Clear nearby waypoints
                if (Waypoints[0] != Vector3.Zero && ped.Position.DistanceTo(Waypoints[0]) < 10f)
                {
                    Waypoints.RemoveAt(0);
                    if (PedAssignments[ped] == PedAssignment.RunToPosition) PedAssignments[ped] = PedAssignment.None;
                }

                // Handle logic with enemy detection, combat, etc.
                bool combat = PedAI_Combat(ped);

                // Handle logic on defending or assaulting capture points
                PedAI_CapturePoint(ped);

                if (ped.IsInCombat || PedAssignments[ped] == PedAssignment.AttackNearby || combat) continue;

                // Handle logic with ped moving to and from its target
                if (SquadLeader.IsInVehicle())
                    PedAI_Driving(ped);
                PedAI_Movement(ped);
            }

            return true;
        }

        private void PedAI_CapturePoint(Ped ped)
        {
            // Assault Capture Point
            if (Role == SquadRole.AssaultCapturePoint && PedAssignments[ped] != PedAssignment.PushLocation && TargetPoint != null)
            {
                if (PedAssignments[ped] != PedAssignment.PushLocation && ped.Position.DistanceTo(TargetPoint.Position) >= 60f)
                {
                    PedAI.RunToFarAway(ped, TargetPoint.Position);
                    PedAssignments[ped] = PedAssignment.PushLocation; // set the ped to assault the capture point
                }
            }

            // Defend Capture Point
            if (Role == SquadRole.DefendCapturePoint && PedAssignments[ped] != PedAssignment.DefendArea && TargetPoint != null)
            {
                if (PedAssignments[ped] != PedAssignment.RunToPosition && ped.Position.DistanceTo(TargetPoint.Position) >= 20f)
                {
                    PedAssignments[ped] = PedAssignment.RunToPosition; // set the ped to defend the area

                    SetTarget(TargetPoint.Position);
                }
                else
                {
                    PedAI.DefendArea(ped, TargetPoint.Position);
                    PedAssignments[ped] = PedAssignment.DefendArea; // set the ped to defend the area

                }
            }
        }

        private bool PedAI_Combat(Ped ped)
        {
            if (!PedTargetCache.ContainsKey(ped)) PedTargetCache.Add(ped, (null, 0));

            Ped cachedEnemy = PedTargetCache[ped].enemy;
            int lastCheckedTime = PedTargetCache[ped].timestamp;

            Ped nearbyEnemy = cachedEnemy; // find a nearby enemy within the squad attack range

            // Handle ped target caching
            if (Game.GameTime - lastCheckedTime > 150 || cachedEnemy == null || cachedEnemy.IsDead || cachedEnemy.Position.DistanceTo(ped.Position) >= 90f)
            {
                nearbyEnemy = FindNearbyEnemy(ped, Owner, squadAttackRange); // search for a nearby enemy, but allow for a longer search time
                PedTargetCache[ped] = (nearbyEnemy, Game.GameTime); // update the cache with the new target and timestamp
            }

            if (nearbyEnemy != null)
            {
                // First, let's make sure the ped attacks any enemies that are nearby that he can see
                if (PedAI.HasLineOfSight(ped, nearbyEnemy))
                {
                    if (PedAssignments[ped] != PedAssignment.AttackNearby)
                    {
                        PedAI.AttackEnemy(ped, nearbyEnemy);
                        PedAssignments[ped] = PedAssignment.AttackNearby;
                    }
                }
                else if (PedAssignments[ped] != PedAssignment.RunToPosition)
                {
                    ped.Task.ClearAllImmediately(); // breaks their combat state

                    PedAI.RunToFarAway(ped, nearbyEnemy.Position);
                    PedAssignments[ped] = PedAssignment.RunToPosition;
                }

                return true;
            }
            else if (nearbyEnemy == null && PedAssignments[ped] == PedAssignment.AttackNearby)
            {
                PedAssignments[ped] = PedAssignment.None;
            }

            return false;
        }

        private void PedAI_Movement(Ped ped)
        {
            if (ped == SquadLeader)
            {
                if (PedAssignments[ped] != PedAssignment.RunToPosition && Waypoints.Count > 0 && Waypoints[0] != Vector3.Zero) // if the squad has a target, but the squad leader is not moving toward it, move!
                {
                    PedAI.RunToFarAway(ped, Waypoints[0]);
                    PedAssignments[ped] = PedAssignment.RunToPosition;
                }

            }
            else // Squad members
            {
                // Follow the squad leader around
                if (PedAssignments[ped] != PedAssignment.FollowSquadLeader && Vector3.Distance(ped.Position, SquadLeader.Position) > 5f)
                {
                    ped.Task.FollowToOffsetFromEntity(SquadLeader, PedAI.GenerateRandomOffset(), 1.5f);
                    PedAssignments[ped] = PedAssignment.FollowSquadLeader;
                }
            }
        }

        private void PedAI_Driving(Ped ped)
        {
            if (ped == SquadLeader)
            {
                // IF the ped is not in a vehicle, has waypoints, and is not currently entering a vehicle, enter a vehicle
                if (!ped.IsInVehicle() && Waypoints.Count > 0 && PedAssignments[ped] != PedAssignment.GetIntoVehicle)
                {
                    PedAI.EnterVehicle(ped, SquadVehicle);
                    PedAssignments[ped] = PedAssignment.GetIntoVehicle; // set the ped to follow the squad leader
                }

                else if (ped.IsInVehicle() && Waypoints.Count > 0)
                {
                    if (ped.IsInPoliceVehicle && !SquadVehicle.IsSirenActive) SquadVehicle.IsSirenActive = true; // activate the siren if the ped is in a police vehicle

                    if (PedAssignments[ped] != PedAssignment.DriveToPosition)
                    {
                        PedAI.DriveToFarAway(ped, Waypoints[0]);
                        PedAssignments[ped] = PedAssignment.DriveToPosition; // set the ped to drive to the target position
                    }
                }
            }
            else
            {
                if (SquadLeader.CurrentVehicle != null && ped.IsInVehicle() == false)
                {
                    PedAI.EnterVehicle(ped, SquadLeader.CurrentVehicle);
                    PedAssignments[ped] = PedAssignment.GetIntoVehicle; // set the ped to follow the squad leader
                }
            }
        }

        private Ped FindNearbyEnemy(Ped self, Team team, float distance, bool infiniteSearch = false)
        {
            Ped foundEnemy;

            if (PedTargetCache[self].enemy == null || PedTargetCache[self].timestamp > Game.GameTime - 1000)
            {
                // Get all enemy squads from other teams
                var enemySquads = ModData.Teams
                    .Where(t => t != team)
                    .SelectMany(t => t.Squads);

                if (infiniteSearch) squadAttackRange *= 4;

                foundEnemy = enemySquads.SelectMany(s => s.Members)
                        .Where(p => p != null && p.Exists() && !p.IsDead && p.Position.DistanceTo(self.Position) <= squadAttackRange)
                        .OrderBy(p => p.Position.DistanceTo(self.Position))
                        .FirstOrDefault();

                if (foundEnemy == null || !foundEnemy.Exists() || foundEnemy.IsDead)
                {
                    foundEnemy = null;
                }
                else
                {
                    PedTargetCache[self] = (foundEnemy, Game.GameTime); // cache the target for a short time
                }
            }
            else return PedTargetCache[self].enemy; // return the ped's cached target if too soon

            return foundEnemy;
        }
    }
}
