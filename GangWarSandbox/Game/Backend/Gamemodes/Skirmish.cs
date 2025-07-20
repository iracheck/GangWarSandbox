using GTA;
using GangWarSandbox.Gamemodes;
using GangWarSandbox.Peds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Gamemodes
{
    internal class SkirmishGamemode : Gamemode
    {
        Dictionary<Team, int> teamPedReserve = new Dictionary<Team, int>();

        // Multiplier for the number of peds in each team's reserve, default = 15
        // e.g. If a team has a squad size of 3, and a multiplier of 15, they will have 45 reservists
        int numReinforcementsMultiplier = 15; 

        public SkirmishGamemode() : base("Skirmish", "DESCRIPTION: A quick battle between factions, the first to wipe out the others' unit reserve wins. >>Incomplete gamemode<<", 4)
        {
            
        }

        public override void OnStart()
        {
            // Calculate the reserves of each team!
            for (int i = 0; i < Mod.Teams.Count; i++)
            {
                Team team = Mod.Teams[i];
                int reserve = (int)Helpers.RoundToNearestTen(team.GetSquadSize() * numReinforcementsMultiplier);
                teamPedReserve.Add(Mod.Teams[i], reserve);
            }
        }

        public override void OnTickGameRunning()
        {
            base.OnTickGameRunning();
        }


    }
}
