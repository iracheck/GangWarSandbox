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
        private static NativeMenu BattleSetupMenu;
        private static NativeMenu TeamSetupMenu;
        private static NativeMenu SpawnpointMenu;
        private static NativeMenu BattleControlMenu;
        private static List<NativeListItem<string>> TeamFactionItems = new List<NativeListItem<string>>();

        // Used to access the main mod instance
        static GangWarSandbox Mod = GangWarSandbox.Instance;
        static Random rand = new Random();

        public static void Show()
        {
            MainMenu.Visible = true;
        }

        public static void Hide()
        {
            MenuPool.HideAll();
        }

        public static void SetupMenu()
        {
            // MAIN MENU
            MainMenu = new NativeMenu("Gang War Sandbox", "MAIN MENU");
            MenuPool.Add(MainMenu);

            // Submenu: BATTLE SETUP
            BattleSetupMenu = new NativeMenu("Battle Setup", "Configure Battle");
            MenuPool.Add(BattleSetupMenu);
            MainMenu.AddSubMenu(BattleSetupMenu);

            var gamemodeItem = new NativeListItem<string>("Gamemode", Mod.AvaliableGamemodes.Select(g => g.Name).ToArray());
            gamemodeItem.Description = "Select the gamemode for this battle. Each gamemode has different rules and mechanics that can be viewed on the mod page or readme file.";

            // A multiplier from the value located in the faction settings, max of 10x
            var unitCountMultiplier = new NativeSliderItem("Unit Count Multiplier", "Current Multiplier: 1.0x", 100, 10);

            var allowVehicles = new NativeCheckboxItem("Vehicles", "Allow non-weaponized vehicles to be used in the battle.");
            var allowWeaponizedVehicles = new NativeCheckboxItem("Weaponized Vehicles", "Allow weaponized vehicles to be used in the battle.");
            var allowHelicopters = new NativeCheckboxItem("Helicopters", "Allow helicopters to be used in the battle.");

            gamemodeItem.ItemChanged += (item, args) =>
            {
                var selectedGamemode = Mod.AvaliableGamemodes.FirstOrDefault(g => g.Name == gamemodeItem.SelectedItem);
                if (selectedGamemode != null)
                {
                    Mod.CurrentGamemode.TerminateGamemode();

                    Mod.CurrentGamemode = selectedGamemode;
                    Mod.CurrentGamemode.InitializeGamemode();

                    Gamemode gm = Mod.CurrentGamemode;

                    allowVehicles.Checked = gm.ShouldBeTicked(gm.EnableParameter_AllowVehicles);
                    allowWeaponizedVehicles.Checked = gm.ShouldBeTicked(gm.EnableParameter_AllowWeaponizedVehicles);
                    allowHelicopters.Checked = gm.ShouldBeTicked(gm.EnableParameter_AllowHelicopters);

                    allowVehicles.Enabled = gm.ShouldBeEnabled(gm.EnableParameter_AllowVehicles);
                    allowWeaponizedVehicles.Enabled = gm.ShouldBeEnabled(gm.EnableParameter_AllowWeaponizedVehicles);
                    allowHelicopters.Enabled = gm.ShouldBeEnabled(gm.EnableParameter_AllowHelicopters);
                }
            };

            unitCountMultiplier.ValueChanged += (item, args) =>
            {
                Mod.CurrentGamemode.UnitCountMultiplier = ((float)unitCountMultiplier.Value) / 10;
                unitCountMultiplier.Description = "Current Multiplier: " + Mod.CurrentGamemode.UnitCountMultiplier + "x";
            };

            allowVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnVehicles = allowVehicles.Checked;};
            allowVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnWeaponizedVehicles = allowWeaponizedVehicles.Checked; };
            allowVehicles.CheckboxChanged += (item, args) => { Mod.CurrentGamemode.SpawnHelicopters = allowHelicopters.Checked; };

            BattleSetupMenu.Add(gamemodeItem);
            BattleSetupMenu.Add(unitCountMultiplier);

            BattleSetupMenu.Add(allowVehicles);
            BattleSetupMenu.Add(allowWeaponizedVehicles);
            BattleSetupMenu.Add(allowHelicopters);


            // Submenu: TEAM SETUP
            TeamSetupMenu = new NativeMenu("Team Setup", "Configure Teams");
            MenuPool.Add(TeamSetupMenu);
            MainMenu.AddSubMenu(TeamSetupMenu);

            List<String> playerTeamItem_Teams = new List<String> { "Neutral", "Hates Everyone" };

            for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
                playerTeamItem_Teams.Add("Team " + (i + 1));

            var playerTeamItem = new NativeListItem<string>("Player Team", playerTeamItem_Teams.ToArray());
            playerTeamItem.Description = "The team of the player character. If selecting 'Neutral' you will still be attacked if you are shooting in the area.";

            playerTeamItem.ItemChanged += (item, args) =>
            {
                var sel = playerTeamItem.SelectedItem;
                if (sel == "Neutral") Mod.PlayerTeam = -1;
                else if (sel == "Hates Everyone") Mod.PlayerTeam = -2;
                else if (sel.StartsWith("Team ")) Mod.PlayerTeam = int.Parse(sel.Substring(5)) - 1;
                else Mod.PlayerTeam = -1;
            };

            TeamFactionItems.Clear();
            for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
            {
                var teamFactionItem = new NativeListItem<string>($"Team {i + 1} Faction", Mod.Factions.Keys.ToArray());
                teamFactionItem.Description = "The faction of team " + (i + 1) + ". This will determine the models, weapons, vehicles, and other attributes of the team.";
                TeamFactionItems.Add(teamFactionItem);
                TeamSetupMenu.Add(teamFactionItem);
            }

            // sets each team faction definition to a random value, for quick plug n' play
            foreach (var teamFactionItem in TeamFactionItems)
            {
                teamFactionItem.SelectedIndex = rand.Next(0, TeamFactionItems.Count);
            }

            TeamSetupMenu.Add(playerTeamItem);

            // Submenu: POINT SETUP
            SpawnpointMenu = new NativeMenu("Map Markers", "Manage Map Markers");
            MenuPool.Add(SpawnpointMenu);
            MainMenu.AddSubMenu(SpawnpointMenu);

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



            SpawnpointMenu.Add(addT1);
            SpawnpointMenu.Add(addT2);
            SpawnpointMenu.Add(addT3);
            SpawnpointMenu.Add(addT4);
            SpawnpointMenu.Add(addCapPt);

            SpawnpointMenu.Add(clear);

            // Submenu: BATTLE CONTROL
            BattleControlMenu = new NativeMenu("Battle Control", "Start or Stop Battle");
            MenuPool.Add(BattleControlMenu);
            MainMenu.AddSubMenu(BattleControlMenu);

            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            start.Activated += (item, args) =>
            {
                for (int i = 0; i < GangWarSandbox.NUM_TEAMS; i++)
                    Mod.ApplyFactionToTeam(Mod.Teams[i], TeamFactionItems[i].SelectedItem);
                Mod.StartBattle();
            };
            stop.Activated += (item, args) => Mod.StopBattle();

            BattleControlMenu.Add(start);
            BattleControlMenu.Add(stop);
        }
    }
}
