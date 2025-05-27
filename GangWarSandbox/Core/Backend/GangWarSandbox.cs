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
        private readonly Random rand = new Random();

        private ObjectPool _menuPool;
        private NativeMenu _mainMenu;
        private NativeListItem<string> _playerRoleItem;
        private NativeListItem<string> _team1FactionItem;
        private NativeListItem<string> _team2FactionItem;
        private bool isBattleRunning = false;
        private bool isNeutral = true;

        public List<Team> Teams = new List<Team>();
        private Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        private Dictionary<Ped, Vector3?> PedTargets = new Dictionary<Ped, Vector3?>();

        public GangWarSandbox()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;

            _menuPool = new ObjectPool();

            Teams.Add(new Team("One"));
            Teams.Add(new Team("Two"));

            LoadINI();

            Teams[0].BlipSprite = BlipSprite.Number1;
            Teams[0].BlipColor = BlipColor.Green;

            Teams[1].BlipSprite = BlipSprite.Number2;
            Teams[1].BlipColor = BlipColor.Red;

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

            if (isBattleRunning)
            {
                CleanupDead();
            }
        }

        private void UnitAIHandler()
        {
            Ped ped;

            for (int i = 0; i < Teams.Count; i++)
            {
                List<Team> enemyTeams = Teams;
                Team team = Teams[i];
 

                for (int j = 0; j < Teams[i].Peds.Count; j++)
                {
                    ped = team.Peds[j];
                    Vector3? currentTarget = PedTargets.ContainsKey(ped) ? PedTargets[ped] : null;
                    Ped nearbyEnemy = FindNearbyEnemy(ped, team);

                    if (ped == null || !ped.IsAlive || ped.IsInCombat) continue;

                    if (nearbyEnemy != null)
                    {
                        ped.Task.FightAgainst(nearbyEnemy);
                    }
                    else if (currentTarget.HasValue && ped.Position.DistanceTo(currentTarget.Value) < 8f)
                    {
                        currentTarget = null;
                        ped.Task.FightAgainstHatedTargets(999f);
                    }
                    else if (currentTarget == null && nearbyEnemy == null)
                    {
                        currentTarget = FindRandomEnemySpawnpoint(team);
                        ped.Task.RunTo(currentTarget.Value);
                    }

                    PedTargets[ped] = currentTarget;

                }
            }
        }

        private void CleanupDead()
        {
            foreach (var team in Teams)
            {
                for (int i = team.Peds.Count - 1; i >= 0; i--)
                {
                    Ped ped = team.Peds[i];
                    if (!ped.Exists() || ped.IsDead)
                    {
                        if (ped.AttachedBlip.Exists()) ped.AttachedBlip.Delete();
                        team.Peds.RemoveAt(i);
                        team.DeadPeds.Add(ped);
                    }
                }

                if (team.DeadPeds.Count > 20)
                {
                    Ped corpse = team.DeadPeds[0];

                    if (corpse.Exists()) 
                        corpse.Delete();
                    team.DeadPeds.RemoveAt(0);
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                _mainMenu.Visible = !_mainMenu.Visible;
                isNeutral = _playerRoleItem.SelectedItem == "Neutral";
            }
        }

        private void SetupMenu()
        {
            _mainMenu = new NativeMenu("Gang War Sandbox", "OPTIONS");
            _menuPool.Add(_mainMenu);

            _playerRoleItem = new NativeListItem<string>("Player Role", new[] { "Neutral", "Team 1" });
            _team1FactionItem = new NativeListItem<string>("Team 1 Faction", Factions.Keys.ToArray());
            _team2FactionItem = new NativeListItem<string>("Team 2 Faction", Factions.Keys.ToArray());

            _mainMenu.Add(_playerRoleItem);
            _mainMenu.Add(_team1FactionItem);
            _mainMenu.Add(_team2FactionItem);

            var addT1 = new NativeItem("Add Spawnpoint - Team 1");
            var addT2 = new NativeItem("Add Spawnpoint - Team 2");
            var clear = new NativeItem("Clear All Spawnpoints");
            var start = new NativeItem("Start Battle");
            var stop = new NativeItem("Stop Battle");

            addT1.Activated += (item, args) => AddSpawnpoint(1);
            addT2.Activated += (item, args) => AddSpawnpoint(2);
            clear.Activated += (item, args) => ClearAllSpawnpoints();
            start.Activated += (item, args) =>
            {
                ApplyFactionToTeam(Teams[0], _team1FactionItem.SelectedItem);
                ApplyFactionToTeam(Teams[1], _team2FactionItem.SelectedItem);
                StartBattle();
            };
            stop.Activated += (item, args) => StopBattle();

            _mainMenu.Add(addT1);
            _mainMenu.Add(addT2);
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
            Vector3 charPos = Game.Player.Character.Position;
            Teams[teamIndex - 1].AddSpawnpoint(charPos);
        }

        private void StartBattle()
        {
            isBattleRunning = true;
            GTA.UI.Screen.ShowSubtitle("Battle Started!");

            RelationshipGroup player = Game.Player.Character.RelationshipGroup;

            Game.Player.Character.RelationshipGroup = Teams[0].Group;

            foreach (var team in Teams)
            {
                foreach (var other in Teams)
                {
                    var relationship = team == other ? Relationship.Respect : Relationship.Hate;
                    team.Group.SetRelationshipBetweenGroups(other.Group, relationship);
                }
            }
        }

        private void StopBattle()
        {
            isBattleRunning = false;
            GTA.UI.Screen.ShowSubtitle("Battle Ended!");
            CleanupAll();
        }

        private void CleanupAll()
        {
            foreach (var team in Teams)
            {
                foreach (var ped in team.Peds)
                {
                    if (ped.AttachedBlip.Exists()) ped.AttachedBlip.Delete();
                    if (ped.Exists()) ped.Delete();
                }
                team.Peds.Clear();
            }
        }
    }
}