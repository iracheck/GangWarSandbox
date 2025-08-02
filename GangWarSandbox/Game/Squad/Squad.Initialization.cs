
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
using System.Data;

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        public enum SquadRole
        {
            Idle,

            DefendCapturePoint, // defend a capture point from enemies trying to take it
            AssaultCapturePoint, // capture a capture point by attacking it and any squads nearby
            ReinforceAllies,
            SeekAndDestroy, // assault a random enemy spawn point
            ChargeCapturePoint,

            VehicleSupport,

            //AirSupport = 31,
            //AirDrop = 32,


        }

        // Personality -- how a squad reacts to certain situations, gives a dynamic feel to the battlefield
        public enum SquadPersonality
        {
            Normal, // the squad will not act in any particular way. the majority of squads
            Aggressive, // the squad will act more aggressively, and move more quickly
        }

        // Squad type is only used for spawning the squad.
        public enum SquadType
        {
            Infantry = 0,
            Sniper = 1,
            Garrison = 2,

            CarVehicle = 11,
            WeaponizedVehicle = 12,

            AirHeli = 20,

            Naval = 30,
        }

        /// <summary>
        /// Creates a new squad belonging to a given team, with spawn location depending on the current gamemode.
        /// </summary>
        /// <param name="owner">The team that the squad spawns for</param>
        /// <param name="vehicle">0=infantry,1=vehicle,2=wep. vehicle,3=helicopter</param>
        /// <param name="role"></param>
        /// <param name="type"></param>
        /// <param name="personality"></param>
        public Squad(Team owner, SquadType type = 0, SquadRole role = 0, SquadPersonality personality = 0)
        {
            Owner = owner;
            Role = role;
            Personality = personality;
            Type = type;

            bool canContinue;

            canContinue = GetSpawnpoint();
            if (!canContinue) return;

            InitializeSquadType();

            InitializeSquadRole();

            InitializeSquadPersonality();

           

            PedTargetCache = new Dictionary<Ped, (Ped enemy, int timestamp)>();
            SpawnSquadPeds(GetSquadSizeByType(Type));

            Vector3 target = CurrentGamemode.GetTarget(this); // get a random target for the squad to attack
            SetTarget(target);

            // Update as soon as they are spawned, to avoid them "looking dumb" for ~half a second.
            Update();
        }

        public bool GetSpawnpoint()
        {
            // determine a spawnpoint
            Vector3 spawnpoint = Vector3.Zero;

            if (CurrentGamemode.SpawnMethod == Gamemode.GamemodeSpawnMethod.Spawnpoint && Owner.SpawnPoints.Count > 0)
            {
                spawnpoint = Owner.SpawnPoints[rand.Next(Owner.SpawnPoints.Count)];

                if (spawnpoint == null || spawnpoint == Vector3.Zero) return false;
                SpawnPos = FindRandomPositionAroundSpawnpoint(spawnpoint);
            }
            else if (CurrentGamemode.SpawnMethod == Gamemode.GamemodeSpawnMethod.Random)
            {
                SpawnPos = FindRandomPositionAroundPlayer(200);
            }

            if (SpawnPos == null || SpawnPos == Vector3.Zero) return false;

            if (IsSpawnPositionCrowded(SpawnPos)) return false;

            return true;
        }

        public void InitializeSquadType()
        {
            if (Type == SquadType.AirHeli && ModData.CurrentGamemode.SpawnHelicopters) // helicopter
            {
                SpawnPos.Z += 95;

                SpawnVehicle(VehicleSet.Type.Helicopter, SpawnPos);
                Owner.HelicopterSquads.Add(this);
            }
            else if (Type == SquadType.WeaponizedVehicle && ModData.CurrentGamemode.SpawnWeaponizedVehicles) // weaponized vehicle
            {
                IsWeaponizedVehicle = true;
                SpawnVehicle(VehicleSet.Type.WeaponizedVehicle, SpawnPos);
                Owner.WeaponizedVehicleSquads.Add(this);
            }
            else if (Type == SquadType.CarVehicle && ModData.CurrentGamemode.SpawnVehicles) // reg vehicle
            {
                SpawnVehicle(VehicleSet.Type.Vehicle, SpawnPos);
                Owner.VehicleSquads.Add(this);
            }
            else // infantry
            {
                Owner.Squads.Add(this);
            }
        }

        public void InitializeSquadRole()
        {
            if (Role == 0)
            {
                if (SquadVehicle != null)
                {
                    Role = SquadRole.VehicleSupport;
                }
                else
                {
                    int max = 20; // base weight of seek and destroy is 20
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
            }
        }

        private void InitializeSquadPersonality()
        {
            if (Personality == 0)
            {
                int randNum = rand.Next(0, 101);

                if (randNum <= 50) // 50% chance to be aggressive
                    Personality = SquadPersonality.Aggressive;
                else
                    Personality = SquadPersonality.Normal;
            }
        }
    }




}