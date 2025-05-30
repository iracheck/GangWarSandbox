
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

namespace GangWarSandbox
{
    public class Squad
    {
        Random rand = new Random();
        PedAI pedAI = new PedAI();

        GangWarSandbox ModData = GangWarSandbox.Instance;

        // Squad Logic begins here

        public Ped SquadLeader;
        public List<Ped> Members = new List<Ped>();
        public Vector3 CurrentTarget = Vector3.Zero;

        public Team Owner;

        public int squadValue; // lower value squads may be assigned to less important tasks
        public float squadAttackRange = 60f;

        
        public Vector3 SpawnPos;


        public Dictionary<Ped, PedAssignment> PedAssignments = new Dictionary<Ped, PedAssignment>();
        Dictionary<Ped, (Ped enemy, int timestamp)> PedTargetCache;

        // Squad Stuck Timer-- if the squad leader is stuck for too long, it will try to move again
        private float SquadLeaderStuckTime = 0f;
        private const float StuckThreshold = 2.0f;

        public SquadRole Role;
        public SquadType Type;
        public SquadPersonality AggroFactor;

        CapturePoint TargetPoint; // the location that the squad's role will be applied to-- variable

        Vehicle SquadVehicle;

        // Squad roles will remain somewhat static-- the AI overseer of each faction will prioritize squads of a certain role to 
        public enum SquadRole
        {
            Idle = 0,

            DefendCapturePoint = 1,
            AssaultCapturePoint = 2,
            ReinforceAllies = 3,
            SeekAndDestroy = 4,
            Retreat = 5,

            //AirSupport = 31,
            //AirDrop = 32,


        }

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

        public enum PedAssignment
        {
            None = 0,
            AttackNearby = 1,
            RunToPosition = 2,
            DefendArea = 3,
            FollowSquadLeader = 4,

            Retreat = 99, // run back to spawn
            Flee = 100, // flee from the battlefield, not just retreating
        }

        // Personality -- how a squad reacts to certain situations, gives a dynamic feel to the battlefield
        public enum SquadPersonality
        {
            Normal = 0, // the squad will not act in any particular way. the majority of squads
        }


        public Squad(Team owner, SquadRole role, SquadType type = 0)
        {
            Owner = owner;
            Role = role;

            SpawnPos = Owner.SpawnPoints[rand.Next(Owner.SpawnPoints.Count)];

            if (type == 0)
            {
                int randNum = rand.Next(0, 101); // Randomly choose a type if none is specified

                if (randNum > 95)
                {
                    Type = SquadType.Garrison;
                }
                else
                {
                    Type = SquadType.Infantry;
                }
            }

            PedTargetCache = new Dictionary<Ped, (Ped enemy, int timestamp)>();
            SpawnSquadPeds(GetSquadSizeByType(Type));

            // Spawn squadmates
            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];
                ped.AlwaysKeepTask = true;
            }

            CurrentTarget = FindRandomEnemySpawnpoint(Owner); // set the current target to a random enemy spawnpoint
        }

        private int GetSquadSizeByType(SquadType type)
        {
            if (type == SquadType.Garrison)
            {
                return 7;
            }
            return Owner.GetSquadSize();
        }

        // When the squad is spawned, spawn its peds 
        private void SpawnSquadPeds(int num)
        {
            SquadLeader = SpawnPed(Owner, true);
            Members.Add(SquadLeader);

            for (int i = 0; i < num - 1; i++) {
                Ped ped = SpawnPed(Owner, false);

                if (ped != null)
                {
                    Members.Add(ped);
                }

            }
        }

        public bool SquadAIHandler()
        {
            if (isEmpty())
            {
                Destroy();
                return false;
            }

            if (SquadLeader == null || SquadLeader.IsDead || !SquadLeader.Exists())
                PromoteLeader();

            Vector3 leaderPosition = SquadLeader.Position;

            //int GameTime = Game.GameTime;

            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];

                if (ped == null || !ped.Exists() || !ped.IsAlive) continue;

                Ped nearbyEnemy = null;

                nearbyEnemy = FindNearbyEnemy(ped, Owner, squadAttackRange); // find a nearby enemy within the squad attack range

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

                        PedAI.RunTo(ped, nearbyEnemy.Position);
                        PedAssignments[ped] = PedAssignment.RunToPosition;
                    }

                    continue;
                }
                else if (nearbyEnemy == null && PedAssignments[ped] == PedAssignment.AttackNearby)
                {
                    PedAssignments[ped] = PedAssignment.None;
                }

                if (ped.IsInCombat || PedAssignments[ped] == PedAssignment.AttackNearby) continue;

                // If assigned to defend an area, defend it!
                if (Role == SquadRole.DefendCapturePoint && CurrentTarget != Vector3.Zero)
                {
                    if (ped.Position.DistanceTo(CurrentTarget) < 5f) // too close!
                    {
                        Vector3 offset = GenerateRandomOffset();
                        Vector3 target = CurrentTarget + offset; // move to a random position around the target

                        ped.Task.GoTo(target, -1);
                    }
                    else if (!ped.IsInCover && !ped.IsGoingIntoCover)
                    {
                        PedAI.SeekCover(ped);
                    }
                }
                else if (PedAssignments[ped] == PedAssignment.DefendArea)
                {
                    PedAssignments[ped] = PedAssignment.None;
                }



                if (ped == SquadLeader)
                {
                    if (PedAssignments[ped] != PedAssignment.RunToPosition && CurrentTarget != Vector3.Zero) // if the squad has a target, but the squad leader is not moving toward it, move!
                    {
                        PedAI.RunTo(ped, CurrentTarget);
                        PedAssignments[ped] = PedAssignment.RunToPosition;
                    }
                    else if (PedAssignments[ped] == PedAssignment.RunToPosition && (ped.IsIdle || ped.Velocity.Length() < 0.2f) && CurrentTarget != Vector3.Zero)
                    {
                        SquadLeaderStuckTime += Game.LastFrameTime;

                        if (SquadLeaderStuckTime >= StuckThreshold) // if the squad leader has been stuck for too long, try to move again
                        {
                            SquadLeaderStuckTime = 0f;
                            PedAI.RunTo(ped, CurrentTarget);
                        }
                    }
                    else if (PedAssignments[ped] == PedAssignment.RunToPosition && (Vector3.Distance(ped.Position, CurrentTarget) < 5f || CurrentTarget == Vector3.Zero))
                    {
                        PedAssignments[ped] = PedAssignment.None;
                    }

                }
                else // Squad members
                {
                    // Follow the squad leader around
                    if (PedAssignments[ped] != PedAssignment.FollowSquadLeader && Vector3.Distance(ped.Position, leaderPosition) > 5f)
                    {
                        ped.Task.FollowToOffsetFromEntity(SquadLeader, GenerateRandomOffset(), 2f);
                        PedAssignments[ped] = PedAssignment.FollowSquadLeader;
                    }
                }

            }

            return true;
        }

        private Vector3 GenerateRandomOffset()
        {
            float offsetX = rand.Next(-5, 6);
            float offsetY = rand.Next(-5, 6);
            return new Vector3(offsetX, offsetY, 0);
        }

        private Vector3 FindRandomEnemySpawnpoint(Team team)
        {
            List<Team> teams = ModData.Teams;

            for (int i = 0; i < teams.Count; i++)
            {
                if (teams[i] == team)
                {
                    continue;
                }
                else
                {
                    return teams[i].SpawnPoints[rand.Next(teams[i].SpawnPoints.Count)];
                }
            }
            return new Vector3(0, 0, 0);
        }

        private Ped FindNearbyEnemy(Ped self, Team team, float distance, bool infiniteSearch = false)
        {
            Ped foundEnemy;

            if (PedTargetCache[self].enemy == null || PedTargetCache[self].timestamp > Game.GameTime + 1000)
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

        public List<Ped> CleanupDead()
        {
            List<Ped> deadPeds = new List<Ped>();

            // iterate from the end, for safety
            for (int i = Members.Count - 1; i >= 0; i--)
            {
                if (Members[i].IsDead)
                {
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

            Members.Clear(); // clear list after cleanup

            if (Owner.Squads.Contains(this))  Owner.Squads.Remove(this);
        }

        public bool isEmpty()
        {
            if (Members.Count <= 0) return true;
            else return false;
        }

        // SpawnPed -- Spawns a ped based on the team, with a given loadout.
        public Ped SpawnPed(Team team, bool isSquadLeader)
        {
            int pedValue = 0;

            if (team.SpawnPoints.Count == 0 || team.Models.Length == 0 || team.Tier1Weapons.Length == 0 || team.Tier2Weapons.Length == 0 || team.Tier3Weapons.Length == 0)
                return null;

            bool shouldSpawnTier4 = team.Tier4Ped == null || !team.Tier4Ped.Exists() || team.Tier4Ped.IsDead;

            int tier = 1;
            int rnum = rand.Next(0, 100);

            rnum = (int)(rnum * team.TierUpgradeMultiplier);

            if (rnum <= 10 || isSquadLeader) tier = 3;
            if (rnum <= 40) tier = 2;
            else tier = 1;


            if (shouldSpawnTier4)
            {
                tier = 4;
                team.Tier4Ped = null;
            }

            var model = new Model(team.Models[rand.Next(team.Models.Length)]);
            if (tier == 4 && !string.IsNullOrEmpty(team.Faction.Tier4Model)) model = new Model(team.Faction.Tier4Model);

            if (!model.IsValid || !model.IsInCdImage) return null;
            model.Request(500);
            if (!model.IsLoaded) return null;


            var ped = World.CreatePed(model, SpawnPos);
            ped.RelationshipGroup = team.Group;

            String weapon = "";

            Blip blip = ped.AddBlip();

            if (tier == 1)
            {
                weapon = Helpers.GetRandom(team.Tier1Weapons);
                ped.Health = team.BaseHealth;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;
                pedValue = 40;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2);
            }
            else if (tier == 2)
            {
                weapon = Helpers.GetRandom(team.Tier2Weapons);
                ped.Health = team.BaseHealth + 100;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.5f;
                pedValue = 100;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2); // offensive
            }
            else if (tier == 3)
            {
                weapon = Helpers.GetRandom(team.Tier3Weapons);
                ped.Health = team.BaseHealth * 2;
                ped.Accuracy = team.Accuracy * 2;
                blip.Sprite = BlipSprite.Enemy2;
                blip.Scale = 0.6f;
                pedValue = 240;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2); // offensive
            }
            else if (tier == 4)
            {
                weapon = Helpers.GetRandom(team.Tier3Weapons);
                ped.Health = team.BaseHealth * 5;
                ped.Accuracy = team.Accuracy * 3;
                blip.Sprite = BlipSprite.Juggernaut;
                pedValue = 580;

                team.Tier4Ped = ped;

                ped.Armor = 100;
                ped.CanSufferCriticalHits = false;
                ped.IsFireProof = true;
                ped.IsInvincible = false;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 3); // suicidal

            }
            else
            {
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.5f;
                weapon = "WEAPON_PISTOL";
            }

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
            ped.Task.SwapWeapon();

            blip.IsShortRange = true;
            blip.Name = $"Team {team.Name}";
            blip.Color = team.BlipColor;

            Members.Add(ped);
            PedAssignments[ped] = PedAssignment.None;

            // Stuff to sort out an issue that will probably be here
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 0, true);  // Always fight
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);  // Can use cover
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);  // Can fight armed when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 50, true); // Disable fleeing

            Function.Call(Hash.SET_PED_SEEING_RANGE, ped, 100f);
            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped, 1); // medium
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, ped, 1);
            Function.Call(Hash.SET_PED_COMBAT_RANGE, ped, 2);             // 0 = near, 1 = medium, 2 = far

            // Fight against any nearby targets, at an even greater range than normal behavior

            PedTargetCache[ped] = (null, 0);
            PedAssignments[ped] = PedAssignment.None;

            squadValue += pedValue;
            return ped;
        }
    }
}
