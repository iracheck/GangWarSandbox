using GTA;
using GangWarSandbox.Gamemodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Gamemodes
{
    internal class InfiniteBattleGamemode : Gamemode
    {

        public InfiniteBattleGamemode() : base("Infinite Battle", "Peds will spawn forever, putting you in a battle that never ends!", 4)
        {
            
        }

        public override void InitializeGamemode() { }

        public override void InitializeUI() { }

        public override void OnTick() { }

        public override void OnTickGameRunning() { }

        public override void OnStart()
        {
        }

        public override void OnEnd() { }

        public override void OnPedKilled(Ped ped, Team teamOfPed) { }

        public override void OnSquadDestroyed(Squad squad, Team teamOfSquad) { }

        public virtual bool ShouldSpawnSquad()
        {
            return true;
        }

        public override void OnPlayerDeath() {}

    }
}
