// GTA Gang War Sandbox - LemonUI Version
// Requirements:
// - ScriptHookVDotNet v3
// - LemonUI.SHVDN3

using GTA;
using GTA.Native;
using GTA.Math;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LemonUI;
using LemonUI.Menus;
using GangWarSandbox;
using GangWarSandbox.Peds;
using GangWarSandbox.Gamemodes;
using System.Runtime.InteropServices;
using GangWarSandbox.Core;
using System.Runtime.Remoting.Messaging;

namespace GangWarSandbox
{
    public class GangWarSandbox : Script
    {
        public static GangWarSandbox Instance { get; private set; }

        private readonly Random rand = new Random();

        // Debug Utilities
        public int DEBUG = 1;
        private Blip DEBUG_active_ai_zone_blip;

        // Constants
        private const int AI_UPDATE_FREQUENCY = 250; // How often squad AI will be updated, in milliseconds
        private const int VEHICLE_AI_UPDATE_FREQUENCY = 100; // How often squad AI will be updated, in milliseconds
        private const int POINT_UPDATE_FREQUENCY = 1000; // How often capture points will be updated, in milliseconds
        private const int MAX_CORPSES = 25; // Maximum number of corpses to keep in memory
        public const int NUM_TEAMS = 4; // How many teams? In the future, it will be loaded from a settings file, but for now it's constant to keep stability
        private const int TIME_BETWEEN_SQUAD_SPAWNS = 3000; // Time in milliseconds between squad spawns for each team

        // Teams
        public int PlayerTeam = -1;
        public List<Team> Teams = new List<Team>();
        public Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        public Dictionary<Team, float> LastSquadSpawnTime = new Dictionary<Team, float>(); // Track last spawn time for each team to prevent spamming or crowding
        public List<BlipSprite> BlipSprites = new List<BlipSprite>
        {
            BlipSprite.Number1,
            BlipSprite.Number2,
            BlipSprite.Number3,
            BlipSprite.Number4,
            BlipSprite.Number5,
            BlipSprite.Number6
        };


        // Tracked Peds
        public List<Ped> DeadPeds = new List<Ped>();
        public List<Vehicle> SquadlessVehicles = new List<Vehicle>();

        // Capture Points
        public List<CapturePoint> CapturePoints = new List<CapturePoint>();

        // LemonUI Menus
        private ObjectPool MenuPool = new ObjectPool();

        private NativeMenu MainMenu;
        private NativeMenu BattleSetupMenu;
        private NativeMenu TeamSetupMenu;
        private NativeMenu SpawnpointMenu;
        private NativeMenu BattleControlMenu;
        private List<NativeListItem<string>> TeamFactionItems = new List<NativeListItem<string>>();

        // Game State
        private bool IsBattleRunning = false;

        // Game Options
        // Options relating to the battle, e.g. unit counts or vehicles
        public float UnitCountMultiplier = 1; // Multiplier for unit count, used to scale the number of soldiers per team based on faction settings

        public bool UseVehicles = false;
        public bool UseWeaponizedVehicles = false;
        public bool UseHelicopters = false;

        public Gamemode CurrentGamemode = new InfiniteBattleGamemode();
        private List<Gamemode> AvaliableGamemodes = new List<Gamemode>
        {
            new InfiniteBattleGamemode(),
            new SkirmishGamemode(),
            // Add more gamemodes here as needed
            // Future expansion: allow users to make their own gamemodes in a dll?
        }; 

        // Player Info
        Ped Player = Game.Player.Character;
        bool PlayerDied = false;
        int TimeOfDeath;

        static int StartingMoney;


        public GangWarSandbox()
        {
            Instance = this;

            // Ensure valid directories exist on startup
            ModFiles.EnsureDirectoriesExist();

            // Try to load the configuration files
            Factions = ConfigParser.LoadFactions();
            ConfigParser.LoadConfiguration();

            Tick += OnTick;
            KeyDown += OnKeyDown;

            MenuPool = new ObjectPool();

            for (int i = 0; i < NUM_TEAMS; i++)
            {
                Teams.Add(new Team((i + 1).ToString())); // Initialize teams with default names and groups
                LastSquadSpawnTime[Teams[i]] = 0; // Initialize last spawn time for each team

                Teams[i].BlipSprite = BlipSprites[i]; // Assign a unique blip sprite for each team (for spawnpoints)
            }



            SetupMenu();
        }


        // Parts of this was written for debugging and so I will need to rewrite it eventually
        private void SetupMenu()
        {
            // MAIN MENU
            MainMenu = new NativeMenu("Gang War Sandbox", "MAIN MENU");
            MenuPool.Add(MainMenu);

            // Submenu: BATTLE SETUP
            BattleSetupMenu = new NativeMenu("Battle Setup", "Configure Battle");
            MenuPool.Add(BattleSetupMenu);
            MainMenu.AddSubMenu(BattleSetupMenu);

            var gamemodeItem = new NativeListItem<string>("Gamemode", AvaliableGamemodes.Select(g => g.Name).ToArray());
            gamemodeItem.Description = "Select the gamemode for this battle. Each gamemode has different rules and mechanics that can be viewed on the mod page or readme file.";

            gamemodeItem.ItemChanged += (item, args) =>
            {
                var selectedGamemode = AvaliableGamemodes.FirstOrDefault(g => g.Name == gamemodeItem.SelectedItem);
                if (selectedGamemode != null)
                {
                    CurrentGamemode.TerminateGamemode();

                    CurrentGamemode = selectedGamemode;
                    CurrentGamemode.InitializeGamemode();
                }
            };

            // A multiplier from the value located in the faction settings, max of 10x
            var unitCountMultiplier = new NativeSliderItem("Unit Count Multiplier", "Current Multiplier: 1.0x", 100, 10);

            unitCountMultiplier.ValueChanged += (item, args) =>
            {
                UnitCountMultiplier = ((float) unitCountMultiplier.Value) / 10;
                unitCountMultiplier.Description = "Current Multiplier: " + UnitCountMultiplier + "x";
            };

            var allowVehicles = new NativeCheckboxItem("Vehicles", "Allow non-weaponized vehicles to be used in the battle.");
            var allowWeaponizedVehicles = new NativeCheckboxItem("Weaponized Vehicles", "Allow weaponized vehicles to be used in the battle.");
            var allowHelicopters = new NativeCheckboxItem("Helicopters", "Allow helicopters to be used in the battle. ");

            allowVehicles.Checked = true;
            allowWeaponizedVehicles.Checked = true;
            allowHelicopters.Checked = true;

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

            for (int i = 0; i < NUM_TEAMS; i++)
                playerTeamItem_Teams.Add("Team " + (i + 1));

            var playerTeamItem = new NativeListItem<string>("Player Team", playerTeamItem_Teams.ToArray());
            playerTeamItem.Description = "The team of the player character. If selecting 'Neutral' you will still be attacked if you are shooting in the area.";

            playerTeamItem.ItemChanged += (item, args) =>
            {
                var sel = playerTeamItem.SelectedItem;
                if (sel == "Neutral") PlayerTeam = -1;
                else if (sel == "Hates Everyone") PlayerTeam = -2;
                else if (sel.StartsWith("Team ")) PlayerTeam = int.Parse(sel.Substring(5)) - 1;
                else PlayerTeam = -1;
            };

            TeamFactionItems.Clear();
            for (int i = 0; i < NUM_TEAMS; i++)
            {
                var teamFactionItem = new NativeListItem<string>($"Team {i + 1} Faction", Factions.Keys.ToArray());
                teamFactionItem.Description = "The faction of team " + (i + 1) + ". This will determine the models, weapons, vehicles, and other attributes of the team.";
                TeamFactionItems.Add(teamFactionItem);
                TeamSetupMenu.Add(teamFactionItem);
            }

            // sets each team faction definition to a random value, for quick plug n' play
            foreach (var  teamFactionItem in TeamFactionItems)
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

            var clearNearest = new NativeItem("Clear Nearest Point");
            var clear = new NativeItem("Clear All Points");

            addT1.Activated += (item, args) => AddSpawnpoint(1);
            addT2.Activated += (item, args) => AddSpawnpoint(2);
            addT3.Activated += (item, args) => AddSpawnpoint(3);
            addT4.Activated += (item, args) => AddSpawnpoint(4);

            addCapPt.Activated += (item, args) => AddCapturePoint();

            clearNearest.Activated += (item, args) => ClearClosestPoint();
            clear.Activated += (item, args) => ClearAllPoints();



            SpawnpointMenu.Add(addT1);
            SpawnpointMenu.Add(addT2);
            SpawnpointMenu.Add(addT3);
            SpawnpointMenu.Add(addT4);
            SpawnpointMenu.Add(addCapPt);

            SpawnpointMenu.Add(clearNearest);
            SpawnpointMenu.Add(clear);

            // Submenu: BATTLE CONTROL
            BattleControlMenu = new NativeMenu("Battle Control", "Start or Stop Battle");
            MenuPool.Add(BattleControlMenu);
            MainMenu.AddSubMenu(BattleControlMenu);

            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            start.Activated += (item, args) =>
            {
                for (int i = 0; i < NUM_TEAMS; i++)
                    ApplyFactionToTeam(Teams[i], TeamFactionItems[i].SelectedItem);
                StartBattle();
            };
            stop.Activated += (item, args) => StopBattle();

            BattleControlMenu.Add(start);
            BattleControlMenu.Add(stop);
        }

        private void ApplyFactionToTeam(Team team, string factionName)
        {
            if (Factions.TryGetValue(factionName, out var faction))
            {
                team.Models = faction.Models;
                team.Faction = faction;
                team.Tier1Weapons = faction.Tier1Weapons;
                team.Tier2Weapons = faction.Tier2Weapons;
                team.Tier3Weapons = faction.Tier3Weapons;
                team.MAX_SOLDIERS = faction.MaxSoldiers;
                team.BaseHealth = faction.BaseHealth;
                team.Accuracy = faction.Accuracy;
                team.TierUpgradeMultiplier = faction.TierUpgradeMultiplier;
                team.BlipColor = faction.Color;
                team.TeamIndex = Teams.IndexOf(team);
                
                team.TeamVehicles = faction.VehicleSet;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            MenuPool.Process();

            CurrentGamemode.OnTick();

            DrawMarkers();

            int GameTime = Game.GameTime;

            if (IsBattleRunning)
            {
                if (PlayerTeam != -1 && PlayerTeam != -2)
                {
                    if (Player.IsDead)
                    {
                        PlayerDied = true;
                        TimeOfDeath = GameTime;
                    }

                    if (PlayerDied && TimeOfDeath + 5000 <= GameTime)
                    {
                        // Player has died and respawned after 5 seconds
                        Vector3 respawnLocation = Teams[PlayerTeam].SpawnPoints.Count > 0 ? Teams[PlayerTeam].SpawnPoints[0] : Vector3.Zero;

                        if (respawnLocation == Vector3.Zero) return;
                        Teams[PlayerTeam].Tier4Ped = Player; // Reset the Tier 4 Ped for the team

                        GTA.UI.Screen.FadeOut(2000);
                        Script.Wait(2000);

                        Player.Position = respawnLocation; // Move player to the spawn point

                        PlayerDied = false; // Reset death state

                        CurrentGamemode.OnPlayerDeath();

                        Script.Wait(500);
                        GTA.UI.Screen.FadeIn(500); // Fade in for 500ms
                    }
                }

                CurrentGamemode.OnTickGameRunning();

                SpawnSquads();

                // Collection error prevention
                var allSquads = Teams.SelectMany(t => t.GetAllSquads()).ToList();

                foreach (var squad in allSquads)
                {
                    // Ped AI
                    if ((squad.SquadVehicle != null && GameTime % VEHICLE_AI_UPDATE_FREQUENCY == 0) || (squad.SquadVehicle == null && GameTime % AI_UPDATE_FREQUENCY == 0) || squad.JustSpawned)
                    {
                        squad.SquadAIHandler();
                        CurrentGamemode.OnSquadUpdate(squad);
                    }

                    // Corpse Removal
                    List<Ped> deadPeds = squad.CleanupDead();

                    if (deadPeds == null) continue;

                    DeadPeds.AddRange(deadPeds);

                    while (DeadPeds.Count >= MAX_CORPSES)
                    {
                        if (DeadPeds[0] != null && DeadPeds[0].Exists())
                        {
                            DeadPeds[0].Delete();
                        }
                        DeadPeds.RemoveAt(0);
                    }
                }

                foreach (var point in CapturePoints)
                {
                    point.CapturePointHandler(); // Process capture points
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                if (!MenuPool.AreAnyVisible)
                    MainMenu.Visible = true;
                else
                    MenuPool.HideAll();
            }
        }

        private void StartBattle()
        {
            if (IsBattleRunning) return;

            IsBattleRunning = true;
            StartingMoney = Player.Money; // Save starting money!!

            Ped player = Game.Player.Character;

            ResetPlayerRelations();

            for (int i = 0; i < CapturePoints.Count; i++)
            {
                CapturePoints[i].BattleStart();
            }

            foreach (var team in Teams)
            {
                team.RecolorBlips();
            }

            CurrentGamemode.InitializeUI();

            CurrentGamemode.OnStart();

            // Spawn squads for each team
            SpawnSquads();

            DEBUG_active_ai_zone_blip = World.CreateBlip(Game.Player.Character.Position, 500f);
            DEBUG_active_ai_zone_blip.Color = BlipColor.Red;
            DEBUG_active_ai_zone_blip.Alpha = 60;

            Game.Player.WantedLevel = 0; // Reset wanted level
            Game.Player.DispatchsCops = false; // disable cop dispatches
        }

        private void StopBattle()
        {
            Player.Money = StartingMoney;

            IsBattleRunning = false;
            CurrentGamemode.OnEnd();

            GTA.UI.Screen.ShowSubtitle("Battle Ended!");
            CleanupAll();

            DEBUG_active_ai_zone_blip.Delete();


            Game.Player.DispatchsCops = true; // Re-enable cop dispatches
        }

        private void SpawnSquads()
        {

            foreach (var team in Teams)
            {
                if (!CurrentGamemode.ShouldSpawnSquad(team)) continue;

                // SAFETY CHECKS: Prevent crashes
                if (team.SpawnPoints.Count == 0 || team.Models.Length == 0)
                {
                    continue;
                }

                int numAlive = 0;

                for (int i = 0; i < team.Squads.Count; i++)
                {
                    numAlive += team.Squads[i].Members.Count;
                }

                int squadSize = team.GetSquadSize();

                if (squadSize <= 0) continue;

                if (Game.GameTime - LastSquadSpawnTime[team] >= TIME_BETWEEN_SQUAD_SPAWNS && numAlive + squadSize <= team.GetMaxNumPeds())
                {
                    LastSquadSpawnTime[team] = Game.GameTime;

                    Squad squad = null;

                    if (team.ShouldSpawnVehicle())
                    {
                        new Squad(team, 1);
                    }
                    else
                    {
                        new Squad(team, 0);
                    }
                }


            }
        }

        private void AddCapturePoint()
        {
            if (!IsBattleRunning)
            {
                CapturePoint point;
                Vector3 pos;

                if (Game.IsWaypointActive)
                {
                    pos = World.WaypointPosition;

                    GTA.UI.Screen.ShowSubtitle($"Capture point created at waypoint.");
                    World.RemoveWaypoint();
                }
                else
                {
                    pos = Game.Player.Character.Position;

                    GTA.UI.Screen.ShowSubtitle($"Capture point created at player location.");
                }

                if (pos == Vector3.Zero) return;

                pos.Z = World.GetGroundHeight(pos);
                point = new CapturePoint(pos);

                CapturePoints.Add(point);
            }
            else
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to create a new capture point.");
            }
        }


        private void AddSpawnpoint(int teamIndex)
        {
            if (!IsBattleRunning)
            {
                if (Game.IsWaypointActive)
                {
                    Vector3 waypointPos = World.WaypointPosition;
                    Teams[teamIndex - 1].AddSpawnpoint(waypointPos);

                    GTA.UI.Screen.ShowSubtitle($"Spawnpoint added for Team {teamIndex} at waypoint.");
                    World.RemoveWaypoint();

                    
                }
                else
                {
                    Vector3 charPos = Game.Player.Character.Position;
                    Teams[teamIndex - 1].AddSpawnpoint(charPos);
                    GTA.UI.Screen.ShowSubtitle($"Spawnpoint added for Team {teamIndex} at player location.");

                }
            }
            else
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to create a new spawnpoint.");
            }
        }

        private void ClearClosestPoint()
        {
            Dictionary<Object, Vector3> list = new Dictionary<Object, Vector3>();

            if (IsBattleRunning)
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to remove spawnpoints.");
                return;
            }

            foreach (var team in Teams)
            {
                foreach (var blip in team.Blips)
                {
                    if (blip.Exists()) list.Add(blip, blip.Position);
                }
            }

            foreach (var point in CapturePoints)
            {
                if (point.PointBlip.Exists()) list.Add(point, point.Position);
            }

            if (list.Count == 0)
            {
                GTA.UI.Screen.ShowSubtitle("No points to clear.");
                return;
            }

            // Find the closest point
            Vector3 playerPos = Game.Player.Character.Position;
            Object closestPoint = list.OrderBy(p => Vector3.Distance(playerPos, list[p])).FirstOrDefault().Key;

            if (closestPoint == null) return;

            if (closestPoint is Blip bpt)
            {
                bpt.Delete();
                foreach (var team in Teams)
                {
                    if (team.Blips.Contains(bpt))
                        team.Blips.Remove(bpt); // Remove from team's blips
                }
            }
            else if (closestPoint is CapturePoint cpt)
            {
                if (cpt.PointBlip.Exists()) cpt.PointBlip.Delete();
                CapturePoints.Remove(cpt);
            }
        }

        private void ClearAllPoints()
        {
            if (IsBattleRunning)
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to remove spawnpoints.");
                return;
            }

            foreach (var team in Teams)
            {
                foreach (var blip in team.Blips)
                {
                    if (blip.Exists()) blip.Delete();
                }
                team.Blips.Clear();
                team.SpawnPoints.Clear();
            }

            foreach (var point in CapturePoints)
            {
                if (point.PointBlip.Exists()) point.PointBlip.Delete();
            }
            CapturePoints.Clear();
        }

        private void CleanupAll()
        {
            foreach (var team in Teams)
            {
                // Make a copy of the list to avoid modifying it while iterating
                var squadsToRemove = team.Squads.ToList();

                squadsToRemove.AddRange(team.VehicleSquads.ToList());
                squadsToRemove.AddRange(team.WeaponizedVehicleSquads.ToList());
                squadsToRemove.AddRange(team.HelicopterSquads.ToList());


                foreach (var squad in squadsToRemove)
                {
                    try
                    {
                        squad.Destroy(); // This can safely remove it from team.Squads now

                        team.Squads.Remove(squad);
                        team.VehicleSquads.Remove(squad);
                        team.WeaponizedVehicleSquads.Remove(squad);
                        team.HelicopterSquads.Remove(squad);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error cleaning up squad: {ex.Message}"); // Log any errors during cleanup
                    }
                }

                team.Squads.Clear();
                team.VehicleSquads.Clear();
                team.WeaponizedVehicleSquads.Clear();
                team.HelicopterSquads.Clear();

                // Clean up squadless vehicles
                foreach (var vehicle in SquadlessVehicles)
                {
                    vehicle.AttachedBlip?.Delete(); // Remove blip if it exists

                    try
                    {
                        if (vehicle.Exists())
                            vehicle.Delete();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error cleaning up vehicle: {ex.Message}"); // Log any errors during cleanup
                    }
                }

                SquadlessVehicles.Clear();

                // Also clean up DeadPeds if needed
                foreach (var ped in DeadPeds.ToList())
                {
                    try
                    {
                        if (ped != null && ped.Exists())
                            ped.Delete();
                    }
                    catch (Exception ex)
                    {
                        GTA.UI.Screen.ShowSubtitle($"Dead ped cleanup error: {ex.Message}");
                    }
                }

                DeadPeds.Clear();
            }
        }

        private void ResetPlayerRelations()
        {
            // Assign player to team
            if (PlayerTeam < 0)
            {
                Game.Player.Character.RelationshipGroup = "PLAYER";
            }
            else
            {
                Game.Player.Character.RelationshipGroup = Teams[PlayerTeam].Group;
                Teams[PlayerTeam].Tier4Ped = Player; // Assign player to be their team's "strong npc"

                // move the player to the first spawn point of their team
                if (Teams[PlayerTeam].SpawnPoints.Count > 0)
                {
                    Player.Position = Teams[PlayerTeam].SpawnPoints[0];
                }
            }
        }


        /// <summary>
        /// Draws markers for capture points, and if debug mode is enabled, squad movement orders
        /// </summary>
        private void DrawMarkers()
        {
            foreach (var point in CapturePoints)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, point.Position, Vector3.Zero, Vector3.Zero, new Vector3(CapturePoint.Radius, CapturePoint.Radius, 1f), Color.White);
            }

            if (DEBUG_active_ai_zone_blip != null)
            {
                DEBUG_active_ai_zone_blip.Position = Game.Player.Character.Position;
            }


            if (DEBUG == 1 && Teams != null)
            {
                foreach (var team in Teams)
                {
                    if (team?.Squads == null) continue;

                    foreach (var squad in team.Squads)
                    {
                        if (squad == null || squad.Waypoints == null || squad.Waypoints.Count == 0)
                            continue;

                        if (squad.SquadLeader == null || !squad.SquadLeader.Exists())
                            continue;

                        Vector3 squadLeaderPos = squad.SquadLeader.Position;
                        Vector3 targetPos = squad.Waypoints[0];

                        World.DrawLine(squadLeaderPos, targetPos, Color.LimeGreen);
                    }
                }
            }
        }
    }
}