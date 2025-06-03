// GTA Gang War Sandbox - LemonUI Version
// Requirements:
// - ScriptHookVDotNet v3
// - LemonUI.SHVDN3

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
using GangWarSandbox.Core.Backend;

namespace GangWarSandbox
{
    public class GangWarSandbox : Script
    {
        public static GangWarSandbox Instance { get; private set; }

        private readonly Random rand = new Random();
        public int DEBUG = 0;

        // Constants
        private const String LOG_FILE_PATH = "scripts/GangWarSandbox.log"; // Path to the log file
        private const int AI_UPDATE_FREQUENCY = 200; // How often squad AI will be updated, in milliseconds
        private const int POINT_UPDATE_FREQUENCY = 1000; // How often capture points will be updated, in milliseconds
        private const int MAX_CORPSES = 25; // Maximum number of corpses to keep in memory
        private const int NUM_TEAMS = 4; // How many teams? In the future, it will be loaded from a settings file, but for now it's constant to keep stability
        private const int TIME_BETWEEN_SQUAD_SPAWNS = 3000; // Time in milliseconds between squad spawns for each team

        // Teams
        public int PlayerTeam = -1;
        public List<Team> Teams = new List<Team>();
        public Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        public Dictionary<Team, float> LastSquadSpawnTime = new Dictionary<Team, float>(); // Track last spawn time for each team to prevent spamming or crowding

        // Tracked Peds
        public List<Ped> DeadPeds = new List<Ped>();

        // Capture Points
        public List<CapturePoint> CapturePoints = new List<CapturePoint>();

        // UI Elements
        private ObjectPool _menuPool = new ObjectPool();

        private NativeMenu _mainMenu;
        private NativeMenu _teamSetupMenu;
        private NativeMenu _spawnpointMenu;
        private NativeMenu _battleControlMenu;
        private List<NativeListItem<string>> _teamFactionItems = new List<NativeListItem<string>>();

        // Game State
        private bool IsBattleRunning = false;
        private Gamemode CurrentGamemode = Gamemode.None; // Currently unused, but can be used to define different gamemodes in the future
       
        enum Gamemode
        {
            None,
            GunGame,
            Deathmatch,
            Conquest,
            KOTH,
        } 

        public GangWarSandbox()
        {
            Instance = this;

            Tick += OnTick;
            KeyDown += OnKeyDown;

            _menuPool = new ObjectPool();

            for (int i = 0; i < NUM_TEAMS; i++)
            {
                Teams.Add(new Team("Team " + (i + 1))); // Initialize teams with default names and groups
                LastSquadSpawnTime[Teams[i]] = 0; // Initialize last spawn time for each team

                // build the enum‐member name
                string enumName = $"Number{i + 1}";

                // try to parse it into a BlipSprite
                if (Enum.TryParse<BlipSprite>(enumName, out var sprite))
                {
                    Teams[i].BlipSprite = sprite;
                }
                else
                {
                    // fallback if something goes wrong
                    Teams[i].BlipSprite = BlipSprite.Standard;
                }
            }

            LoadINI();

            SetupMenu();
        }

        private void LoadINI()
        {
            try
            {
                string path = "scripts/GangWarSandbox.ini";
                if (!File.Exists(path)) return;

                var lines = File.ReadAllLines(path);

                string currentFaction = null;
                Faction faction = null;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("=") && !line.StartsWith("[")) continue;

                    if (line.StartsWith("["))
                    {
                        currentFaction = line.Trim('[', ']');
                        faction = new Faction { Name = currentFaction };
                        Factions[currentFaction] = faction;
                        continue;
                    }

                    if (faction == null) continue;

                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex == -1 || equalsIndex == line.Length - 1) continue; // skip invalid lines

                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    switch (key)
                    {
                        case "Models":
                            faction.Models = value.Split(',').Select(s => s.Trim()).ToArray();
                            break;
                        case "Tier4Model":
                            faction.Tier4Model = value;
                            break;
                        case "Tier1Weapons":
                            faction.Tier1Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                            break;
                        case "Tier2Weapons":
                            faction.Tier2Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                            break;
                        case "Tier3Weapons":
                            faction.Tier3Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                            break;
                        case "MaxSoldiers":
                            if (int.TryParse(value, out int soldiers))
                                faction.MaxSoldiers = soldiers;
                            break;
                        case "BaseHealth":
                            if (int.TryParse(value, out int health))
                                faction.BaseHealth = health;
                            break;
                        case "BaseAccuracy":
                            if (int.TryParse(value, out int accuracy))
                                faction.Accuracy = accuracy;
                            break;
                        case "TierUpgradeMultiplier":
                            if (float.TryParse(value, out float mult))
                                faction.TierUpgradeMultiplier = mult;
                            break;
                        case "BlipColor":
                            if (Enum.TryParse(value, out BlipColor blipColor))
                                faction.Color = blipColor;
                            break;
                    }
                }
            }
            catch
            {
                GTA.UI.Screen.ShowSubtitle("Error in loading INI. Please report it on the mod page, sorry!");
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            _menuPool.Process();

            int GameTime = Game.GameTime;

            if (IsBattleRunning)
            {
                SpawnSquads(); // spawn squads that may be missing

                // Collection error prevention
                var allSquads = Teams.SelectMany(t => t.Squads).ToList();

                foreach (var squad in allSquads)
                {
                    Logger.LogDebug("Updating Ped AI");
                    // Ped AI
                    if (Game.GameTime % AI_UPDATE_FREQUENCY == 0 || squad.JustSpawned)
                    {
                        squad.SquadAIHandler();
                    }

                    // Corpse Removal
                    Logger.LogDebug("Removing dead people...");
                    List<Ped> deadPeds = squad.CleanupDead();
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

                Logger.LogDebug("Updating capturepoints");
                foreach (var point in CapturePoints)
                {
                    if (Game.GameTime % POINT_UPDATE_FREQUENCY == 0)
                    point.CapturePointHandler(); // Process capture points
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                if (!_menuPool.AreAnyVisible)
                    _mainMenu.Visible = true;
                else
                    _menuPool.HideAll();
            }
        }


        // ChatGPT supplied me with much of the documentation/LemonUI api knowledge during the creation of this.
        // It will be rewritten eventually, but in the meantime avoid making anything dependent on this code.
        // (there should never be anything dependent on it, but just as a precaution)
        private void SetupMenu()
        {
            // MAIN MENU
            _mainMenu = new NativeMenu("Gang War Sandbox", "MAIN MENU");
            _menuPool.Add(_mainMenu);

            // Submenu: TEAM SETUP
            _teamSetupMenu = new NativeMenu("Team Setup", "Configure Teams");
            _menuPool.Add(_teamSetupMenu);
            _mainMenu.AddSubMenu(_teamSetupMenu);

            List<String> playerTeamItem_Teams = new List<String> { "Neutral" };
            for (int i = 0; i < NUM_TEAMS; i++)
                playerTeamItem_Teams.Add("Team " + (i + 1));

            var playerTeamItem = new NativeListItem<string>("Player Team", playerTeamItem_Teams.ToArray());

            playerTeamItem.ItemChanged += (item, args) =>
            {
                var sel = playerTeamItem.SelectedItem;
                PlayerTeam = (sel == "Neutral") ? -1 : int.Parse(sel.Substring(5)) - 1;
            };

            _teamFactionItems.Clear();
            for (int i = 0; i < NUM_TEAMS; i++)
            {
                var teamFactionItem = new NativeListItem<string>($"Team {i + 1} Faction", Factions.Keys.ToArray());
                _teamFactionItems.Add(teamFactionItem);
                _teamSetupMenu.Add(teamFactionItem);
            }
            _teamSetupMenu.Add(playerTeamItem);

            // Submenu: SPAWNPOINT SETUP
            _spawnpointMenu = new NativeMenu("Map Markers", "Manage Map Markers");
            _menuPool.Add(_spawnpointMenu);
            _mainMenu.AddSubMenu(_spawnpointMenu);

            var addT1 = new NativeItem("Add Spawnpoint - Team 1");
            var addT2 = new NativeItem("Add Spawnpoint - Team 2");
            var addT3 = new NativeItem("Add Spawnpoint - Team 3");
            var addT4 = new NativeItem("Add Spawnpoint - Team 4");
            var clearNearest = new NativeItem("Clear Nearest Spawnpoint");
            var clear = new NativeItem("Clear All Spawnpoints");
            var addCapPt = new NativeItem("Add Capture Point");



            addT1.Activated += (item, args) => AddSpawnpoint(1);
            addT2.Activated += (item, args) => AddSpawnpoint(2);
            addT3.Activated += (item, args) => AddSpawnpoint(3);
            addT4.Activated += (item, args) => AddSpawnpoint(4);

            addCapPt.Activated += (item, args) => AddCapturePoint();

            clear.Activated += (item, args) => ClearAllPoints();



            _spawnpointMenu.Add(addT1);
            _spawnpointMenu.Add(addT2);
            _spawnpointMenu.Add(addT3);
            _spawnpointMenu.Add(addT4);
            _spawnpointMenu.Add(addCapPt);
            _spawnpointMenu.Add(clear);

            // Submenu: BATTLE CONTROL
            _battleControlMenu = new NativeMenu("Battle Control", "Start or Stop Battle");
            _menuPool.Add(_battleControlMenu);
            _mainMenu.AddSubMenu(_battleControlMenu);

            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            start.Activated += (item, args) =>
            {
                for (int i = 0; i < NUM_TEAMS; i++)
                    ApplyFactionToTeam(Teams[i], _teamFactionItems[i].SelectedItem);
                StartBattle();
            };
            stop.Activated += (item, args) => StopBattle();

            _battleControlMenu.Add(start);
            _battleControlMenu.Add(stop);
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
                team.teamIndex = Teams.IndexOf(team);
            }
        }

        private void ClearAllPoints()
        {
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

        private void AddCapturePoint()
        {
            if (!IsBattleRunning)
            {
                CapturePoint point;

                if (Game.IsWaypointActive)
                {
                    Vector3 waypointPos = World.WaypointPosition;
                    point = new CapturePoint(waypointPos);

                    GTA.UI.Screen.ShowSubtitle($"Capture point created at waypoint.");
                    World.RemoveWaypoint();
                }
                else
                {
                    Vector3 charPos = Game.Player.Character.Position;
                    point = new CapturePoint(charPos);
                    
                    GTA.UI.Screen.ShowSubtitle($"Capture point created at player location.");
                }

                CapturePoints.Add(point);
            }
            else
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to create a new capture point.");
            }
        }


        private void StartBattle()
        {
            Logger.LogDebug("Starting battle");
            IsBattleRunning = true;
            GTA.UI.Screen.ShowSubtitle("Battle Started!");

            Ped player = Game.Player.Character;

            Logger.LogDebug("Assigning player team");
            // Assign player to team
            if (PlayerTeam == -1)
            {
                Game.Player.Character.RelationshipGroup = "PLAYER";
            }
            else
            {
                Game.Player.Character.RelationshipGroup = Teams[PlayerTeam].Group;
                Teams[PlayerTeam].Tier4Ped = player; // Assign player to be their team's "strong npc"

                Logger.LogDebug("Player has team, teleporting them...");
                // move the player to the first spawn point of their team
                if (Teams[PlayerTeam].SpawnPoints.Count > 0)
                {
                    player.Position = Teams[PlayerTeam].SpawnPoints[0];
                }
            }

            Logger.LogDebug("Assigning team relationships");
            foreach (var team in Teams)
            {
                // team.RecolorBlips(); // ensure the blips are the correct color

                foreach (var other in Teams)
                {
                    var relationship = team == other ? Relationship.Respect : Relationship.Hate;
                    team.Group.SetRelationshipBetweenGroups(other.Group, relationship);
                }

            }

            Logger.LogDebug("Spawning squads for the first time...");
            // Spawn squads for each team
            SpawnSquads();
        }

        private void SpawnSquads()
        {
            foreach (var team in Teams)
            {
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

                while (LastSquadSpawnTime[team] >= Game.GameTime - TIME_BETWEEN_SQUAD_SPAWNS && numAlive + squadSize <= team.Faction.MaxSoldiers)
                {
                    LastSquadSpawnTime[team] = Game.GameTime;
                    Squad squad = new Squad(team, 0);

                    team.Squads.Add(squad);
                    numAlive += squad.Members.Count;
                }
            }
        }

        private void StopBattle()
        {
            IsBattleRunning = false;
            GTA.UI.Screen.ShowSubtitle("Battle Ended!");
            CleanupAll();
        }

        private void CleanupAll()
        {
            foreach (var team in Teams)
            {
                // Make a copy of the list to avoid modifying it while iterating
                var squadsToRemove = team.Squads.ToList();

                foreach (var squad in squadsToRemove)
                {
                    try
                    {
                        squad.Destroy(); // This can safely remove it from team.Squads now
                    }
                    catch (Exception ex)
                    {
                        GTA.UI.Screen.ShowSubtitle($"Squad cleanup error: {ex.Message}");
                    }
                }

                team.Squads.Clear(); // extra safety to make sure it's empty
            }

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
}