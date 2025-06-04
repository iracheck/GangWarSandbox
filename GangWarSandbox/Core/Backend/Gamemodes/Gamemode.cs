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
        // Last updated: 6/4/2025

        protected GangWarSandbox ModData { get; }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public int MaxFactions { get; private set; }

        protected Gamemode(string name, string description, int maxFactions)
        {
            ModData = GangWarSandbox.Instance;
            Name = name;
            Description = description;
            MaxFactions = maxFactions;
        }

        // For anyone looking to add new gamemodes-- these are all the methods avaliable to be implemented in adding your own logic.

        /// <summary>
        /// Executes when the game mode is first selected by the player
        /// </summary>
        public abstract void InitializeGamemode();

        /// <summary>
        /// Initializes the UI for the gamemode, such as setting up menus or HUD elements. Executes once when battle begins.
        /// </summary>
        public abstract void InitializeUI();

        /// <summary>
        /// Executes every game tick, regardless of if the battle is running or not.
        /// </summary>
        public abstract void OnTick();

        /// <summary>
        /// Executes every game tick, only when the battle is running.
        /// </summary>
        public abstract void OnTickGameRunning();

        /// <summary>
        /// Runs once when the battle begins. Seperate from UI logic. This specifically executes immediately after teams are initialized with factions, but before spawning begins.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// Runs once when the battle ends. This method should clean up any UI modifications, if applicable.
        /// </summary>
        public abstract void OnEnd();

        /// <summary>
        ///  Runs whenever a ped is killed, and that ped is cleaned up by the script.
        /// </summary>
        /// <param name="ped">The ped that died</param>
        /// <param name="teamOfPed">The team of the ped that died</param>
        public abstract void OnPedKilled(Ped ped, Team teamOfPed);

        /// <summary>
        /// Runs whenever a squad is destroyed and subsequently cleaned up by the script.
        /// </summary>
        /// <param name="squad">The squad that was wiped out</param>
        /// <param name="teamOfSquad">The team of the squad that was destroyed</param>
        public abstract void OnSquadDestroyed(Squad squad, Team teamOfSquad);

        /// <summary>
        /// Allows you to determine new conditions for when a squad should spawn. 
        /// This does not overwrite existing conditions, rather allows you to set new ones. 
        /// For example, teams will not go over their unit caps even with this method overriden. 
        /// Return true to allow squad spawning, return false to prevent it.
        /// </summary>
        public virtual bool ShouldSpawnSquad()
        {
            return true;
        }

        /// <summary>
        /// Listener for when the player dies. Used to determine custom effects during battle when the player dies. --> Executes after the player respawns.
        /// </summary>
        public abstract void OnPlayerDeath();

    }
}
