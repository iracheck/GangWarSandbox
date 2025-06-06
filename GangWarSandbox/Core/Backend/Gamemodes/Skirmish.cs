using GTA;
using GangWarSandbox.Gamemodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Core.Backend.Gamemodes
{
    internal class SkirmishGamemode : Gamemode
    {
        Dictionary<Team, int> teamPedReserve = new Dictionary<Team, int>();

        // Multiplier for the number of peds in each team's reserve, default = 15
        // e.g. If a team has a squad size of 3, and a multiplier of 15, they will have 45 reservists
        int numReinforcements = 15; 

        public SkirmishGamemode() : base("Skirmish", "A quick battle between factions, the first to wipe out the others' unit reserve wins.", 4)
        {
            
        }

        public override void InitializeGamemode() { }

        public override void InitializeUI() { }

        public override void OnTick() { }

        public override void OnTickGameRunning() { }

        public override void OnStart()
        {
            // Calculate the reserves of each team!
            for (int i = 0; i < ModData.Teams.Count; i++)
            {
                Team team = ModData.Teams[i];
                int reserve = (int)Helpers.RoundToNearestTen(team.GetSquadSize() * numReinforcements);
                teamPedReserve.Add(ModData.Teams[i], reserve);
            }
        }

        public override void OnEnd()
        {

        }

        public override void OnPedKilled(Ped ped, Team teamOfPed)
        {
            
        }

        public override void OnSquadUpdate(Squad squad) { }

        public override void OnSquadDestroyed(Squad squad, Team teamOfSquad)
        {
            // Handle logic when a Squad is destroyed in Skirmish mode
        }

        public override void OnPlayerDeath() { }

    }
}
