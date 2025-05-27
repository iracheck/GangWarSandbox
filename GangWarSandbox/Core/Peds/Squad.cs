
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

        // Squad Logic begins here

        Ped SquadLeader;
        List<Ped> Members = new List<Ped>();
        Team Owner;

        SquadRole Role; // Attack, Defend, Patrol, Idle
        SquadAggroFactor AggroFactor;

        CapturePoint TargetPoint; // the location that the squad's role will be applied to-- variable

        // Squad roles will remain somewhat static-- the AI overseer of each faction will be hesitant to change them unless they're struggling
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

            SpawnSquadPeds(owner.GetSquadSize());

            // Initialize basic squad leadership logic
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

        public void SquadAIHandler()
        {
            if (isEmpty()) return;

            Vector3 leaderPosition = SquadLeader.Position;
        }

        public void PromoteLeader()
        {
            SquadLeader = Members[0];
            Members.RemoveAt(0);
        }

        public bool isEmpty()
        {
            if (Members.Count <= 0) return true;
            else return false;
        }


        // SpawnPed -- Spawns a ped based on the team, with a given loadout.
        public Ped SpawnPed(Team team, bool isSquadLeader)
        {
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

            var pos = team.SpawnPoints[rand.Next(team.SpawnPoints.Count)];
            var model = new Model(team.Models[rand.Next(team.Models.Length)]);

            if (!model.IsValid || !model.IsInCdImage) return null;
            model.Request(500);
            if (!model.IsLoaded) return null;

            var ped = World.CreatePed(model, pos);
            ped.RelationshipGroup = team.Group;

            String weapon = "";

            Blip blip = ped.AddBlip();

            if (tier == 1)
            {
                weapon = Helpers.GetRandom(team.Tier1Weapons);
                ped.Health = team.BaseHealth;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.4f;

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 1); // defensive
            }
            else if (tier == 2)
            {
                weapon = Helpers.GetRandom(team.Tier2Weapons);
                ped.Health = team.BaseHealth + 100;
                ped.CanSufferCriticalHits = false;
                blip.Sprite = BlipSprite.Enemy;
                blip.Scale = 0.5f;

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

                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, 2); // offensive
            }
            else if (tier == 4)
            {
                weapon = Helpers.GetRandom(team.Tier3Weapons);
                ped.Health = team.BaseHealth * 5;
                ped.Accuracy = team.Accuracy * 3;
                blip.Sprite = BlipSprite.Juggernaut;
                blip.Scale = 0.8f;

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

            GTA.UI.Screen.ShowSubtitle($"Spawned with weapon: {weapon}");

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

            team.Peds.Add(ped);

            // Stuff to sort out an issue that will probably be here
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 0, true);  // Always fight
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 1, true);  // Can use cover
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 5, true);  // Can fight armed when unarmed
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 50, true); // Disable fleeing

            Function.Call(Hash.SET_PED_COMBAT_ABILITY, ped, 1); // medium
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, ped, 1);

            // Fight against any nearby targets, at an even greater range than normal behavior
            ped.Task.FightAgainstHatedTargets(100f);

            return ped;
        }
    }
}
