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
        int CurrentWave = 0; // Current level of the survival gamemode, used for difficulty scaling

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

            var level1Enemy = new NativeListItem<string>($"Tier 1 Hunter Faction", Mod.Factions.Keys.ToArray());
            level1Enemy.Description = "The primary team that appears to hunt you. These will always appear.";

            level1Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level1Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[0], selectedFaction);
                }
            };

            var level2Enemy = new NativeListItem<string>($"Tier 2 Hunter Faction", Mod.Factions.Keys.ToArray());
            level2Enemy.Description = "The second team that appears to hunt you. These will only appear starting in later rounds.";

            level2Enemy.ItemChanged += (item, args) =>
            {
                var selectedFaction = level1Enemy.SelectedItem;
                if (selectedFaction != null && Mod.Factions.ContainsKey(selectedFaction))
                {
                    Mod.ApplyFactionToTeam(Mod.Teams[0], selectedFaction);
                }
            };

            var missions = new NativeCheckboxItem("Missions", "Missions are a set of objectives that can be completed to earn extra points, or weapons/ammo/vehicles.", false);
            missions.Enabled = false; // Missions are not implemented yet

            gamemodeMenu.Add(level1Enemy);
            gamemodeMenu.Add(level2Enemy);
            gamemodeMenu.Add(missions);
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

            PlayerScore += 0.02 * ped.MaxHealth * Combo * Math.Pow(CurrentWave, 1.2); ;

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
