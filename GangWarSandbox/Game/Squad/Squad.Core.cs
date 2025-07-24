
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
using System.ComponentModel;
using GangWarSandbox.Core.StrategyAI;
using GangWarSandbox.Core;
using GangWarSandbox.Gamemodes;

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        Random rand = new Random();
        PedAI pedAI = new PedAI();

        GangWarSandbox ModData = GangWarSandbox.Instance;
        Gamemode CurrentGamemode;

        // Squad Logic begins here

        public bool JustSpawned = true;

        public Ped SquadLeader;
        public List<Ped> Members = new List<Ped>();

        public Team Owner;

        public int squadValue; // lower value squads may be assigned to less important tasks
        public float squadAttackRange = 55f;


        public List<Vector3> Waypoints = new List<Vector3>();
        public Dictionary<Ped, PedAssignment> PedAssignments = new Dictionary<Ped, PedAssignment>();
        Dictionary<Ped, (Ped enemy, int timestamp)> PedTargetCache;

        // Squad Stuck Timer-- if the squad leader is stuck for too long, it will try to move again
        private int SquadLeaderStuckTicks = 0;

        public SquadRole Role;
        public SquadType Type;
        public SquadPersonality Personality;

        // Abstract Orders
        // these are orders that come from the "Strategy AI" of each team
        public CapturePoint TargetPoint; // the location that the squad's role will be applied to-- variable

        public Vehicle SquadVehicle = null;
        public bool IsWeaponizedVehicle;

        // Squad roles are the command to the squad from AI overseer

        public enum SquadType
        {
            InfantryRandom = 0,
            Infantry = 1,
            Sniper = 2,
            Garrison = 3,

            VehicleRandom = 10,
            CarVehicle = 11,
            WeaponizedVehicle = 12,

            AirHeli = 20,
            AirHeliReinforce = 21,
            AirPlane = 22,

            Naval = 30,
        }

        // Personality -- how a squad reacts to certain situations, gives a dynamic feel to the battlefield
        public enum SquadPersonality
        {
            Normal = 0, // the squad will not act in any particular way. the majority of squads
            Aggressive = 1, // the squad will act more aggressively, and move more quickly
        }

        /// <summary>
        /// Creates a new squad belonging to a given team, with spawn location depending on the current gamemode.
        /// </summary>
        /// <param name="owner">The team that the squad spawns for</param>
        /// <param name="vehicle">0=infantry,1=vehicle,2=wep. vehicle,3=helicopter</param>
        /// <param name="role"></param>
        /// <param name="type"></param>
        /// <param name="personality"></param>
        public Squad(Team owner, int vehicle = 0, SquadRole role = 0, SquadType type = 0, SquadPersonality personality = 0)
        {
            Owner = owner;
            Role = role;
            Personality = personality;
            Type = type;

            CurrentGamemode = ModData.CurrentGamemode;

            // First, determine a spawnpoint
            Vector3 spawnpoint = Vector3.Zero;

            if (CurrentGamemode.GMSpawnMethod == Gamemode.SpawnMethod.Spawnpoint && owner.SpawnPoints.Count > 0)
            {
                spawnpoint = Owner.SpawnPoints[rand.Next(Owner.SpawnPoints.Count)];
            }
            else if (CurrentGamemode.GMSpawnMethod == Gamemode.SpawnMethod.Random)
            {
                spawnpoint = FindRandomPositionAroundPlayer(200);
            }

            if (spawnpoint == Vector3.Zero)
            {
                return;
            }
            // Find a random point around the spawn position to actually spawn in
            SpawnPos = FindRandomPositionAroundSpawnpoint(spawnpoint);

            if (IsSpawnPositionCrowded(SpawnPos)) return;

            if (vehicle != 1)
            {
                if (Owner.TeamVehicles == null || Owner.TeamVehicles.Vehicles.Count == 0)
                {
                    vehicle = 0; // no vehicles available, set to regular squad
                }
            }

            if (vehicle == 3 && ModData.CurrentGamemode.SpawnHelicopters) // helicopter
            {
                SpawnPos.Z += 95;

                SpawnVehicle(VehicleSet.Type.Helicopter, SpawnPos);
                Owner.HelicopterSquads.Add(this);
            }
            else if (vehicle == 2 && ModData.CurrentGamemode.SpawnWeaponizedVehicles) // weaponized vehicle
            {
                IsWeaponizedVehicle = true;
                SpawnVehicle(VehicleSet.Type.WeaponizedVehicle, SpawnPos);
                Owner.WeaponizedVehicleSquads.Add(this);
            }
            else if (vehicle == 1 && ModData.CurrentGamemode.SpawnVehicles) // reg vehicle
            {
                SpawnVehicle(VehicleSet.Type.Vehicle, SpawnPos);
                Owner.VehicleSquads.Add(this);
            }
            else
            {
                Owner.Squads.Add(this);
            }

            if (role == 0)
            {
                if (vehicle != 0 || SquadVehicle != null)
                {
                    Role = SquadRole.VehicleSupport;
                }

                int max = 20; // weights for seek and destroy included in the max
                int assault = 0;
                int defend = 0;

                assault += StrategyAIHelpers.CalculateNeedToAssaultPoint(Owner);
                defend += StrategyAIHelpers.CalculateNeedToDefendPoint(Owner);
                max += assault + defend;

                int randNum = rand.Next(0, max);

                if (randNum <= assault) // Assault
                {
                    Role = SquadRole.AssaultCapturePoint;
                }
                else if (randNum <= defend + assault) // Defend
                {
                    Role = SquadRole.DefendCapturePoint;
                }
                else
                {
                    Role = SquadRole.SeekAndDestroy; // default role if no other roles are available
                }
            }

            if (personality == 0)
            {
                int randNum = rand.Next(0, 101);

                if (randNum <= 50) // 50% chance to be aggressive
                    Personality = SquadPersonality.Aggressive;
                else
                    Personality = SquadPersonality.Normal;
            }

            PedTargetCache = new Dictionary<Ped, (Ped enemy, int timestamp)>();
            SpawnSquadPeds(GetSquadSizeByType(Type));

            Vector3 target = CurrentGamemode.GetTarget(this); // get a random target for the squad to attack
            SetTarget(target);
        }

        // Runs every 200ms (default) and updates all AI, squad states, etc.
        public bool Update()
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

            bool isCloseEnough = (SquadLeader.Position.DistanceTo(Waypoints[0]) < 15f) || (SquadVehicle != null && SquadVehicle.Position.DistanceTo(Waypoints[0]) < 40f);
            bool waypointSkipped = Waypoints.Count > 1 && Waypoints[1] != null && Waypoints[1] != Vector3.Zero && SquadLeader.Position.DistanceTo(Waypoints[1]) < 50f &&
                Waypoints[0].DistanceTo(SquadLeader.Position) > Waypoints[1].DistanceTo(SquadLeader.Position);

            // Clear nearby waypoints
            if (isCloseEnough || waypointSkipped)
            {
                Waypoints.RemoveAt(0);
                foreach (var ped in Members)
                {
                    if (PedAssignments[ped] == PedAssignment.RunToPosition || PedAssignments[ped] == PedAssignment.DriveToPosition)
                    {
                        PedAssignments[ped] = PedAssignment.None;
                    }
                }
            }

            // Gamemode: should try to get a new target?
            if (CurrentGamemode.ShouldGetNewTarget(this))
            {
                CurrentGamemode.GetTarget(this);
            }

            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];
                ped.AttachedBlip.Alpha = GetDesiredBlipVisibility(ped, Owner);

                if (ped == null || !ped.Exists() || !ped.IsAlive || ped.IsRagdoll) continue; // skip to the next ped

                // Handle logic with enemy detection, combat, etc.
                bool combat = PedAI_Combat(ped);

                // Handle logic on defending or assaulting capture points
                PedAI_CapturePoint(ped);

                if (ped.IsShooting && ped.IsInCombat|| PedAssignments[ped] == PedAssignment.AttackNearby || combat) continue;

                // Handle logic with ped moving to and from its target
                bool movementChecked = PedAI_Driving(ped);
                if (!movementChecked) PedAI_Movement(ped);
            }

            return true;
        }


        public void PromoteLeader()
        {
            foreach (var ped in Members)
            {
                if (ped.Exists() && !ped.IsDead)
                {
                    if (ped.IsInVehicle() && ped.CurrentVehicle.Driver == ped) continue; // do not promote a driver as leader
                    SquadLeader = ped;
                    return;
                }
            }
        }

        public bool IsEmpty()
        {
            if (Members.Count <= 0) return true;
            else return false;
        }

        // Temp until squad types in introduced
        private int GetSquadSizeByType(SquadType type)
        {
            return Owner.GetSquadSize();
        }

        // Squad points are used in autocalculated battles
        public int GetSquadPoints()
        {
            int squadSize = GetSquadSizeByType(Type);
            int members = Members.Count;
            float multiplier = members / squadSize;

            int points = 0;

            if (members <= 0) return 0;

            points = (int) (squadValue * multiplier) + 50;
            
            return points;
        }

        private int GetDesiredBlipVisibility(Ped ped, Team team)
        {
            int maxAlpha;

            // Absolute conditions
            if (ped.IsInVehicle() || ped.IsDead) return 0;
            else if (ped == SquadLeader) maxAlpha = 255;
            else maxAlpha = 200;

            if (team.TeamIndex == ModData.PlayerTeam || ModData.PlayerTeam == -1) return maxAlpha;

            if (ModData.DEBUG == 1 || CurrentGamemode.FogOfWar == false) return 255;

            // Relative conditions
            float healthPercent = (float)ped.Health / (float)ped.MaxHealth;
            maxAlpha = (int)(maxAlpha * healthPercent + 10); // health

            if (maxAlpha == 0) return 0;

            float dist = ped.Position.DistanceTo(Game.Player.Character.Position);

            // Distance conditions, only happens when player is on a team
            if (dist > 125f) return 0;
            else if (dist < 50f) return maxAlpha;
            else
            {
                maxAlpha = (int)(maxAlpha * (1 - (dist / 200)));
            }

            return maxAlpha;

        }

    }




}