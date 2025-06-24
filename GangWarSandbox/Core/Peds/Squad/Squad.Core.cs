
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

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        Random rand = new Random();
        PedAI pedAI = new PedAI();

        GangWarSandbox ModData = GangWarSandbox.Instance;

        // Squad Logic begins here

        public bool JustSpawned = true;

        public Ped SquadLeader;
        public List<Ped> Members = new List<Ped>();

        public Team Owner;

        public int squadValue; // lower value squads may be assigned to less important tasks
        public float squadAttackRange = 60f;


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
        CapturePoint TargetPoint; // the location that the squad's role will be applied to-- variable

        public Vehicle SquadVehicle = null;

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
            Aggressive = 1, // the squad may not wait for combat to end to push its target
        }

        public Squad(Team owner, int vehicle = 0, SquadRole role = 0, SquadType type = 0, SquadPersonality personality = 0)
        {
            Owner = owner;
            Role = role;
            Personality = personality;
            Type = type;

            SpawnPos = Owner.SpawnPoints[rand.Next(Owner.SpawnPoints.Count)];

            if (!IsSpawnPosSafe(SpawnPos)) return;
            else Owner.Squads.Add(this);

            if (vehicle == 3) // helicopter
            {
                SpawnVehicle(VehicleSet.Type.Helicopter, SpawnPos);
                Owner.WeaponizedVehicleSquads.Add(this);
            }
            else if (vehicle == 2) // wpnzd vehicle
            {
                SpawnVehicle(VehicleSet.Type.WeaponizedVehicle, SpawnPos);
                Owner.WeaponizedVehicleSquads.Add(this);
            }
            else if (vehicle == 1) // reg vehicle
            {
                SpawnVehicle(VehicleSet.Type.Vehicle, SpawnPos);
                Owner.VehicleSquads.Add(this);
            }
            // if vehicle == 0, it's a regular squad

            if (role == 0)
            {
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

            // Vehicle creation is handled within the squad

            PedTargetCache = new Dictionary<Ped, (Ped enemy, int timestamp)>();
            SpawnSquadPeds(GetSquadSizeByType(Type));

            Waypoints.Add(Vector3.Zero);

            GetTarget(); // get a random target for the squad to attack
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

    }


}