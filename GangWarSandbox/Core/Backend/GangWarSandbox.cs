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

namespace GangWarSandbox
{
    public class GangWarSandbox : Script
    {
        public static GangWarSandbox Instance { get; private set; }

        private readonly Random rand = new Random();

        private const int MAX_CORPSES = 25; // Maximum number of corpses to keep in memory
        private static int NUM_TEAMS = 4; // loaded once from ini-- in current phase its a constant

        private int PlayerTeam = -1;

        // UI Elements
        private ObjectPool _menuPool;
        private NativeMenu _mainMenu;
        private List<NativeListItem<string>> _teamFactionItems = new List<NativeListItem<string>>();



        private bool IsBattleRunning = false;

        public List<Team> Teams = new List<Team>();
        private Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

        public List<Ped> DeadPeds = new List<Ped>();

        public GangWarSandbox()
        {
            Instance = this;

            Tick += OnTick;
            KeyDown += OnKeyDown;

            _menuPool = new ObjectPool();

            for (int i = 0; i < NUM_TEAMS; i++)
            {
                Teams.Add(new Team("Team " + (i + 1))); // Initialize teams with default names and groups

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

                // Make a snapshot copy of all squads to avoid collection modification issues
                var allSquads = Teams.SelectMany(t => t.Squads).ToList();

                foreach (var squad in allSquads)
                {
                    if (Game.GameTime % 200 == 0 || squad.JustSpawned)
                    {
                        squad.SquadAIHandler();
                    }

                    List<Ped> deadPeds = squad.CleanupDead();
                    DeadPeds.AddRange(deadPeds);

                    // Remove corpses if there are too many
                    while (DeadPeds.Count >= MAX_CORPSES)
                    {
                        if (DeadPeds[0] != null && DeadPeds[0].Exists())
                        {
                            DeadPeds[0].Delete();
                        }
                        DeadPeds.RemoveAt(0);
                    }
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                _mainMenu.Visible = !_mainMenu.Visible;
            }
        }

        private void SetupMenu()
        {
            _mainMenu = new NativeMenu("Gang War Sandbox", "OPTIONS");
            _menuPool.Add(_mainMenu);

            var playerTeamItem = new NativeListItem<string>("Player Team", new[] { "Neutral" });

            for (int i = 0; i < NUM_TEAMS; i++)
            {
                playerTeamItem.Add("Team " + (i + 1));
            }

            // FIXED: Clear list and populate after creating the actual list items
            _teamFactionItems.Clear();

            for (int i = 0; i < NUM_TEAMS; i++)
            {
                var teamFactionItem = new NativeListItem<string>("Team " + (i + 1) + " Faction", Factions.Keys.ToArray());
                _teamFactionItems.Add(teamFactionItem);
                _mainMenu.Add(teamFactionItem);
            }

            var addT1 = new NativeItem("Add Spawnpoint - Team 1");
            var addT2 = new NativeItem("Add Spawnpoint - Team 2");
            var addT3 = new NativeItem("Add Spawnpoint - Team 3");
            var addT4 = new NativeItem("Add Spawnpoint - Team 4");
            var clear = new NativeItem("Clear All Spawnpoints");
            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            playerTeamItem.Activated += (item, args) =>
            {
                var sel = playerTeamItem.SelectedItem;
                if (sel == "Neutral")
                    PlayerTeam = -1;
                else
                    PlayerTeam = int.Parse(sel.Substring(5)) - 1;
            };
            addT1.Activated += (item, args) => AddSpawnpoint(1);
            addT2.Activated += (item, args) => AddSpawnpoint(2);
            addT3.Activated += (item, args) => AddSpawnpoint(3);
            addT4.Activated += (item, args) => AddSpawnpoint(4);
            clear.Activated += (item, args) => ClearAllSpawnpoints();
            start.Activated += (item, args) =>
            {
                for (int i = 0; i < NUM_TEAMS; i++)
                {
                    ApplyFactionToTeam(Teams[i], _teamFactionItems[i].SelectedItem);
                }
                StartBattle();
            };
            stop.Activated += (item, args) => StopBattle();

            _mainMenu.Add(playerTeamItem);
            _mainMenu.Add(addT1);
            _mainMenu.Add(addT2);
            _mainMenu.Add(addT3);
            _mainMenu.Add(addT4);
            _mainMenu.Add(clear);
            _mainMenu.Add(start);
            _mainMenu.Add(stop);
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

        private void ClearAllSpawnpoints()
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
        }

        private void AddSpawnpoint(int teamIndex)
        {
            if (!IsBattleRunning)
            {
                Vector3 charPos = Game.Player.Character.Position;
                Teams[teamIndex - 1].AddSpawnpoint(charPos);
            }
            else
            {
                GTA.UI.Screen.ShowSubtitle("Stop the battle to create a new spawnpoint.");
            }
        }

        private void StartBattle()
        {
            IsBattleRunning = true;
            GTA.UI.Screen.ShowSubtitle("Battle Started!");

            Ped player = Game.Player.Character;


            // Assign player to team
            if (PlayerTeam == -1)
            {
                Game.Player.Character.RelationshipGroup = "PLAYER";
            }
            else
            {
                Game.Player.Character.RelationshipGroup = Teams[PlayerTeam].Group;
                Teams[PlayerTeam].Tier4Ped = player; // Assign player to be their team's "strong npc"

                // move the player to the first spawn point of their team
                if (Teams[PlayerTeam].SpawnPoints.Count > 0)
                {
                    player.Position = Teams[PlayerTeam].SpawnPoints[0];
                }
            }

            foreach (var team in Teams)
            {
                team.RecolorBlips(); // ensure the blips are the correct color

                foreach (var other in Teams)
                {
                    var relationship = team == other ? Relationship.Respect : Relationship.Hate;
                    team.Group.SetRelationshipBetweenGroups(other.Group, relationship);
                }

            }

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

                // Avoid infinite loop or bad math
                if (squadSize <= 0) continue;

                while (numAlive + squadSize <= team.Faction.MaxSoldiers)
                {
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