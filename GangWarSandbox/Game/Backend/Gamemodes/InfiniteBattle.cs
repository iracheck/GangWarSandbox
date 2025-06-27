using GTA;
using GangWarSandbox.Gamemodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Gamemodes
{
    /// <summary>
    /// Infinite Battle Gamemode is the barebones gamemode-- nothing special happens, just peds constantly spawning and running toward eachother. Capture Points work but are only there for show --> no victory conditions.
    /// </summary>
    internal class InfiniteBattleGamemode : Gamemode
    {
        // Summary:
        // 
        public InfiniteBattleGamemode() : base("Infinite Battle", "Peds will spawn forever, putting you in a battle that never ends!", 4)
        { }

        // The only thing unique about a "infinite battle" is that it sets loose restrictions on how many vehicles a team can have! In this case, only 40% of their population is dedicated to vehicles
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
