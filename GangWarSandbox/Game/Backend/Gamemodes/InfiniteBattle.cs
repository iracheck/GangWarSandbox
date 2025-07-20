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
        public InfiniteBattleGamemode() : base("Infinite Battle", "DESCRIPTION: Peds will spawn forever, putting you in a battle that never ends!", 4)
        { }

    }
}
