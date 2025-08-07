using GangWarSandbox.Peds;
using GTA;
using GTA.Math;
using LemonUI.Menus;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GangWarSandbox.Utilities;
using static GangWarSandbox.Peds.Squad;

namespace GangWarSandbox.Gamemodes
{
    public abstract class Gamemode
    {
        // Last updated: 6/5/2025

        protected static GangWarSandbox Mod { get; set; }

        public enum GamemodeBool { PlayerChoice = -1, False = 0, True = 1 }
        public enum GamemodeSpawnMethod { Spawnpoint, Random }

        public string Name { get; private set; } = "no_name";
        public string Description { get; private set; } = "no_desc";
        public int MaxTeams { get; private set; } = GangWarSandbox.NUM_TEAMS;

        // Gamemode Settings
        public GamemodeSpawnMethod SpawnMethod = GamemodeSpawnMethod.Spawnpoint; // options: "Spawnpoint", "Random"

        // Treat these ints as a bool --> 0 = false, 1 = true, -1 = player choice
        public GamemodeBool EnableParameter_AllowVehicles { get; set; } = GamemodeBool.PlayerChoice;
        public GamemodeBool EnableParameter_AllowWeaponizedVehicles { get; set; } = GamemodeBool.PlayerChoice;
        public GamemodeBool EnableParameter_AllowHelicopters { get; set; } = GamemodeBool.PlayerChoice;
        public GamemodeBool EnableParameter_FogOfWar { get; set; } = GamemodeBool.PlayerChoice;

        // if unit count multiplier is not "PlayerChoice," it can't be modified. Unfortunately just a small quirk with checkboxes and how they relate to Gamemodes.
        public GamemodeBool EnableParameter_UnitCountMultiplier { get; set; } = GamemodeBool.PlayerChoice;


        public GamemodeBool EnableParameter_Spawnpoints { get; set; } = GamemodeBool.PlayerChoice;
        public GamemodeBool EnableParameter_CapturePoints { get; set; } = GamemodeBool.PlayerChoice;

        // These are the users actual choices
        public bool SpawnVehicles { get; set; } = true;
        public bool SpawnWeaponizedVehicles { get; set; } = false;
        public bool SpawnHelicopters { get; set; } = false;
        public bool FogOfWar { get; set; } = true;

        /// <summary>
        /// This value directly multiplies the maximum number of units for each faction on the battlefield. It can be modified by the user in the gamemode menu, or by the gamemode itself.
        /// </summary>
        public float UnitCountMultiplier = 1; // Multiplier for unit count, used to scale the number of soldiers per team based on faction settings


        // Gamemode Attributes
        // These are a list of attributes that are 
        /// <summary>
        /// Higher values decrease the time it takes to capture a capture point. Default: 1.0f. 
        /// </summary>
        public float CaptureProgressMultiplier { get; set; } = 1.0f;

        /// <summary>
        /// A direct multiple of the maximum ped health. Generally suggested to remain quite low (or around the default) Default: 1.0f.
        /// </summary>
        public float PedHealthMultiplier { get; set; } = 1.0f;

        public bool HasTier4Ped = true;



        protected Gamemode(string name, string description, int maxFactions)
        {
            Mod = GangWarSandbox.Instance;

            Name = name;
            Description = "DESCRIPTION: " + description;
            MaxTeams = maxFactions;
        }

        // For anyone looking to add new gamemodes-- these are all the methods avaliable to be implemented in adding your own logic.

        /// <summary>
        /// This allows you to construct a LemonUI menu for the gamemode, which appears as the second option in the list below the "Gamemode." It will appear as "Gamemode Options." See LemonUI documentation for more help.
        /// </summary>
        public virtual NativeMenu ConstructGamemodeMenu()
        {
            return null;
        }

        /// <summary>
        /// Conditions for when the gamemode can be started. This is helpful for enforcing a specified number of spawnpoints, etc. The unoverriden version of this function ensures two valid teams with two valid spawnpoints. 
        /// Also consider adding an error message. Return true to start the battle.
        /// </summary>
        public virtual bool CanStartBattle()
        {
            int validTeams = 0;
            Mod = GangWarSandbox.Instance;

            for (int i = 0; i < MaxTeams; i++)
            {
                var team = Mod.Teams[i];

                if (team != null && team.SpawnPoints != null && team.SpawnPoints.Count != 0)
                {
                    validTeams++;
                }
            }

            if (validTeams >= 2) return true;
            else
            {
                GTA.UI.Screen.ShowSubtitle("You must have atleast two factions with a spawnpoint to start the battle.", 2500);
                return false;
            }
        }

        /// <summary>
        /// Executes when the game mode is first selected by the player. This is the best place to initialize certain Gamemode attributes, such as CaptureProgressMultiplier or PedHealthMultiplier. Of course, you can still modify them later. It also includes a variable, "oldGM" that allows you to retain data from the previously selected gamemode, which is what the unoverriden version of this method does.
        /// </summary>
        public virtual void InitializeGamemode(Gamemode oldGM)
        {
            if (ShouldBeEnabled(EnableParameter_UnitCountMultiplier)) UnitCountMultiplier = oldGM.UnitCountMultiplier;
            if (ShouldBeEnabled(EnableParameter_AllowVehicles)) SpawnVehicles = oldGM.SpawnVehicles;
            if (ShouldBeEnabled(EnableParameter_AllowWeaponizedVehicles)) SpawnWeaponizedVehicles = oldGM.SpawnWeaponizedVehicles;
            if (ShouldBeEnabled(EnableParameter_AllowHelicopters)) SpawnHelicopters = oldGM.SpawnHelicopters;
            if (ShouldBeEnabled(EnableParameter_FogOfWar)) FogOfWar = oldGM.FogOfWar;
        }

        /// <summary>
        /// Executes when the game mode is unselected in the GUI. This is where you should remove all irrelevant data from the previous gamemode, such as clearing out capture points or spawnpoints.
        /// </summary>
        public virtual void TerminateGamemode() { }

        /// <summary>
        /// Executes every game tick, regardless of if the battle is running or not.
        /// </summary>
        public virtual void OnTick() { }

        /// <summary>
        /// Executes every game tick, only when the battle is running. This executes BEFORE any squad spawning or body cleanup.
        /// </summary>
        public virtual void OnTickGameRunning() { }

        /// <summary>
        /// Runs once when the battle begins. This specifically executes immediately after teams are initialized with factions, but before spawning begins.
        /// </summary>
        public virtual void OnStart() { }

        /// <summary>
        /// Runs immediately after a squad is spawned.
        /// </summary>
        /// <param name="squad">The squad that spawned</param>
        public virtual void OnSquadSpawn(Squad squad) { }

        /// <summary>
        /// Runs immediately after a squad is spawned.
        /// </summary>
        /// <param name="squad">The squad that spawned</param>
        public virtual void OnVehicleSpawn(Vehicle vehicle) { }

        /// <summary>
        /// Runs every time a squad is updated (default: 200ms), or when it first spawns. Grants access to all squad data. Example usage: Applying new weapons to squad based on gamemode state
        /// </summary>
        /// <param name="squad">The squad that was updated.</param>
        public virtual void OnSquadUpdate(Squad squad) { }

        /// <summary>
        /// Runs every time a squad is updated (default: 200ms), or when it first spawns. Return true to ignore any additional AI logic.
        /// </summary>
        /// <param name="ped">The ped that is being possibly overriden</param>
        public virtual bool AIOverride(Squad squad, Ped ped)
        {
            return false;
        } 

        /// <summary>
        /// Runs at the beginning, and sets up relationships. By default, this resets everything.
        /// </summary>
        public virtual void SetRelationships()
        {
            foreach (var team in Mod.Teams)
            {
                team.AlliedIndexes.Clear();
            }

            Mod.ResetPlayerRelations();
        }

        /// <summary>
        /// Runs once when the battle ends. This method should clean up any UI modifications, if applicable.
        /// </summary>
        public virtual void OnEnd() { }

        /// <summary>
        ///  Runs whenever a ped is killed, and immediately before that ped is cleaned up by the script.
        /// </summary>
        /// <param name="ped">The ped that died</param>
        /// <param name="teamOfPed">The team of the ped that died</param>
        public virtual void OnPedKilled(Ped ped, Team teamOfPed) { }

        /// <summary>
        /// Handles squad target finding. Waypoints are automatically generated based upon this 
        /// </summary>
        /// <param name="squad">The squad that was wiped out</param>
        public virtual Vector3 GetTarget(Squad s)
        {
            Random rand = new Random();
            List<CapturePoint> capturePoints = Mod.CapturePoints;
            Vector3 target = Vector3.Zero;

            if (s.Role == SquadRole.AssaultCapturePoint)
            {
                List<CapturePoint> unownedPoints = new List<CapturePoint>();

                for (int i = 0; i < capturePoints.Count; i++)
                {
                    if (capturePoints[i].Owner == null || capturePoints[i].Owner != s.Owner)
                    {
                        unownedPoints.Add(capturePoints[i]); // add the capture point to the list of unowned points
                    }
                }

                if (unownedPoints.Count > 0)
                {
                    s.TargetPoint = unownedPoints[rand.Next(unownedPoints.Count)]; // randomly select a capture point that is not owned by the squad's team
                    target = s.TargetPoint.Position; // set the target to the capture point's position
                }
                else
                {
                    s.TargetPoint = null;
                    target = Vector3.Zero;
                }
            }
            else if (s.Role == SquadRole.DefendCapturePoint)
            {
                for (int i = 0; i < capturePoints.Count; i++)
                {
                    if (capturePoints[i] != null && capturePoints[i].Owner == s.Owner)
                    {
                        s.TargetPoint = capturePoints[i]; // set the target point to the capture point owned by the squad's team
                        target = s.TargetPoint.Position;
                    }
                }
            }
            else if (s.Role == SquadRole.SeekAndDestroy || s.Role == SquadRole.VehicleSupport)
            {
                {
                    target = PedAI.FindRandomEnemySpawnpoint(s.Owner);
                }
            }

            // Final failsafe: ensure a non-zero target is returned
            if (target == Vector3.Zero)
            {
                target = PedAI.FindRandomEnemySpawnpoint(s.Owner);

                // Still can't find one? Fallback solution
                if (target == Vector3.Zero)
                {
                    Logger.LogError("A squad failed to find a valid target.");
                    GTA.UI.Screen.ShowSubtitle("A squad failed to find a valid target. This is a bug, please report it to the developer.");
                }
            }

            return target;
        }

        public virtual bool ShouldGetNewTarget(Squad s)
        {
            return false;
        }

        /// <summary>
        /// Runs whenever a squad is destroyed and subsequently cleaned up by the script.
        /// </summary>
        /// <param name="squad">The squad that was wiped out</param>
        /// <param name="teamOfSquad">The team of the squad that was destroyed</param>
        public virtual void OnSquadDestroyed(Squad squad, Team teamOfSquad) { }

        /// <summary>
        /// Cleans up all data from mod based on what the allowed settings of the new gamemode are. At the moment, this is not implemented fully and clears out everything for safety.
        /// </summary>
        public void ClearPreviousGamemode()
        {
            Mod.ClearAllPoints();
            Mod.CleanupAll();
        }

        /// <summary>
        /// Allows you to determine new conditions for when a squad should spawn. 
        /// Note that this looks at ALL squads, including infantry, vehicles, and helicopters.
        /// This does not overwrite existing conditions, rather allows you to set new ones. 
        /// For example, teams will not go over their unit caps even with this method overriden. 
        /// Return true to allow squad spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnSquad(Team team, int squadSize)
        {
            // SAFETY CHECKS: Prevent crashes and ensure team is even capable of spawning squads
            if (
                (SpawnMethod == GamemodeSpawnMethod.Spawnpoint && (team.SpawnPoints == null || team.SpawnPoints.Count == 0)) ||
                team.Models.Length == 0
            )
            {
                return false;
            }

            int numAlive = 0;

            var allTeamSquads = team.Squads
                .Concat(team.VehicleSquads)
                .Concat(team.WeaponizedVehicleSquads)
                .Concat(team.HelicopterSquads)
                .ToList();

            for (int i = 0; i < allTeamSquads.Count(); i++)
            {
                numAlive += allTeamSquads[i].Members.Count;
            }

            if (numAlive + squadSize <= team.GetMaxNumPeds() &&
                team.Models != null && team.Models.Length > 0) return true;
            else return false;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 20% of a team's population is allocated to regular vehicle squads.
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnVehicleSquad(Team team)
        {
            if (Helpers.RandomChance(50)) return false;

            int members = GetMemberCountByType(team, team.VehicleSquads);

            if (members >= (team.GetMaxNumPeds() * 0.10f)) // 10%
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 10% of a team's population is allocated to helicopter squads.
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnWeaponizedVehicleSquad(Team team)
        {
            if (Helpers.RandomChance(30)) return false;

            int members = GetMemberCountByType(team, team.WeaponizedVehicleSquads);

            if (members >= (team.GetMaxNumPeds() * 0.10f)) // 10%
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 10% of a team's population is allocated to helicopter squads. 
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnHelicopterSquad(Team team)
        {
            if (Helpers.RandomChance(20)) return false;

            int members = GetMemberCountByType(team, team.HelicopterSquads);

            if (members >= (team.GetMaxNumPeds() * 0.10f)) // 10%
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Listener for when the player dies. Used to determine custom effects during battle when the player dies. Executes *AFTER* the player respawns.
        /// </summary>
        public virtual void OnPlayerDeath() { }

        public static bool ShouldBeTicked(GamemodeBool b)
        {
            if (b == GamemodeBool.PlayerChoice || b == GamemodeBool.True) return true;
            else return false;
        }

        public static bool ShouldBeEnabled(GamemodeBool b)
        {
            if (b == GamemodeBool.PlayerChoice) return true;
            else return false;
        }

        protected static int GetMemberCountByType(Team team, List<Squad> list)
        {
            int count = 0;

            foreach (var squad in list)
            {
                count += squad.Members.Count;
            }

            return count;
        }
    }
}
