using GangWarSandbox.Peds;
using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Gamemodes
{
    public abstract class Gamemode
    {
        // Last updated: 6/5/2025

        protected GangWarSandbox ModData { get; }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public int MaxFactions { get; private set; }
        public bool SupportCapturePoints { get; set; }

        // Gamemode Attributes
        public float CaptureProgressMultiplier { get; set; } = 1.0f;
        public float PedHealthMultiplier { get; set; } = 1.0f;


        protected Gamemode(string name, string description, int maxFactions, bool supportCapturePoints = true)
        {
            ModData = GangWarSandbox.Instance;
            Name = name;
            Description = description;
            MaxFactions = maxFactions;
            SupportCapturePoints = supportCapturePoints;
        }

        // For anyone looking to add new gamemodes-- these are all the methods avaliable to be implemented in adding your own logic.

        /// <summary>
        /// Executes when the game mode is first selected by the player. This is the best place to initialize certain Gamemode attributes, such as CaptureProgressMultiplier or PedHealthMultiplier. Of course, you can still modify them later.
        /// </summary>
        public virtual void InitializeGamemode() { }

        /// <summary>
        /// Executes when the game mode is first selected by the player. This is the best place to initialize certain Gamemode attributes, such as CaptureProgressMultiplier or PedHealthMultiplier. Of course, you can still modify them later.
        /// </summary>
        public virtual void TerminateGamemode() { }

        /// <summary>
        /// Initializes the UI for the gamemode, such as setting up menus or HUD elements. Executes once when battle begins.
        /// </summary>
        public virtual void InitializeUI() { }

        /// <summary>
        /// Executes every game tick, regardless of if the battle is running or not.
        /// </summary>
        public virtual void OnTick() { }

        /// <summary>
        /// Executes every game tick, only when the battle is running. This executes BEFORE any squad spawning or body cleanup.
        /// </summary>
        public virtual void OnTickGameRunning() { }

        /// <summary>
        /// Runs once when the battle begins. Seperate from UI logic. This specifically executes immediately after teams are initialized with factions, but before spawning begins.
        /// </summary>
        public virtual void OnStart() { }

        /// <summary>
        /// Runs every time a squad is updated (default: 200ms), or when it first spawns. Grants access to all squad data. Example usage: Applying new weapons to squad based on gamemode state
        /// </summary>
        /// <param name="squad">The squad that was updated.</param>
        public virtual void OnSquadUpdate(Squad squad) { }

        /// <summary>
        /// Runs once when the battle ends. This method should clean up any UI modifications, if applicable.
        /// </summary>
        public virtual void OnEnd() { }

        /// <summary>
        ///  Runs whenever a ped is killed, and that ped is cleaned up by the script.
        /// </summary>
        /// <param name="ped">The ped that died</param>
        /// <param name="teamOfPed">The team of the ped that died</param>
        public virtual void OnPedKilled(Ped ped, Team teamOfPed) { }

        /// <summary>
        /// Runs whenever a squad is destroyed and subsequently cleaned up by the script.
        /// </summary>
        /// <param name="squad">The squad that was wiped out</param>
        /// <param name="teamOfSquad">The team of the squad that was destroyed</param>
        public virtual void OnSquadDestroyed(Squad squad, Team teamOfSquad) { }

        /// <summary>
        /// Allows you to determine new conditions for when a squad should spawn. 
        /// Note that this looks at ALL squads, including infantry, vehicles, and helicopters.
        /// This does not overwrite existing conditions, rather allows you to set new ones. 
        /// For example, teams will not go over their unit caps even with this method overriden. 
        /// Return true to allow squad spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnSquad(Team team)
        {
            return true;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 20% of a team's population is allocated to regular vehicle squads.
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnVehicleSquad(Team team)
        {
            return true;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 10% of a team's population is allocated to helicopter squads.
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnWeaponizedVehicleSquad(Team team)
        {
            return true;
        }

        /// <summary>
        /// Allows you to determine new conditions for when a vehicle squad should spawn. Unlike ShouldSpawnSquad(), this OVERWRITES EXISTING conditions, as existing conditions are incredibly circumstanstial.
        /// Example: Infinite Battle/Skirmish conditions say "roughly" 10% of a team's population is allocated to helicopter squads. 
        /// Return true to allow spawning, return false to prevent it. Returns true by default.
        /// </summary>
        public virtual bool ShouldSpawnHelicopterSquad(Team team)
        {
            return true;
        }

        /// <summary>
        /// Listener for when the player dies. Used to determine custom effects during battle when the player dies. Executes *AFTER* the player respawns.
        /// </summary>
        public virtual void OnPlayerDeath() { }

        protected int GetMemberCountByType(Team team, List<Squad> list)
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
