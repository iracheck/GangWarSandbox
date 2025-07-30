
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
                    ModData.CurrentGamemode.OnPedKilled(Members[i], Owner);
                    deadPeds.Add(Members[i]);
                    if (Members[i].AttachedBlip != null && Members[i].AttachedBlip.Exists())
                        Members[i].AttachedBlip.Delete();

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
            if (Owner.TeamVehicles == null) return;

            Model model = Owner.TeamVehicles.ChooseVehicleModel(type);
            if (model == null) return;

            if (!model.IsValid || !model.IsVehicle) return;

            SquadVehicle = World.CreateVehicle(model, position);

            if (SquadVehicle == null) return;

            CurrentGamemode.OnVehicleSpawn(SquadVehicle);
            SquadVehicle.AddBlip();

            if (SquadVehicle.AttachedBlip == null) return;

            if (type == VehicleSet.Type.Vehicle && (SquadVehicle.IsBicycle || SquadVehicle.IsMotorcycle) ) 
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.Motorcycle;
            else if (type == VehicleSet.Type.Vehicle)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.GangVehiclePolice;
            else if (type == VehicleSet.Type.WeaponizedVehicle)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.TechnicalAqua;
            else if (type == VehicleSet.Type.Helicopter)
                SquadVehicle.AttachedBlip.Sprite = BlipSprite.HelicopterAnimated;

            SquadVehicle.AttachedBlip.Name = $"Team {Owner.Name} Vehicle";
            SquadVehicle.AttachedBlip.Color = Owner.BlipColor;

        }

        public bool IsSpawnPositionCrowded(Vector3 pos, float minDistance = 5f)
        {
            var nearbyPeds = World.GetAllPeds().Where(p => p.Exists() && p.Position.DistanceTo(pos) < minDistance);

            if (nearbyPeds.Count() > 10)
            {
                return true;
            }

            return false;
        }

        public Vector3 FindRandomPositionAroundPlayer(int radius = 200, int minRadius = 100)
        {
            Ped player = Game.Player.Character;
            Vector3 playerPos = player.Position;
            Vector3 newSpawnPoint = playerPos;

            int attempts = 0;
            int MAX_ATTEMPTS = 15;

            // Adjust radius if in vehicle
            bool playerInVehicle = player.IsInVehicle();
            if (playerInVehicle)
            {
                radius *= 2;
                minRadius *= 3;
            }

            if (radius < minRadius) radius = minRadius;

            Vector3 forward = player.ForwardVector.Normalized;

            while (true)
            {
                attempts++;

                float distance = (float)(minRadius + rand.NextDouble() * (radius - minRadius));
                float angle;

                // Forward bias for vehicle mode
                if (playerInVehicle)
                {
                    if (rand.NextDouble() < 0.8)
                    {
                        float cone = 25f * (float)Math.PI / 180f;
                        float forwardAngle = (float)Math.Atan2(forward.Y, forward.X);
                        angle = forwardAngle + (float)(rand.NextDouble() * cone - cone / 2f);
                        distance *= 1.3f;
                    }
                    else
                    {
                        angle = (float)(rand.NextDouble() * Math.PI * 2);
                    }
                }
                else
                {
                    angle = (float)(rand.NextDouble() * Math.PI * 2);
                }

                Vector3 offset = new Vector3(
                    (float)(Math.Cos(angle) * distance),
                    (float)(Math.Sin(angle) * distance),
                    0f
                );

                newSpawnPoint = playerPos + offset;

                // ✅ Use GET_CLOSEST_VEHICLE_NODE_WITH_HEADING for precise lane snapping
                OutputArgument outCoords = new OutputArgument();
                OutputArgument outHeading = new OutputArgument();

                Function.Call(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,
                    newSpawnPoint.X, newSpawnPoint.Y, newSpawnPoint.Z,
                    outCoords, outHeading, 1, 3.0f, 0);

                Vector3 roadPos = outCoords.GetResult<Vector3>();
                float roadHeading = outHeading.GetResult<float>();

                if (roadPos != Vector3.Zero)
                {
                    newSpawnPoint = roadPos;

                    // Move slightly toward lane center using perpendicular to road heading
                    Vector3 perpendicular = new Vector3(
                        (float)-Math.Sin(roadHeading * Math.PI / 180f),
                        (float)Math.Cos(roadHeading * Math.PI / 180f),
                        0f
                    ).Normalized;

                    newSpawnPoint += perpendicular * 1.5f; // tweak as needed
                }

                // Avoid crowded areas
                bool noEntitiesNearby = World.GetNearbyEntities(newSpawnPoint, 5f).Length == 0;
                bool noPedsNearby = World.GetNearbyPeds(newSpawnPoint, 5f).Length == 0;
                if (!(noEntitiesNearby && noPedsNearby))
                {
                    if (attempts >= MAX_ATTEMPTS) return Vector3.Zero;
                    continue;
                }

                // Z-level check
                if (newSpawnPoint.Z < player.Position.Z - 10f || newSpawnPoint.Z > player.Position.Z + 10f)
                {
                    if (attempts >= MAX_ATTEMPTS - 3)
                    {
                        if (attempts >= MAX_ATTEMPTS) return Vector3.Zero;
                        if (newSpawnPoint.Z < player.Position.Z - 50f || newSpawnPoint.Z > player.Position.Z + 50f)
                        {
                            continue;
                        }
                    }
                    else continue;
                }

                if (newSpawnPoint.DistanceTo2D(playerPos) < (IsVehicleSquad() ? 150f : 100f))
                {
                    if (attempts >= MAX_ATTEMPTS) return Vector3.Zero;
                    continue;
                }

                // Check if under the map
                Vector3 rayStart = newSpawnPoint + new Vector3(0, 0, 100f);
                Vector3 rayEnd = newSpawnPoint;
                RaycastResult downcast = World.Raycast(rayStart, rayEnd, IntersectFlags.Map);
                RaycastResult upcast = World.Raycast(newSpawnPoint, newSpawnPoint + new Vector3(0, 0, 15f), IntersectFlags.Map);

                if (!upcast.DidHit && downcast.DidHit && downcast.HitPosition.DistanceTo(newSpawnPoint) <= 15f)
                {
                    newSpawnPoint = downcast.HitPosition;
                }

                return newSpawnPoint;
            }
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

                int safetyRadius;

                if (SquadVehicle != null && SquadVehicle.IsHelicopter) safetyRadius = 13;
                else if (SquadVehicle != null) safetyRadius = 10;
                                          else safetyRadius = 3;

                // Testing spots for safety
                Vector3 testSpot = Vector3.Zero;

                if (SquadVehicle != null)
                {
                    testSpot = World.GetNextPositionOnStreet(newSpawnPoint, true);

                    if (testSpot != Vector3.Zero && testSpot.DistanceTo(spawnpoint) <= 30f)
                        newSpawnPoint = testSpot; // if the point is valid, use it as the spawn point
                }

                bool noPedsNearby = World.GetNearbyPeds(newSpawnPoint, safetyRadius).Length == 0;
                bool noBuildingsNearby = World.GetNearbyBuildings(newSpawnPoint, safetyRadius).Length == 0;
                bool noObjectsNearby = World.GetNearbyProps(newSpawnPoint, safetyRadius).Length == 0;
                bool noEntitiesNearby = World.GetNearbyEntities(newSpawnPoint, safetyRadius).Length == 0;

                if (noPedsNearby && noBuildingsNearby && noObjectsNearby && noEntitiesNearby) pointInvalid = false;
                else continue;

                if (attempts > 10 && pointInvalid)
                {
                    pointInvalid = false;
                    newSpawnPoint = spawnpoint; // if we can't find a valid spawn point after multiple attempts, spawn at the center point
                }

                // can the old spawnpoint "see" the new one?
                RaycastResult raycast = World.Raycast(spawnpoint, newSpawnPoint, IntersectFlags.Map | IntersectFlags.Objects);

                if (raycast.DidHit && SquadVehicle == null) continue;

                // By seeing that a raycast downward finds something, but a raycast upward doesn't-- we can discover that the ped is being spawned under a map.
                // Not a foolproof implementation, but it has proven to solve 90% of issues with spawning.
                Vector3 rayStart = newSpawnPoint + new Vector3(0, 0, 100f);
                Vector3 rayEnd = newSpawnPoint + new Vector3(0, 0, 0f);
                RaycastResult downcast = World.Raycast(rayStart, rayEnd, IntersectFlags.Map);

                Vector3 upStart = newSpawnPoint;
                Vector3 upEnd = newSpawnPoint + new Vector3(0, 0, 15f);
                RaycastResult upcast = World.Raycast(upStart, upEnd, IntersectFlags.Map);

                // Only snap upward if nothing is above (we’re not inside a garage etc.)
                if (!upcast.DidHit && downcast.DidHit && downcast.HitPosition.DistanceTo(newSpawnPoint) <= 15f)
                {
                    newSpawnPoint = downcast.HitPosition;
                    pointInvalid = false;
                }

            }

            return newSpawnPoint;

        }

        // SpawnPed -- Spawns a ped based on the team, with a given loadout.
        public Ped SpawnPed(Team team, bool isSquadLeader)
        {
            if (team.Models.Length == 0)
                return null;


            int pedValue = 0;
            int baseAccuracy = 15;
            int tier;

            bool hasAnyWeapons = team.Tier1Weapons.Length > 0 || team.Tier2Weapons.Length > 0 || team.Tier3Weapons.Length > 0;

            // Tier 4 peds only spawn when there is no tier 4 living tier 4 ped of that team AND when the player is on an opposing team
            bool playerHasNoTeam = ModData.PlayerTeam == -1 || ModData.PlayerTeam == -2 || ModData.PlayerTeam < 0 || ModData.PlayerTeam >= ModData.Teams.Count || ModData.Teams[ModData.PlayerTeam] == null;

            bool shouldSpawnTier4 = (team.Tier4Ped == null || !team.Tier4Ped.Exists() || team.Tier4Ped.IsDead) && !playerHasNoTeam;

            int rnum = rand.Next(0, 100);

            rnum = (int)(rnum * team.TierUpgradeMultiplier);

            if (shouldSpawnTier4) tier = 4;
            else if (isSquadLeader || rnum >= 95) tier = 3;
            else if (rnum >= 60) tier = 2;
            else tier = 1;

            var model = new Model(team.Models[rand.Next(team.Models.Length)]);
            if (tier == 4 && !string.IsNullOrEmpty(team.Faction.Tier4Model)) model = new Model(team.Faction.Tier4Model);

            if (!model.IsValid || !model.IsInCdImage) return null;
            model.Request(500);
            if (!model.IsLoaded) return null;


            var ped = CreatePedInWorld(model, SpawnPos, isSquadLeader);

            if (ped == null) return null;

            ped.RelationshipGroup = team.Group;

            string weapon = "";

            if (hasAnyWeapons)
            {
                if (tier == 1 && team.Tier1Weapons.Length > 0)
                    weapon = Helpers.GetRandom(team.Tier1Weapons);
                else if (tier == 2 && team.Tier2Weapons.Length > 0)
                    weapon = Helpers.GetRandom(team.Tier2Weapons);
                else if (tier == 3 && team.Tier3Weapons.Length > 0)
                    weapon = Helpers.GetRandom(team.Tier3Weapons);
                else if (tier == 4 && team.Tier3Weapons.Length > 0)
                    weapon = Helpers.GetRandom(team.Tier3Weapons);
            }

            Blip blip = ped.AddBlip();

            if (tier == 1)
            {
                ped.Health = team.BaseHealth;
                ped.MaxHealth = team.BaseHealth;
                baseAccuracy = 7;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;
                pedValue = 40;
            }
            else if (tier == 2)
            {
                ped.Health = (int)(team.BaseHealth * 1.2f) ;
                ped.MaxHealth = (int)(team.BaseHealth * 1.2f);
                baseAccuracy = 15;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;
                pedValue = 100;
            }
            else if (tier == 3)
            {
                ped.Health = (int)(team.BaseHealth * 1.5f);
                ped.MaxHealth = (int)(team.BaseHealth * 1.5f);
                baseAccuracy = 30;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.6f;
                pedValue = 280;

                ped.CanSufferCriticalHits = false;


                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 60, true); // Throws smoke grenades
            }
            else if (tier == 4)
            {
                ped.Health = (int)(team.BaseHealth * 1.8f);
                ped.MaxHealth = (int)(team.BaseHealth * 1.8f);
                baseAccuracy = 75;

                blip.Sprite = BlipSprite.Juggernaut;
                pedValue = 675;

                team.Tier4Ped = ped;

                ped.Armor = 350;
                ped.CanWrithe = false;
                ped.IsFireProof = true;
                ped.IsInvincible = false;
                ped.CanSufferCriticalHits = false; // ped won't die if they get shot in the head (most will anyways)
            }
            else
            {
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.5f;
            }

            if (Personality == SquadPersonality.Aggressive)
            {
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 3);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 43, true); // Advance if can't find cover

            }
            else
            {
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2);
            }


            // Calculate an accuracy bonus
            ped.Accuracy = baseAccuracy + team.Accuracy; // adds the accuracy bonus

            if (!string.IsNullOrEmpty(weapon))
            {
                ped.Weapons.Give((WeaponHash)Game.GenerateHash(weapon), 999, true, true);
                if (SquadVehicle != null) ped.Weapons.Give(WeaponHash.Pistol, 999, false, true);

                // Force equip weapon
                Function.Call(Hash.SET_CURRENT_PED_WEAPON, ped, Game.GenerateHash(weapon), true);
            }



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
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);  // Use vehicles
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 2, true);  // Drive by shooting
            if (IsWeaponizedVehicle) Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 3, false);  // can leave vehicle in combat ONLY if its not a weaponized vehicle <-----
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);  // Can fight armed when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 12, true);  // Can blind fire
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 14, true);  // Can investigate gunshots/sounds
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 20, false);  // cant taunt
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 21, false); // Can chase target
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 22, true); // Can drag friends to safety
            //Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 44, true); // Switch to defensive when in cover
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 46, true); // Can fight armed peds when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 50, true); // Can charge
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 58, true); // Don't flee from combat
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 53, true); // Advance if no cover avaliable
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 42, true); // Can flank
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 87, true); // prefer ground targets

            if (Personality == SquadPersonality.Aggressive)
            {
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 28, true); // Advance if frustrated (can't see the enemy?)
            }

            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped, 77, true); // Disable threat broadcast
            Function.Call(Hash.SET_PED_CONFIG_FLAG, ped, 106, true); // Disable ragdoll from bullets




            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 125f);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped, 1); // medium
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, ped, 1);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped, 1); // 0 = near, 1 = medium, 2 = far

            Function.Call(Hash.SET_PED_LOD_MULTIPLIER, ped, 10.0f);
            Function.Call(Hash.SET_ENTITY_AS_MISSION_ENTITY, ped, true, true);
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, ped, true);

            Function.Call(Hash.SET_PED_PATH_MAY_ENTER_WATER, ped, true);
            Function.Call(Hash.SET_PED_PATH_PREFER_TO_AVOID_WATER, ped, true);

            // This can be used to fully customize the AI. Two options: Either completely rewrite AI, or use it for custom AI actions (e.g. pushing toward a target)
            //Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, ped, true);

            ped.DrivingStyle = DrivingStyle.Rushed;
            ped.VehicleDrivingFlags = VehicleDrivingFlags.UseShortCutLinks | VehicleDrivingFlags.AllowGoingWrongWay;

            PedTargetCache[ped] = (null, 0);
            PedAssignments[ped] = PedAssignment.None;

            squadValue += pedValue;
            return ped;
        }

    }



}