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

        public SkirmishGamemode() : base("Skirmish", "A quick battle between factions, the first to wipe out the others' unit reserve wins.", 4)
        {
            
        }

        public override void OnStart()
        {
            // Calculate the reserves of each team!
            for (int i = 0; i < ModData.Teams.Count; i++)
            {
                Team team = ModData.Teams[i];
                int reserve = (int)Helpers.RoundToNearestTen(team.GetSquadSize() * numReinforcementsMultiplier);
                teamPedReserve.Add(ModData.Teams[i], reserve);
            }
        }

        public override bool ShouldSpawnHelicopterSquad(Team team)
        {
            int members = GetMemberCountByType(team, team.HelicopterSquads);

            if (members >= (team.GetMaxNumPeds() * 0.1f)) // 10%
            {
                return false;
            }

            return true;
        }

        public override bool ShouldSpawnWeaponizedVehicleSquad(Team team)
        {
            int members = GetMemberCountByType(team, team.HelicopterSquads);

            if (members >= (team.GetMaxNumPeds() * 0.1f)) // 10%
            {
                return false;
            }

            return true;
        }

        public override bool ShouldSpawnVehicleSquad(Team team)
        {
            int members = GetMemberCountByType(team, team.HelicopterSquads);

            if (members >= (team.GetMaxNumPeds() * 0.2f)) // 20%
            {
                return false;
            }

            return true;
        }


    }
}
