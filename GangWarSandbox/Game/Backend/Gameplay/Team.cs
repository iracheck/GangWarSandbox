using GTA;
using GTA.Native;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LemonUI;
using LemonUI.Menus;
using GangWarSandbox.Core;
using GangWarSandbox.Peds;
using System.Drawing;

namespace GangWarSandbox
{
    public class Team
    {
        static GangWarSandbox ModData = GangWarSandbox.Instance;

        public string Name { get; }
        public RelationshipGroup Group { get; set; }

        public Faction Faction { get; set; } = null;
        public VehicleSet TeamVehicles { get; set; } = null;

        public int MAX_SOLDIERS { get; set; } = 25;
        public int BaseHealth { get; set; } = 300;
        public int Accuracy { get; set; } = 5;
        public List<Vector3> SpawnPoints { get; } = new List<Vector3>();

        public List<Squad> Squads = new List<Squad>();
        public List<Squad> VehicleSquads = new List<Squad>();
        public List<Squad> WeaponizedVehicleSquads = new List<Squad>();
        public List<Squad> HelicopterSquads = new List<Squad>();

        public List<Ped> DeadPeds { get; } = new List<Ped>();
        public Ped Tier4Ped = null;

        public int TeamIndex;


        public List<Blip> Blips { get; } = new List<Blip>();

        public string[] Models { get; set; } = Array.Empty<string>();
        public string[] Tier1Weapons { get; set; } = Array.Empty<string>();
        public string[] Tier2Weapons { get; set; } = Array.Empty<string>();
        public string[] Tier3Weapons { get; set; } = Array.Empty<string>();
        public float TierUpgradeMultiplier;

        public Color GenericColor { get; set; } = Color.White;
        public BlipColor BlipColor { get; set; } = BlipColor.White;
        public BlipSprite BlipSprite { get; set; } = BlipSprite.Standard;





        public Team(string name)
        {
            Name = name;
            Group = World.AddRelationshipGroup(name);
        }

        public int GetSquadSize()
        {
            if (Faction == null || Faction.MaxSoldiers <= 0)
            {
                GTA.UI.Screen.ShowSubtitle("Faction is not set or has no soldiers available.");
                return 5; // default
            }

            int squadSize = (int) (Faction.MaxSoldiers * ModData.CurrentGamemode.UnitCountMultiplier) / 5;

            if (squadSize > 6) squadSize = 6;
            if (squadSize < 2) squadSize = 2;

            return squadSize;
        }

        public List<Squad> GetAllSquads()
        {
            List<Squad> squads = new List<Squad>();

            squads.AddRange(Squads);
            squads.AddRange(VehicleSquads);
            squads.AddRange(WeaponizedVehicleSquads);
            squads.AddRange(HelicopterSquads);

            return squads;
        }

        public int GetMaxNumPeds()
        {
            return (int) (MAX_SOLDIERS * ModData.CurrentGamemode.UnitCountMultiplier);
        }

        public bool ShouldSpawnVehicle()
        {
            int numVehiclePeds = 0;
            Random rand = new Random();

            

            foreach (var squad in VehicleSquads.ToList())
            {
                if (squad.IsEmpty() || (squad.SquadVehicle != null && squad.SquadVehicle.IsDead))
                {
                    VehicleSquads.Remove(squad);
                }
                else
                {
                    numVehiclePeds += squad.Members.Count(ped => ped.Exists() && !ped.IsDead);
                }

                
            }
            if (TeamVehicles == null)
            {
                Logger.LogDebug("CRITICAL: TeamVehicles of team " + Name + " is null. This should never happen.");
                return false;
            }
            else if (TeamVehicles.Vehicles.Count == 0 && TeamVehicles.WeaponizedVehicles.Count == 0 && TeamVehicles.Helicopters.Count == 0)
            {
                Logger.LogDebug("TeamVehicles of team " + Name + " is empty!");
                return false; // no vehicles available
            }
            else if (numVehiclePeds < (GetMaxNumPeds() * 0.15f))
            {
                double rnum = rand.NextDouble();

                // On top of strict requirements for vehicle squads (15% of total peds), also only percent chance of them spawning this "spawn tick"
                if (rnum >= 0.75)
                {
                    return true;
                }
                else return false;
            }

            return false;
        }


        public List<Ped> GetAllPeds()
        {
            List<Ped> allPeds = new List<Ped>();

            foreach (var squad in Squads)
            {
                if (squad.IsEmpty()) continue;
                else allPeds.AddRange(squad.Members);
            }

            return allPeds;
        }

        public void RecolorBlips()
        {
            foreach (var blip in Blips)
            {
                blip.Color = BlipColor;
            }
        }

        public void DestroySquads()
        {
            foreach (var squad in Squads)
            {
                if (squad.IsEmpty()) continue;

                foreach (var ped in squad.Members)
                {
                    if (ped.Exists())
                    {
                        ped.Delete();
                    }
                }
                squad.Members.Clear();
                squad.SquadLeader = null;
            }
        }


        public void AddSpawnpoint(Vector3 position)
        {
            SpawnPoints.Add(position);
            Blip blip = World.CreateBlip(position);

            blip.Sprite = BlipSprite;
            blip.Name = "Team " + Name + " Spawn";
            blip.Color = BlipColor;
            blip.Scale = 0.8f;

            Blips.Add(blip);

        }

        public void Cleanup()
        {
            foreach (var squad in Squads)
                if (squad.IsEmpty()) squad.Destroy();

            foreach (var ped in DeadPeds)
                if (ped.Exists()) ped.Delete();

            foreach (var blip in Blips)
                if (blip.Exists()) blip.Delete();

            Blips.Clear();
            Squads.Clear();
            SpawnPoints.Clear();
        }
    }
}
