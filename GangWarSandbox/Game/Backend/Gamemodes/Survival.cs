using GTA;
using GangWarSandbox.Gamemodes;
using GangWarSandbox.Peds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LemonUI.Menus;

namespace GangWarSandbox.Gamemodes
{
    internal class SurvivalGamemode : Gamemode
    {
        double PlayerScore = 0;

        double TimeStart;
        double TimeElapsed;

        // Gamemode States
        int currentLevel = 0; // Current level of the survival gamemode, used for difficulty scaling

        int Combo = 1;
        int ComboLastTime = 0;

        public SurvivalGamemode() : base("Survival", "Survive as long as possible. Kill enemies to earn points, and try to achieve the highest score you can!", 0)
        {
            EnableParameter_AllowWeaponizedVehicles = GamemodeBool.True;
            EnableParameter_AllowVehicles = GamemodeBool.True;
            EnableParameter_AllowHelicopters = GamemodeBool.True;

            EnableParameter_CapturePoints = GamemodeBool.False;
            EnableParameter_Spawnpoints = GamemodeBool.False;
        }

        public override void OnStart()
        {
            TimeStart = Game.GameTime;
            
            PlayerScore = 0;
            Combo = 1;
            ComboLastTime = 0;

            InitializeUI();
        }

        public override NativeMenu ConstructGamemodeMenu()
        {
            Mod = GangWarSandbox.Instance; // prevent a fatal crash

            if (Mod == null) return null;

            NativeMenu gamemodeMenu = new NativeMenu("Gamemode Settings", "Gamemode Settings", "Modify the settings of your Survival gamemode, such as the factions hunting you.");
            BattleSetupUI.MenuPool.Add(gamemodeMenu);
            var enemyTeam = new NativeListItem<string>($"Hunter Faction", Mod.Factions.Keys.ToArray());
            enemyTeam.Description = "The team that will be your enemy in this gamemode. This will determine the models, weapons, vehicles, and other attributes of the enemy team.";

            enemyTeam.ItemChanged += (item, args) =>
            {
                var selectedFaction = enemyTeam.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[0], selectedFaction);
                }
            };


            gamemodeMenu.Add(enemyTeam);
            return gamemodeMenu;
        } 

        public override void OnTickGameRunning()
        {
            TimeElapsed = Game.GameTime - TimeStart;
        }

        public override void OnPedKilled(Ped ped, Team teamOfPed)
        {
            Ped killer = (Ped)(ped.Killer);

            if (killer != Game.Player.Character) return;

            PlayerScore += 0.02 * ped.MaxHealth * Combo;

            // Increase combo if the player has killed another ped within 2 seconds
            if (ComboLastTime > Game.GameTime - 2000)
            {
                Combo++;
            }
            else Combo = 0;

            ComboLastTime = Game.GameTime;
        }

    }
}
