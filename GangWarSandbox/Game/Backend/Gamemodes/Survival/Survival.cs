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
        int PlayerScore;

        double TimeStart;
        double TimeElapsed;

        // Gamemode States
        int CurrentThreatLevel; // Current level of the survival gamemode, used for difficulty scaling

        // Each threat level, and how it scales for the player.
        // Each numerical index represents a value
        // index 0 => max number of squads
        // index 1 => max number of vehicles
        // index 2 => max number of weaponized vehicles
        // index 3 => max number of helicopters
        // index 4 => max faction tier that can spawn
        // index 5 => threat "weight" (described in a comment below) to reach this point --> tl;dr a combination of multiple factors to determine how progressed the gamemode is

        // Note that the max number of squads is a global value. This means that you could have 15 vehicle squads, 5 weaponized vehicles squads, but only 15 max squads, and 
        // it will be a mixture of those two types, but not more than 15 total squads.
        // Any left over slots will be filled with infantry squads, as the default squad type.
        List<int[]> ThreatLevelSettings = new List<int[]>
        {
            // max squads(total) (0) - vehicles (1) - weaponized vehicles (2) - helicopters (3) - max faction tier[1-3] (4) - threat weight (5)
            new int[] { 2, 2, 0, 0, 1, 0 }, // 1
            new int[] { 3, 2, 0, 0, 1, 400 }, // 2
            new int[] { 4, 3, 0, 0, 1, 900 }, // 3
            new int[] { 5, 3, 0, 0, 1, 1600 }, // 4
            new int[] { 5, 4, 0, 1, 1, 2600 }, // 5
            new int[] { 5, 4, 0, 1, 2, 3700 }, // 6
            new int[] { 6, 4, 0, 1, 2, 4800 }, // 7
            new int[] { 6, 4, 1, 1, 2, 6000 }, // 8
            new int[] { 7, 5, 1, 1, 2, 7800 }, // 9
            new int[] { 7, 5, 1, 1, 3, 9800 }, // 10
            new int[] { 8, 5, 1, 2, 3, 11000 }, // 11
            new int[] { 8, 4, 1, 2, 3, 13500 }, // 12
            new int[] { 9, 4, 1, 2, 3, 17000 }, // 13
            new int[] { 9, 5, 1, 2, 3, 22000 }, // 14
            new int[] { 10, 5, 2, 2, 3, 30000 }, // 15
            new int[] { 14, 6, 3, 3, 3, 45000 }, // GURANTEED DEATH
        };

        int Combo;
        int ComboLastTime;

        public SurvivalGamemode() : base("Survival", "Survive as long as possible. Kill enemies to earn points, and try to achieve the highest score you can! Just like trying to get five stars.\n\nBETA GAMEMODE", 0)
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
            Combo = 0;
            ComboLastTime = 0;

            CurrentThreatLevel = 0;

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

            level1Enemy.SelectedItem = Mod.Teams[1].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();
            level1Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level1Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[1], selectedFaction);
                }
            };

            var level2Enemy = new NativeListItem<string>($"Tier 2 Hunter Faction", Mod.Factions.Keys.ToArray());
            level2Enemy.Description = "The second team that appears to hunt you. These will only appear starting in later rounds.";

            level2Enemy.SelectedItem = Mod.Teams[2].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();
            level2Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level2Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[2], selectedFaction);
                }
            };

            var level3Enemy = new NativeListItem<string>($"Tier 3 Hunter Faction", Mod.Factions.Keys.ToArray());
            level3Enemy.Description = "The last team that appears to hunt you. These will only appear after you have survived for a long time.";

            level3Enemy.SelectedItem = Mod.Teams[3].Faction?.Name ?? Mod.Factions.Keys.FirstOrDefault();
            level3Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level3Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[3], selectedFaction);
                }
            };

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
            UpdateGameUI();

            KillFarAwaySquads();

            if (ComboLastTime < Game.GameTime - 7000)
            {
                Combo = 0;
                ComboLastTime = Game.GameTime;
            }
        }

        public override void OnPedKilled(Ped ped, Team teamOfPed)
        {
            float multiplier = 0.5f;
            Entity killer = ped.Killer;

            if (killer != Game.Player.Character) multiplier *= 0.75f;

            // Increase combo if the player has killed another ped within 7 seconds
            if (ComboLastTime > Game.GameTime - 7000)
            {
                Combo++;
            }

            // Get 50% of the max health of the ped, scaled by the current threat level and how deep the combo is
            PlayerScore += (int) (multiplier * ped.MaxHealth * Math.Pow(Combo, 0.25) * Math.Pow(CurrentThreatLevel + 1, 0.1));
            Logger.Log("Current Score: " + PlayerScore.ToString() + "| Combo: " + Combo.ToString());

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

        public override bool AIOverride(Squad squad, Ped ped)
        {
            Ped player = Game.Player.Character;
            var assignments = squad.PedAssignments;

            if (!ped.IsInVehicle() && (ped.Position.DistanceTo(player.Position) < 50f || (!ped.IsInCombatAgainst(player) && PedAI.HasLineOfSight(ped, player)) && assignments[ped] != Squad.PedAssignment.GamemodeReserved1))
            {
                ped.Task.FightAgainst(player);
                assignments[ped] = Squad.PedAssignment.GamemodeReserved1; // attack player on foot
                return true;
            }
            else if (assignments[ped] == Squad.PedAssignment.GamemodeReserved1)
            {
                if (ped.Position.DistanceTo(player.Position) > 60f) return false;
                else return true;
            }

            if (player.IsInVehicle() && squad.SquadVehicle != null && squad.IsSquadInsideVehicle())
            {
                if (ped == squad.SquadLeader)
                {
                    ped.Task.VehicleChase(player);
                    assignments[ped] = Squad.PedAssignment.GamemodeReserved2; // chase player with/in vehicle
                }
                else
                {
                    ped.Task.VehicleShootAtPed(player);
                    assignments[ped] = Squad.PedAssignment.GamemodeReserved2; // chase player with/in vehicle
                }

                return true;
            }
            else if (!player.IsInVehicle() && ped.IsInVehicle() && ped.Position.DistanceTo(player.Position) < 40f)
            {
                ped.Task.LeaveVehicle();
                assignments[ped] = Squad.PedAssignment.ExitVehicle;
                return true;
            }
            else if (assignments[ped] == Squad.PedAssignment.GamemodeReserved2)
            {
                if (!player.IsInVehicle() || player.Position.DistanceTo(ped.Position) > 100f) return false;
                else return true;
            }


            return false;
        }

        public override bool ShouldSpawnSquad(Team team, int squadSize)
        {
            if (Mod.Teams.IndexOf(team) == 0) return false; // team index 0 "bodyguards" not implemented
            // team index 1 ("tier 1 enemy") can always spawn
            if (Mod.Teams.IndexOf(team) == 2 && ThreatLevelSettings[CurrentThreatLevel][4] < 2) return false; // team index 2 ("tier 2 enemy") can only spawn if the threat level is at least 2
            if (Mod.Teams.IndexOf(team) == 3 && ThreatLevelSettings[CurrentThreatLevel][4] < 3) return false; // team index 3 ("tier 3 enemy") can only spawn if the threat level is at least 3

            int allSquadsCount = team.Squads.Count + team.VehicleSquads.Count + team.WeaponizedVehicleSquads.Count + team.HelicopterSquads.Count;
            if (allSquadsCount >= ThreatLevelSettings[CurrentThreatLevel][0])
            {
                return false;
            }
            else return true;
        }

        public override bool ShouldSpawnVehicleSquad(Team team)
        {
            // If the player is in a vehicle, always spawn a vehicle squad. NOTE: This will *try* to spawn weaponized vehicle & helicopter squads first.
            if (Game.Player.Character.IsInVehicle()) return true;

            int maxForTeam = ThreatLevelSettings[CurrentThreatLevel][1] / ThreatLevelSettings[CurrentThreatLevel][4];
            if (team.VehicleSquads.Count >= maxForTeam)
            {
                return false; 
            }
            else return true; 
        }

        public override bool ShouldSpawnWeaponizedVehicleSquad(Team team)
        {
            int maxForTeam = ThreatLevelSettings[CurrentThreatLevel][2] / ThreatLevelSettings[CurrentThreatLevel][4];
            if (team.WeaponizedVehicleSquads.Count >= maxForTeam)
            {
                return false; 
            }
            else return true; 
        }

        public override bool ShouldSpawnHelicopterSquad(Team team)
        {
            int maxForTeam = ThreatLevelSettings[CurrentThreatLevel][3] / ThreatLevelSettings[CurrentThreatLevel][4];
            if (team.HelicopterSquads.Count >= maxForTeam)
            {
                return false;
            }
            else return true;
        }

        public override void OnSquadSpawn(Squad squad)
        {
            foreach (var ped in squad.Members)
            {
                ped.Health = 100 + (CurrentThreatLevel * 5);
                ped.Accuracy = (int) (ped.Accuracy * 0.5);

                if (ped.AttachedBlip != null) ped.AttachedBlip.Color = BlipColor.Red;
            }
            if (squad.SquadVehicle != null && squad.SquadVehicle.AttachedBlip != null) squad.SquadVehicle.AttachedBlip.Color = BlipColor.Red;
        }

        public override void OnVehicleSpawn(Vehicle vehicle)
        {
            vehicle.EnginePowerMultiplier = 1.5f;   // Acceleration boost
            vehicle.MaxSpeed = 180f;                // Cap speed (m/s)
            vehicle.EngineTorqueMultiplier = 1.5f;  // Extra pulling power
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
                Logger.LogDebug("Increased threat level: " + CurrentThreatLevel + " (weight: " + threatWeight + ")");
                return true;
            }

            return false;
        }

        public void UpdateGameUI()
        {
            if (BattleSetupUI.MenuPool.AreAnyVisible) return;

            float initialX = 5f;
            float initialY = 5f;
            float lineSpacing = 15f; // distance between columns

            string[] text =
            {
                $"Threat Level: {CurrentThreatLevel + 1}",
                $"Score: {PlayerScore}",
                $"Combo: {Combo}",
                $"Time Survived: {FormatTimeSurvived()}"
            };

            for (int i = 0; i < text.Length; i++)
            {
                string data = text[i];

                float x = initialX; // temp
                float y = initialY + (i * lineSpacing);
                System.Drawing.Color color = System.Drawing.Color.White;
                if (i == 0) color = System.Drawing.Color.Coral;

                new GTA.UI.TextElement($"{data}", new System.Drawing.PointF(x, y), 0.35f, color, GTA.UI.Font.ChaletLondon).Draw();
            }
        }

        public string FormatTimeSurvived()
        {
            int minutes = (int)(TimeElapsed / 60000);
            int seconds = (int)((TimeElapsed % 60000) / 1000);

            return $"{minutes:D2}:{seconds:D2}";
        }

        public void KillFarAwaySquads()
        {
            foreach (var team in Mod.Teams)
            {
                foreach (var squad in team.Squads)
                {
                    if (squad.SquadLeader.Position.DistanceTo(Game.Player.Character.Position) > 300f) squad.Destroy();
                }

                foreach (var squad in team.VehicleSquads)
                {
                    if (squad.SquadLeader.Position.DistanceTo(Game.Player.Character.Position) > 400f) squad.Destroy();
                }

                foreach (var squad in team.WeaponizedVehicleSquads)
                {
                    if (squad.SquadLeader.Position.DistanceTo(Game.Player.Character.Position) > 500f) squad.Destroy();
                }

                foreach (var squad in team.HelicopterSquads)
                {
                    if (squad.SquadLeader.Position.DistanceTo(Game.Player.Character.Position) > 600f) squad.Destroy();
                }
            }
        }

    }
}
