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
            gamemodeItem.Enabled = Mod.IsBattleRunning == false;

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

            var gmMenu = gm.ConstructGamemodeMenu();

            if (gmMenu != null)
            {
                MainMenu.AddSubMenu(gmMenu);
            }

            if (gm.MaxTeams > 1)
            {
                MainMenu.AddSubMenu(CreateTeamSetupSubmenu(gm));
            }

            if (Gamemode.ShouldBeEnabled(gm.EnableParameter_Spawnpoints) || Gamemode.ShouldBeEnabled(gm.EnableParameter_CapturePoints))
            {
                MainMenu.AddSubMenu(CreatePointSetupMenu(gm));
            }

            if (true) // temporary condition
            {
                MainMenu.AddSubMenu(CreateBattleOptionsMenu(gm));
            }


            // End of Menu: BATTLE CONTROL
            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            start.Enabled = Mod.IsBattleRunning == false;
            stop.Enabled = Mod.IsBattleRunning == true;

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
                RebuildMenu();
                Mod.StopBattle();
            };

            MainMenu.Add(start);
            MainMenu.Add(stop);

            MenuPool.RefreshAll();
        }

        public static NativeMenu CreateTeamSetupSubmenu(Gamemode gm)
        {
            // Submenu: TEAM SETUP
            TeamSetupMenu = new NativeMenu("Team Setup", "Configure Teams");
            MenuPool.Add(TeamSetupMenu);

            // AI FACTIONS
            TeamFactionItems.Clear();
            for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
            {
                int teamIndex = i; // ← ✅ Capture the loop variable correctly

                var teamFactionItem = new NativeListItem<string>($"Team {teamIndex + 1} Faction", Mod.Factions.Keys.ToArray());
                teamFactionItem.Description = "The faction of team " + (teamIndex + 1) + ".";

                teamFactionItem.ItemChanged += (item, args) =>
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[teamIndex], teamFactionItem.SelectedItem);
                };

                TeamFactionItems.Add(teamFactionItem);
                TeamSetupMenu.Add(teamFactionItem);
            }

            // sets each team faction definition to a random value on mod startup, for quick plug n' play
            foreach (var teamFactionItem in TeamFactionItems)
            {
                teamFactionItem.SelectedIndex = rand.Next(0, Mod.Factions.Count);
            }

            // PLAYER ALLEGIANCE SETUP
            List<String> playerTeamItem_Teams = new List<String> { "Neutral", "Hates Everyone" };

            for (int i = 0; i < gm.MaxTeams; i++)
                playerTeamItem_Teams.Add("Team " + (i + 1));



            var playerTeamItem = new NativeListItem<string>("Player Team", playerTeamItem_Teams.ToArray());
            playerTeamItem.Description = "The team of the player character. If selecting 'Neutral' you will still be attacked if acting hostile toward the NPCs.";

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
            var allowVehicles = new NativeCheckboxItem("Vehicles", "Allow non-weaponized vehicles to be used in the battle.", true);
            var allowWeaponizedVehicles = new NativeCheckboxItem("Weaponized Vehicles", "[EXPERIMENTAL] Allow weaponized vehicles to be used in the battle.", false);
            var allowHelicopters = new NativeCheckboxItem("Helicopters", "[EXPERIMENTAL] Allow helicopters to be used in the battle.", false);

            unitCountMultiplier.ValueChanged += (item, args) =>
            {
                Mod.CurrentGamemode.UnitCountMultiplier = ((float)unitCountMultiplier.Value) / 10;
                unitCountMultiplier.Description = "Current Multiplier: " + Mod.CurrentGamemode.UnitCountMultiplier + "x";
            };

            allowVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnVehicles = allowVehicles.Checked; };
            allowWeaponizedVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnWeaponizedVehicles = allowWeaponizedVehicles.Checked; };
            allowHelicopters.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnHelicopters = allowHelicopters.Checked; };

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

            var addT1 = new NativeItem("Add Spawnpoint - Team 1");
            var addT2 = new NativeItem("Add Spawnpoint - Team 2");
            var addT3 = new NativeItem("Add Spawnpoint - Team 3");
            var addT4 = new NativeItem("Add Spawnpoint - Team 4");
            var addCapPt = new NativeItem("Add Capture Point");

            var clear = new NativeItem("Clear All Points");

            addT1.Activated += (item, args) => Mod.AddSpawnpoint(1);
            addT2.Activated += (item, args) => Mod.AddSpawnpoint(2);
            addT3.Activated += (item, args) => Mod.AddSpawnpoint(3);
            addT4.Activated += (item, args) => Mod.AddSpawnpoint(4);

            addCapPt.Activated += (item, args) => Mod.AddCapturePoint();

            clear.Activated += (item, args) => Mod.ClearAllPoints();

            addT1.Enabled = gm.MaxTeams > 0;
            addT2.Enabled = gm.MaxTeams > 1;
            addT3.Enabled = gm.MaxTeams > 2;
            addT4.Enabled = gm.MaxTeams > 3;

            SpawnpointMenu.Add(addT1);
            SpawnpointMenu.Add(addT2);
            SpawnpointMenu.Add(addT3);
            SpawnpointMenu.Add(addT4);
            SpawnpointMenu.Add(addCapPt);

            SpawnpointMenu.Add(clear);

            return SpawnpointMenu;
        }


    }
}
