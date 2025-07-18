using GangWarSandbox.Core;
using GangWarSandbox.Gamemodes;
using LemonUI;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GangWarSandbox
{
    static class BattleSetupUI
    {
        // LemonUI Menus
        public static ObjectPool MenuPool = new ObjectPool();

        private static NativeMenu MainMenu;
        private static NativeMenu TeamSetupMenu;
        private static NativeMenu SpawnpointMenu;
        private static NativeMenu BattleOptionsMenu;
        private static NativeMenu GamemodeMenu;
        private static List<NativeListItem<string>> TeamFactionItems = new List<NativeListItem<string>>();

        private static string[] SavedFactions = new string[GangWarSandbox.NUM_TEAMS];

        // Used to access the main mod instance
        static GangWarSandbox Mod = GangWarSandbox.Instance;
        static Random rand = new Random();

        public static void Show()
        {
            RebuildMenu();
            MainMenu.Visible = true;
        }

        public static void Hide()
        {
            MenuPool.HideAll();
        }

        // MENU V2
        public static void SetupMenu()
        {
            // MAIN MENU
            MainMenu = new NativeMenu("Gang War Sandbox", "MAIN MENU");
            MenuPool.Add(MainMenu);
            MainMenu.Description = "Welcome to Gang War Sandbox! This menu allows you to set up and manage battles between factions. Use the options below to configure your battle settings.";

            // GAMEMODE SELECTOR
            var gamemodeItem = new NativeListItem<string>("Gamemode", Mod.AvaliableGamemodes.Select(g => g.Name).ToArray());
            gamemodeItem.Description = "Select the gamemode for this battle. Each gamemode has different rules and mechanics that can be viewed on the mod page or readme file.";
            gamemodeItem.Enabled = !Mod.IsBattleRunning;

            gamemodeItem.ItemChanged += (item, args) =>
            {
                var selectedGamemode = Mod.AvaliableGamemodes.FirstOrDefault(g => g.Name == gamemodeItem.SelectedItem);
                if (selectedGamemode != null)
                {
                    Mod.CurrentGamemode.TerminateGamemode();

                    Mod.CurrentGamemode = selectedGamemode;
                    Mod.CurrentGamemode.InitializeGamemode();

                    RebuildMenu();
                }
            };

            MainMenu.Add(gamemodeItem);

            RebuildMenu();
        }

        public static void RebuildMenu()
        {
            while (MainMenu.Items.Count > 1)
            {
                MainMenu.Remove(MainMenu.Items.Last());
            }

            var gm = Mod.CurrentGamemode;

            MainMenu.ElementAt(0).Enabled = !Mod.IsBattleRunning;

            var gmMenu = gm.ConstructGamemodeMenu();

            if (gmMenu != null)
            {
                var menu = MainMenu.AddSubMenu(gmMenu);
                menu.Enabled = !Mod.IsBattleRunning;
            }

            if (gm.MaxTeams > 1)
            {
                var menu = MainMenu.AddSubMenu(CreateTeamSetupSubmenu(gm));
                menu.Enabled = !Mod.IsBattleRunning;
            }

            if (Gamemode.ShouldBeEnabled(gm.EnableParameter_Spawnpoints) || Gamemode.ShouldBeEnabled(gm.EnableParameter_CapturePoints))
            {
                var menu = MainMenu.AddSubMenu(CreatePointSetupMenu(gm));
                menu.Enabled = !Mod.IsBattleRunning;
            }

            if (true) // temporary condition
            {
                var menu = MainMenu.AddSubMenu(CreateBattleOptionsMenu(gm));
                menu.Enabled = !Mod.IsBattleRunning;
            }


            // End of Menu: BATTLE CONTROL
            var start = new NativeItem("Start Battle", "Start the battle with the specified settings.");

            var stop = new NativeItem("Stop Battle", "Stop the battle and automatically clean up all peds and vehicles.");

            var reload = new NativeItem("Reload Config", "Reload all configuration files. Note that some of your chosen settings will be lost.");

            start.Enabled = Mod.IsBattleRunning == false;
            stop.Enabled = Mod.IsBattleRunning == true;
            reload.Enabled = Mod.IsBattleRunning == false;

            start.Activated += (item, args) =>
            {
                for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
                    Mod.ApplyFactionToTeam(Mod.Teams[i], TeamFactionItems[i].SelectedItem);

                Mod.StartBattle();
                RebuildMenu();
                MenuPool.HideAll(); // Hide the menu after starting the battle
            };
            stop.Activated += (item, args) =>
            {
                Mod.StopBattle();
                RebuildMenu();
            };

            reload.Activated += (item, args) =>
            {
                Mod.StopBattle(); // just in case
                ConfigParser.ReloadAll();

                RebuildMenu();
            };

            MainMenu.Add(start);
            MainMenu.Add(stop);
            MainMenu.Add(reload);

            MenuPool.RefreshAll();
        }

        public static NativeMenu CreateTeamSetupSubmenu(Gamemode gm)
        {
            TeamSetupMenu = new NativeMenu("Team Setup", "Configure Teams");
            MenuPool.Add(TeamSetupMenu);

            TeamFactionItems.Clear();
            for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
            {
                int teamIndex = i;
                var factionNames = Mod.Factions.Keys.ToArray();
                var teamFactionItem = new NativeListItem<string>($"Team {teamIndex + 1} Faction", factionNames);
                teamFactionItem.Description = "The faction of team " + (teamIndex + 1) + ".";

                // Use saved faction name if available
                string savedFaction = SavedFactions[teamIndex];
                if (!string.IsNullOrEmpty(savedFaction) && Mod.Factions.ContainsKey(savedFaction))
                {
                    teamFactionItem.SelectedItem = savedFaction;
                }
                else
                {
                    teamFactionItem.SelectedIndex = rand.Next(0, factionNames.Length);
                    SavedFactions[teamIndex] = teamFactionItem.SelectedItem; // Save random selection
                }


                // Apply on change
                teamFactionItem.ItemChanged += (item, args) =>
                {
                    string selected = teamFactionItem.SelectedItem;
                    SavedFactions[teamIndex] = selected;
                    Mod.ApplyFactionToTeam(Mod.Teams[teamIndex], selected);
                };

                TeamFactionItems.Add(teamFactionItem);
                TeamSetupMenu.Add(teamFactionItem);
            }


            // PLAYER TEAM SETUP
            List<string> playerTeamOptions = new List<string>() { "Neutral", "Hates Everyone" };
            for (int i = 0; i < gm.MaxTeams; i++)
                playerTeamOptions.Add("Team " + (i + 1));

            var playerTeamItem = new NativeListItem<string>("Player Team", playerTeamOptions.ToArray());
            playerTeamItem.Description = "The team of the player character.";

            playerTeamItem.SelectedIndex = LoadPlayerTeamChoice();

            playerTeamItem.ItemChanged += (item, args) =>
            {
                var sel = playerTeamItem.SelectedItem;

                if (sel == "Neutral") Mod.PlayerTeam = -1;
                else if (sel == "Hates Everyone") Mod.PlayerTeam = -2;
                else if (sel.StartsWith("Team ")) Mod.PlayerTeam = int.Parse(sel.Substring(5)) - 1;
                else Mod.PlayerTeam = -1;
            };

            TeamSetupMenu.Add(playerTeamItem);


            return TeamSetupMenu;
        }

        public static NativeMenu CreateBattleOptionsMenu(Gamemode gm)
        {
            BattleOptionsMenu = new NativeMenu("Battle Options", "Configure Battle Options");
            MenuPool.Add(BattleOptionsMenu);

            // A multiplier from the value located in the faction settings, max of 10x
            var unitCountMultiplier = new NativeSliderItem("Unit Count Multiplier", "Current Multiplier: 1.0x", 100, 10);

            // Values letting the user decide if they want to allow vehicles, weaponized vehicles, and helicopters in the battle
            var allowVehicles = new NativeCheckboxItem("Vehicles", "Allow non-weaponized vehicles to be used in the battle.", gm.SpawnVehicles);
            var allowWeaponizedVehicles = new NativeCheckboxItem("Weaponized Vehicles", "[EXPERIMENTAL] Allow weaponized vehicles to be used in the battle.\n\n" +
                "Due to early access, Weaponized Vehicles has not been fully completed. Specifically, peds in the weapon will get out. Do not expect fluid results.", gm.SpawnWeaponizedVehicles);
            var allowHelicopters = new NativeCheckboxItem("Helicopters", "[EXPERIMENTAL] Allow helicopters to be used in the battle.\n\n" +
                "Due to early access, their behavior has not been fully completed. Helicopters harm the flow of the battle, so do not expect fluid results.", gm.SpawnHelicopters);

            unitCountMultiplier.ValueChanged += (item, args) =>
            {
                Mod.CurrentGamemode.UnitCountMultiplier = ((float)unitCountMultiplier.Value) / 10;
                unitCountMultiplier.Description = "Current Multiplier: " + Mod.CurrentGamemode.UnitCountMultiplier + "x";
            };

            allowVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnVehicles = allowVehicles.Checked; };
            allowWeaponizedVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnWeaponizedVehicles = allowWeaponizedVehicles.Checked; };
            allowHelicopters.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnHelicopters = allowHelicopters.Checked; };

            allowVehicles.Enabled = Gamemode.ShouldBeEnabled(gm.EnableParameter_AllowVehicles);
            allowWeaponizedVehicles.Enabled = Gamemode.ShouldBeEnabled(gm.EnableParameter_AllowWeaponizedVehicles);
            allowHelicopters.Enabled = Gamemode.ShouldBeEnabled(gm.EnableParameter_AllowHelicopters);

            BattleOptionsMenu.Add(unitCountMultiplier);

            BattleOptionsMenu.Add(allowVehicles);
            BattleOptionsMenu.Add(allowWeaponizedVehicles);
            BattleOptionsMenu.Add(allowHelicopters);

            return BattleOptionsMenu;
        }

        public static NativeMenu CreatePointSetupMenu(Gamemode gm)
        {
            // Submenu: POINT SETUP
            SpawnpointMenu = new NativeMenu("Map Markers", "Manage Map Markers");
            MenuPool.Add(SpawnpointMenu);


            for (int i = 0; i < gm.MaxTeams; i++)
            {
                int index = i;
                var addSpawnpoint = new NativeItem($"Add Spawnpoint - Team {i + 1}", $"Adds a spawnpoint for team {i + 1} at your current location, or at your waypoint if you have one.");
                addSpawnpoint.Activated += (item, args) => Mod.AddSpawnpoint(index);
                SpawnpointMenu.Add(addSpawnpoint);
            }

            var addCapPt = new NativeItem("Add Capture Point", "Adds a capture point at your current location, or at your waypoint if you have one.");

            var clear = new NativeItem("Clear All Points", "Clears all spawnpoints on the map. There is no undo button for this action.");

            addCapPt.Activated += (item, args) => Mod.AddCapturePoint();

            clear.Activated += (item, args) => Mod.ClearAllPoints();

            SpawnpointMenu.Add(addCapPt);

            SpawnpointMenu.Add(clear);

            return SpawnpointMenu;
        }


        public static int LoadPlayerTeamChoice()
        {
            if (Mod.PlayerTeam == -2) return 1;
            if (Mod.PlayerTeam == -1) return 0; // Neutral
            else
            {
                return Mod.PlayerTeam + 2;
            }
        }

    }
}
