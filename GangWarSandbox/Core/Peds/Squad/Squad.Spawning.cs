
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
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using GangWarSandbox.Core.StrategyAI;
using GangWarSandbox.Core;

namespace GangWarSandbox.Peds
{
    public partial class Squad
    {
        // Where the squad spawns. Only used once to ensure they spawn together
        public Vector3 SpawnPos;

        // The index of the currently spawned ped. Never used once spawning process has completed
        // Used for determining positions of seat in vehicle
        // Starts at -1; increments every time someone is spawned (think: starts counting at zero) 
        public int SpawnIndex = -1;

        public bool IsDestroyed = false;

        public List<Ped> CleanupDead()
        {
            List<Ped> deadPeds = new List<Ped>();

            if (Members.Count == 0)
            {
                IsDestroyed = true;
                Owner.Squads.Remove(this);
                Owner.VehicleSquads.Remove(this);
                Owner.WeaponizedVehicleSquads.Remove(this);
                Owner.HelicopterSquads.Remove(this);

                if (Owner.VehicleSquads.Contains(this))
                {
                    Owner.VehicleSquads.Remove(this);
                }
                else if (Owner.WeaponizedVehicleSquads.Contains(this))
                {
                    Owner.WeaponizedVehicleSquads.Remove(this);
                }
                else if (Owner.HelicopterSquads.Contains(this))
                {
                    Owner.HelicopterSquads.Remove(this);
                }

                try
                {
                    SquadVehicle.AttachedBlip.Delete();
                    SquadVehicle.Health = 50;
                    SquadVehicle.IsPersistent = false; // remove from memory

                    ModData.SquadlessVehicles.Add(SquadVehicle);
                    SquadVehicle = null;


                } 
                catch { }
                

                ModData.CurrentGamemode.OnSquadDestroyed(this, Owner);
                return null;
            }


            // iterate from the end, for safety
            for (int i = Members.Count - 1; i >= 0; i--)
            {
                if (Members[i].IsDead)
                {
                    deadPeds.Add(Members[i]);
                    if (Members[i].AttachedBlip != null && Members[i].AttachedBlip.Exists())
                        Members[i].AttachedBlip.Delete();

                    ModData.CurrentGamemode.OnPedKilled(Members[i], Owner);
                    Members.RemoveAt(i);
                }
            }

            return deadPeds;
        }

        public void Destroy(bool killPeds = true)
        {
            IsDestroyed = true;

            foreach (var ped in Members)
            {
                if (ped != null)
                {
                    if (ped.AttachedBlip != null && ped.AttachedBlip.Exists())
                        ped.AttachedBlip.Delete();

                    if (ped.Exists())
                        ped.Delete();
                }
            }

            if (SquadVehicle != null && SquadVehicle.Exists())
                SquadVehicle.Delete();

            Members.Clear(); // clear list after cleanup
        }

        // When the squad is spawned, spawn its peds 
        private void SpawnSquadPeds(int num)
        {
            if (num == 0) num = 4; // if the squad is invalidly sized, give it a default value

            SquadLeader = SpawnPed(Owner, true);
            SpawnIndex++; // must increase spawn index, to ensure vehicle squads are spawned correctly

            for (int i = 0; i < num - 1; i++)
            {
                Ped ped = SpawnPed(Owner, false);
                SpawnIndex++;
            }
        }

        public Ped CreatePedInWorld(Model model, Vector3 spawnPos, bool isSquadLeader = false)
        {
            Ped ped;

            if (SquadVehicle == null)
                ped = World.CreatePed(model, spawnPos);
            else
            {
                if (SquadVehicle.IsSeatFree((VehicleSeat)SpawnIndex))
                {
                    ped = SquadVehicle.CreatePedOnSeat((VehicleSeat)SpawnIndex, model);
                }
                else ped = null;
            }

            return ped;
        }

        public void SpawnVehicle(VehicleSet.Type type, Vector3 position)
        {
            Model model = Owner.TeamVehicles.ChooseVehicleModel(type);
            if (model == null) return;

            if (!model.IsValid || !model.IsVehicle) return;

            SquadVehicle = World.CreateVehicle(model, position);

            SquadVehicle.AddBlip();

            if (type == VehicleSet.Type.Vehicle)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.GangVehiclePolice;
            else if (type == VehicleSet.Type.WeaponizedVehicle)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.WeaponizedTampa;
            else if (type == VehicleSet.Type.Helicopter)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;

            SquadVehicle.AttachedBlip.Name = $"Team {Owner.Name} Vehicle";
            SquadVehicle.AttachedBlip.Color = Owner.BlipColor;

        }

        public Vector3 FindRandomPositionAroundSpawnpoint(Vector3 spawnpoint)
        {
            Vector3 newSpawnPoint = spawnpoint;
            int radius = 10;

            bool pointInvalid = true;
            int attempts = 0;

            while (pointInvalid)
            {
                newSpawnPoint = spawnpoint;
                attempts++;

                Vector3 randXOffset = new Vector3(rand.Next(-radius, radius), 0, 0);
                Vector3 randYOffset = new Vector3(0, rand.Next(-radius, radius), 0);

                newSpawnPoint += randXOffset + randYOffset;

                int safetyRadius = SquadVehicle == null ? 1 : 6;

                bool noPedsNearby = World.GetNearbyPeds(newSpawnPoint, safetyRadius).Length == 0;
                bool noBuildingsNearby = World.GetNearbyBuildings(newSpawnPoint, safetyRadius).Length == 0;

                // Testing spots for safety
                Vector3 testSpot = Vector3.Zero;

                if (SquadVehicle != null)
                {
                    testSpot = World.GetNextPositionOnStreet(newSpawnPoint, true);

                    if (testSpot != Vector3.Zero && testSpot.DistanceTo(spawnpoint) <= 20f)
                        newSpawnPoint = testSpot; // if the point is valid, use it as the spawn point
                }


                if (noPedsNearby && noBuildingsNearby) pointInvalid = false;

                if (attempts > 10 && pointInvalid)
                {
                    pointInvalid = false;
                    newSpawnPoint = spawnpoint; // if we can't find a valid spawn point, send back an invalid point
                }


            }

            return newSpawnPoint;

        }

        // SpawnPed -- Spawns a ped based on the team, with a given loadout.
        public Ped SpawnPed(Team team, bool isSquadLeader)
        {
            int pedValue = 0;
            int baseAccuracy = 15;

            if (team.SpawnPoints.Count == 0 || team.Models.Length == 0 || team.Tier1Weapons.Length == 0 || team.Tier2Weapons.Length == 0 || team.Tier3Weapons.Length == 0)
                return null;

            // Tier 4 peds only spawn when there is no tier 4 living tier 4 ped of that team AND when the player is on an opposing team
            bool playerHasNoTeam = ModData.PlayerTeam == -1 || ModData.PlayerTeam == -2 || ModData.PlayerTeam < 0 || ModData.PlayerTeam >= ModData.Teams.Count || ModData.Teams[ModData.PlayerTeam] == null;

            bool shouldSpawnTier4 = (team.Tier4Ped == null || !team.Tier4Ped.Exists() || team.Tier4Ped.IsDead) && !playerHasNoTeam;

            int tier;
            int rnum = rand.Next(0, 100);

            rnum = (int)(rnum * team.TierUpgradeMultiplier);

            if (isSquadLeader || rnum >= 95) tier = 3;
            else if (rnum >= 60) tier = 2;
            else tier = 1;


            if (shouldSpawnTier4)
                tier = 4;

            var model = new Model(team.Models[rand.Next(team.Models.Length)]);
            if (tier == 4 && !string.IsNullOrEmpty(team.Faction.Tier4Model)) model = new Model(team.Faction.Tier4Model);

            if (!model.IsValid || !model.IsInCdImage) return null;
            model.Request(500);
            if (!model.IsLoaded) return null;


            var ped = CreatePedInWorld(model, SpawnPos, isSquadLeader);

            if (ped == null) return null;

            ped.RelationshipGroup = team.Group;

            String weapon = "";

            Blip blip = ped.AddBlip();

            if (tier == 1)
            {
                weapon = Helpers.GetRandom(team.Tier1Weapons);
                ped.Health = team.BaseHealth;
                baseAccuracy = 7;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;
                pedValue = 40;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2);
            }
            else if (tier == 2)
            {
                weapon = Helpers.GetRandom(team.Tier2Weapons);
                ped.Health = (int)(team.BaseHealth * 1.2f) ;
                baseAccuracy = 15;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;
                pedValue = 100;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2); // offensive
            }
            else if (tier == 3)
            {
                weapon = Helpers.GetRandom(team.Tier3Weapons);
                ped.Health = (int)(team.BaseHealth * 1.5f);
                baseAccuracy = 30;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.6f;
                pedValue = 280;

                ped.CanSufferCriticalHits = false;


                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 60, true); // Throws smoke grenades
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2); // offensive
            }
            else if (tier == 4)
            {
                weapon = Helpers.GetRandom(team.Tier3Weapons);
                ped.Health = (int)(team.BaseHealth * 2.0);
                baseAccuracy = 75;

                blip.Sprite = BlipSprite.Juggernaut;
                pedValue = 675;

                team.Tier4Ped = ped;

                ped.Armor = 350;
                ped.CanWrithe = false;
                ped.IsFireProof = true;
                ped.IsInvincible = false;
                ped.CanSufferCriticalHits = false; // ped won't die if they get shot in the head (most will anyways)

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 3); // suicidal

            }
            else
            {
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.5f;
                weapon = "WEAPON_PISTOL";
            }

            // Calculate an accuracy bonus
            ped.Accuracy = baseAccuracy + team.Accuracy; // adds the accuracy bonus

            if (!string.IsNullOrEmpty(weapon))
            {
                ped.Weapons.Give((WeaponHash)Game.GenerateHash(weapon), 999, true, true);
            }
            else
            {
                ped.Weapons.Give(WeaponHash.Pistol, 999, true, true);
            }

            // Force equip weapon
            Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, Game.GenerateHash(weapon), true);

            blip.IsShortRange = true;
            blip.Name = $"Team {team.Name} Infantry";
            blip.Color = team.BlipColor;

            ped.AlwaysKeepTask = true;
            ped.HearingRange = 5;
            ped.IsPersistent = true;
            ped.LodDistance = 750; // Increase the distance at which peds will do tasks
            ped.DropsEquippedWeaponOnDeath = false;

            Members.Add(ped);
            PedAssignments[ped] = PedAssignment.None;

            // Combat Flags
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 0, true);  // Always fight
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);  // Can use cover
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);  // Can fight armed when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 21, false); // Can drag friends to safety
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 22, true); // Can drag friends to safety
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 50, true); // Can charge
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 58, true); // Don't flee from combat
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 53, true); // Advance if no cover avaliable
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 42, true); // Can flank
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 28, true); // Advance if frustrated (can't see the enemy?)
            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped, 77, true); // Disable threat broadcast

            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 70f);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped, 1); // medium
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, ped, 1);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped, 1); // 0 = near, 1 = medium, 2 = far

            Function.Call(Hash.SET_PED_LOD_MULTIPLIER, ped, 10.0f);
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, ped, true, true);
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, ped, true);

            Function.Call(Hash.SET_PED_PATH_MAY_ENTER_WATER, ped, true);
            Function.Call(Hash.SET_PED_PATH_PREFER_TO_AVOID_WATER, ped, false);



            // Fight against any nearby targets, at an even greater range than normal behavior

            PedTargetCache[ped] = (null, 0);
            PedAssignments[ped] = PedAssignment.None;

            squadValue += pedValue;
            return ped;
        }

    }



}