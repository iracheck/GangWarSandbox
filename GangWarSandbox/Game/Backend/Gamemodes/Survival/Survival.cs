using GTA;
using GangWarSandbox.Gamemodes;
using GangWarSandbox.Peds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemonUI.Menus;
using GTA.Native;
using GTA.Math;

namespace GangWarSandbox.Gamemodes
{
    internal class SurvivalGamemode : Gamemode
    {
        public Team enemyTeam1;
        public Team enemyTeam2;
        public Team enemyTeam3;

        double PlayerScore = 0;

        double TimeStart;
        double TimeElapsed;

        // Gamemode States
        int CurrentThreatLevel = 0; // Current level of the survival gamemode, used for difficulty scaling

        // Each threat level, and how it scales for the player.
        // Each numerical index represents a value
        // index 0 => max number of squads
        // index 1 => max number of vehicles
        // index 2 => max number of weaponized vehicles
        // index 3 => max number of helicopters
        // index 4 => max faction tier that can spawn
        // index 5 => threat "weight" (described in a comment below) to reach this point --> tl;dr a combination of multiple factors to determine how progressed the gamemode is

        // Note that the max number of squads is a global value. This means that you could have 15 vehicle squads, 5 weaponized vehicles squads, but only 15 max squads, and it will be a mixture of those two types.
        // Any left over slots will be filled with infantry squads, as the default squad type.
        List<int[]> ThreatLevelSettings = new List<int[]>
        {
            // max squads(total) (0) - vehicles (1) - weaponized vehicles (2) - helicopters (3) - max faction tier[1-3] (4) - threat weight (5)
            new int[] { 2, 0, 0, 0, 1, 0 }, // 1
            new int[] { 3, 1, 0, 0, 1, 50 }, // 2
            new int[] { 4, 2, 0, 0, 1, 200 }, // 3
            new int[] { 5, 2, 0, 0, 1, 500 }, // 4
            new int[] { 6, 2, 0, 0, 1, 850 }, // 5
            new int[] { 7, 2, 0, 0, 2, 1200 }, // 6
            new int[] { 10, 3, 1, 0, 2, 1700 }, // 7
            new int[] { 10, 3, 1, 1, 2, 2400 }, // 8
            new int[] { 12, 4, 1, 1, 3, 3600 }, // 9
            new int[] { 12, 4, 1, 1, 3, 4500 }, // 10
            new int[] { 12, 4, 1, 2, 3, 5600 }, // 11
            new int[] { 12, 4, 1, 2, 3, 7000 }, // 12
            new int[] { 14, 4, 1, 2, 3, 9000 }, // 13
            new int[] { 14, 5, 1, 2, 3, 12000 }, // 14
            new int[] { 14, 5, 2, 2, 3, 22000 }, // 15
        };

        int Combo = 1;
        int ComboLastTime = 0;

        public SurvivalGamemode() : base("Survival", "Survive as long as possible. Kill enemies to earn points, and try to achieve the highest score you can! Just like trying to get five stars.", 0)
        {
            SpawnMethod = GamemodeSpawnMethod.Random;

            EnableParameter_AllowWeaponizedVehicles = GamemodeBool.True;
            EnableParameter_AllowVehicles = GamemodeBool.True;
            EnableParameter_AllowHelicopters = GamemodeBool.True;

            EnableParameter_FogOfWar = GamemodeBool.False;
            FogOfWar = true;

            EnableParameter_CapturePoints = GamemodeBool.False;
            EnableParameter_Spawnpoints = GamemodeBool.False;
        }

        // GAMEMODE PROVIDED OVERRIDDEN METHODS BEGIN HERE

        public override void OnStart()
        {
            TimeStart = Game.GameTime;
            
            PlayerScore = 0;
            Combo = 1;
            ComboLastTime = 0;

            CurrentThreatLevel = 0;

            InitializeUI();

            SetRelationships();
        }

        public override NativeMenu ConstructGamemodeMenu()
        {
            Mod = GangWarSandbox.Instance;

            if (Mod == null) return null;

            NativeMenu gamemodeMenu = new NativeMenu("Survival Settings", "Survival Settings", "Modify the settings of your Survival gamemode, such as the factions hunting you.");
            BattleSetupUI.MenuPool.Add(gamemodeMenu);

            var level1Enemy = new NativeListItem<string>($"Tier 1 Hunter Faction", Mod.Factions.Keys.ToArray());
            level1Enemy.Description = "The primary team that appears to hunt you. These will always appear.";

            level1Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level1Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[1], selectedFaction);
                }
            };
            level1Enemy.SelectedItem = Mod.Teams[1].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();

            var level2Enemy = new NativeListItem<string>($"Tier 2 Hunter Faction", Mod.Factions.Keys.ToArray());
            level2Enemy.Description = "The second team that appears to hunt you. These will only appear starting in later rounds.";

            level2Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level2Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[2], selectedFaction);
                }
            };
            level2Enemy.SelectedItem = Mod.Teams[1].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();

            var level3Enemy = new NativeListItem<string>($"Tier 3 Hunter Faction", Mod.Factions.Keys.ToArray());
            level3Enemy.Description = "The last team that appears to hunt you. These will only appear after you have survived for a long time.";

            level3Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level3Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[3], selectedFaction);
                }
            };
            level3Enemy.SelectedItem = Mod.Teams[1].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();


            //var missions = new NativeCheckboxItem("Missions", "Missions are a set of objectives that can be completed to earn extra points, or weapons/ammo/vehicles.", false);
            //missions.Enabled = false; // Missions are not implemented yet

            gamemodeMenu.Add(level1Enemy);
            gamemodeMenu.Add(level2Enemy);
            gamemodeMenu.Add(level3Enemy);
            //gamemodeMenu.Add(missions);
            return gamemodeMenu;
        } 

        public override void OnTickGameRunning()
        {
            TimeElapsed = Game.GameTime - TimeStart;
            UpdateThreatLevel();
        }

        public override void OnPedKilled(Ped ped, Team teamOfPed)
        {
            Entity killer = ped.Killer;

            if (killer != Game.Player.Character) return;

            // Increase combo if the player has killed another ped within 2 seconds
            if (ComboLastTime > Game.GameTime - 2000)
            {
                Combo++;
            }
            else Combo = 1;

            PlayerScore += 0.2 * ped.MaxHealth * Math.Pow(Combo, 0.25) * Math.Pow(CurrentThreatLevel + 1, 0.1);
            Logger.Log("Current Score: " + PlayerScore.ToString());

            ComboLastTime = Game.GameTime;
        }

        public override void TerminateGamemode()
        {
            Mod.ClearAllPoints();
            base.TerminateGamemode();
        }

        public override bool CanStartBattle()
        {
            return true;
        }

        public override bool ShouldGetNewTarget(Squad s)
        {
            if (s.Waypoints.Count == 0) return true;

            if (s.Waypoints.Last().DistanceTo(Game.Player.Character.Position) > 7.5f) return true;
            else return false;
        }

        public override Vector3 GetTarget(Squad s)
        {
            return Game.Player.Character.Position;
        }

        public override bool ShouldSpawnSquad(Team team, int squadSize)
        {
            if (Mod.Teams.IndexOf(team) == 0) return false; // team index 0 "bodyguards" not implemented
            // team index 1 ("tier 1 enemy") can always spawn
            if (Mod.Teams.IndexOf(team) == 2 && ThreatLevelSettings[CurrentThreatLevel][4] < 2) return false; // team index 2 ("tier 2 enemy") can only spawn if the threat level is at least 2
            if (Mod.Teams.IndexOf(team) == 3 && ThreatLevelSettings[CurrentThreatLevel][4] < 3) return false; // team index 3 ("tier 3 enemy") can only spawn if the threat level is at least 3

            if (team.Squads.Count >= ThreatLevelSettings[CurrentThreatLevel][0])
            {
                return false;
            }
            else return true;
        }

        public override bool ShouldSpawnVehicleSquad(Team team)
        {
            if (team.VehicleSquads.Count >= ThreatLevelSettings[CurrentThreatLevel][1])
            {
                return false; 
            }
            else return true; 
        }

        public override bool ShouldSpawnWeaponizedVehicleSquad(Team team)
        {
            if (team.WeaponizedVehicleSquads.Count >= ThreatLevelSettings[CurrentThreatLevel][2])
            {
                return false; 
            }
            else return true; 
        }

        public override bool ShouldSpawnHelicopterSquad(Team team)
        {
            if (team.HelicopterSquads.Count >= ThreatLevelSettings[CurrentThreatLevel][3])
            {
                return false;
            }
            else return true;
        }


        // ALL NON-OVERRIDEN METHODS BEGIN HERE

        public override void SetRelationships()
        {
            // Assign team relationships
            foreach (var team in Mod.Teams)
            {
                if (team.TeamIndex == 0)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Companion, team.Group, Game.Player.Character.RelationshipGroup);
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Companion, Game.Player.Character.RelationshipGroup, team.Group);
                    continue;
                }

                if (team.TeamIndex != 1)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Companion, team.Group, Mod.Teams[1].Group);
                    team.AlliedIndexes.Add(1);
                }
                if (team.TeamIndex != 2)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Companion, team.Group, Mod.Teams[2].Group);
                    team.AlliedIndexes.Add(2);
                }
                if (team.TeamIndex != 3)
                {
                    Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Companion, team.Group, Mod.Teams[3].Group);
                    team.AlliedIndexes.Add(3);
                }

                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Hate, team.Group, Game.Player.Character.RelationshipGroup);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, (int)Relationship.Hate, Game.Player.Character.RelationshipGroup, team.Group);


            }
        }

        public bool UpdateThreatLevel()
        {
            // Threat level uses two factors:
            // - Time (1s:1pt)
            // - Player Score (1pt:0.2pt)
            // This combined score is used to determine the current threat level, which is then used to scale the difficulty of the gamemode.

            double threatWeight = (TimeElapsed / 1000) + (PlayerScore * 0.2);

            if (CurrentThreatLevel < ThreatLevelSettings.Count - 1 && threatWeight > ThreatLevelSettings[CurrentThreatLevel + 1][5])
            {
                CurrentThreatLevel++;
                Logger.Log("Increased threat level: " + CurrentThreatLevel + " (weight: " + threatWeight + ")");
                return true;
            }

            return false;
        }

    }
}
