
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

namespace GangWarSandbox
{
    public class Squad
    {
        Random rand = new Random();
        GangWarSandbox ModData = GangWarSandbox.Instance;

        // Squad Logic begins here

        public Ped SquadLeader;
        public List<Ped> Members = new List<Ped>();
        public Dictionary<Ped, Vector3> currentTarget = new Dictionary<Ped, Vector3>();
        public Team Owner;

        public int squadValue; // lower value squads may be assigned to less important tasks
        public Vector3 SpawnPos;

        public SquadRole Role;
        public SquadAggroFactor AggroFactor;

        CapturePoint TargetPoint; // the location that the squad's role will be applied to-- variable

        // Squad roles will remain somewhat static-- the AI overseer of each faction will prioritize squads of a certain role to 
        public enum SquadRole
        {
            Idle = 0, // logically should not be used, but is there as a fallback :)
            AttackDestroy = 1,
            AttackCapture = 2,
            Defend = 3,
            Patrol = 4,
        }


        // Aggro Factor-- how willing a squad is to push blindly to its death, or be fearful and retreat
        public enum SquadAggroFactor
        {
            Normal = 0, // the squad will always listen to the commander AI-- normal behavior
            Suicidal = 1, // the squad will never retreat, and may ignore commander orders and attack anyways
            LastStand = 2, // when faced in danger, the squad will stand its ground (unless ordered to retreat)
            Scared = 3, // the squad will be easier to break, and may flee the battlefield when it loses a lot of members
            Fearless = 4, // the squad will refuse an order to retreat
        }


        public Squad(Team owner, SquadRole role)
        {
            Owner = owner;
            Role = role;
            SpawnPos = Owner.SpawnPoints[rand.Next(Owner.SpawnPoints.Count)];

            SpawnSquadPeds(owner.GetSquadSize());

            // Spawn squadmates
            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];
            }
        }

        // When the squad is spawned, spawn its peds :)
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

        // Makes the squad run away and flee
        private void SquadRunAway()
        {
            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];
                if (ped == null || !ped.Exists() || !ped.IsAlive) continue;

                // Run away from the current target
                if (currentTarget.ContainsKey(ped))
                {
                    Vector3 fleePosition = ped.Position + (ped.Position - currentTarget[ped]).Normalized * 50f;
                    ped.Task.RunTo(fleePosition);
                }
                else
                {
                    // If no target, just run away in a random direction
                    Vector3 fleePosition = ped.Position + new Vector3(rand.Next(-50, 50), rand.Next(-50, 50), 0);
                    ped.Task.RunTo(fleePosition);
                }

                Members.Remove(ped);
            }
        }

        public bool SquadAIHandler()
        {
            if (isEmpty()) Destroy(); // destroy the squad if its empty
            if (SquadLeader.IsDead || !SquadLeader.Exists()) PromoteLeader(); // ensures a leader always exists

            Vector3 leaderPosition = SquadLeader.Position;

            for (int i = 0; i < Members.Count; i++)
            {
                Ped ped = Members[i];
                if (ped == null || !ped.IsAlive || ped.IsInCombat) continue;

                Ped nearbyEnemy = FindNearbyEnemy(ped, Owner);

                // Squad Leader Logic
                if (ped == SquadLeader)
                {
                    if (nearbyEnemy != null) // enemy found nearby-- fight them
                    {
                        GTA.UI.Screen.ShowSubtitle($"Squad found an enemy.");
                        ped.Task.FightAgainst(nearbyEnemy);
                    }
                    if (!currentTarget.ContainsKey(ped) || ped.Position.DistanceTo(currentTarget[ped]) < 8f)
                    {
                        currentTarget[ped] = FindRandomEnemySpawnpoint(Owner);
                        ped.Task.GoTo(currentTarget[ped]);
                    }
                    else if (ped.Velocity.LengthSquared() < 0.1f) // standing still? probably not intended
                    {
                        ped.Task.GoTo(currentTarget[ped]); // only reassign if stuck
                    }

                    continue;
                }

                // Squad Member Logic
                if (nearbyEnemy != null)
                {
                    ped.Task.FightAgainst(nearbyEnemy);
                }
                else
                {
                    float distance = ped.Position.DistanceTo(leaderPosition);
                    if (distance >= 7f)
                    {
                        ped.Task.FollowToOffsetFromEntity(SquadLeader, GenerateRandomOffset(), 2.5f);
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


        private Ped FindNearbyEnemy(Ped self, Team team)
        {
            const float searchRadius = 80f;

            // Get all enemy squads from other teams
            var enemySquads = ModData.Teams
                .Where(t => t != team)
                .SelectMany(t => t.Squads);

            // Search all enemy squad members
            return enemySquads
                .SelectMany(s => s.Members)
                .Where(p => p != null && p.Exists() && !p.IsDead && p.Position.DistanceTo(self.Position) <= searchRadius)
                .OrderBy(p => p.Position.DistanceTo(self.Position))
                .FirstOrDefault();
        }


        public void PromoteLeader()
        {
            foreach (var ped in Members)
            {
                if (ped.Exists() && !ped.IsDead)
                {
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

        public void Destroy()
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

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 1); // defensive
            }
            else if (tier == 2)
            {
                weapon = Helpers.GetRandom(team.Tier2Weapons);
                ped.Health = team.BaseHealth + 100;
                ped.CanSufferCriticalHits = false;
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
                ped.CanSufferCriticalHits = false;
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
                blip.Scale = 0.8f;
                pedValue = 570;

                team.Tier4Ped = ped;

                ped.Armor = 100;
                ped.CanSufferCriticalHits = false;
                ped.IsFireProof = true;
                ped.IsInvincible = false; // Still killable

                ped.Task.FightAgainstHatedTargets(200f);
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

            ped.AlwaysKeepTask = true;

            blip.IsShortRange = true;
            blip.Name = $"Team {team.Name}";
            blip.Color = team.BlipColor;

            Members.Add(ped);

            // Stuff to sort out an issue that will probably be here
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 0, true);  // Always fight
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);  // Can use cover
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);  // Can fight armed when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 50, true); // Disable fleeing

            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped, 1); // medium
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, ped, 1);

            // Fight against any nearby targets, at an even greater range than normal behavior
            ped.Task.FightAgainstHatedTargets(100f);

            squadValue += pedValue;
            return ped;
        }
    }
}
