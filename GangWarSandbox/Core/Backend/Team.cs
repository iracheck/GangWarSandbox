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

namespace GangWarSandbox
{
    public class Team
    {
        public string Name { get; }
        public RelationshipGroup Group { get; set; }
        public Faction Faction { get; set; } = null;

        public int MAX_SOLDIERS { get; set; } = 25;
        public int BaseHealth { get; set; } = 300;
        public int Accuracy { get; set; } = 5;
        public List<Vector3> SpawnPoints { get; } = new List<Vector3>();

        public List<Squad> Squads = new List<Squad>();
        public List<Ped> DeadPeds { get; } = new List<Ped>();
        public Ped Tier4Ped = null;


        public List<Blip> Blips { get; } = new List<Blip>();

        public string[] Models { get; set; } = Array.Empty<string>();
        public string[] Tier1Weapons { get; set; } = Array.Empty<string>();
        public string[] Tier2Weapons { get; set; } = Array.Empty<string>();
        public string[] Tier3Weapons { get; set; } = Array.Empty<string>();

        public float TierUpgradeMultiplier;

        public BlipColor BlipColor { get; set; } = BlipColor.White;
        public BlipSprite BlipSprite { get; set; } = BlipSprite.Standard;

        public Team(string name)
        {
            Name = name;
            Group = World.AddRelationshipGroup(name);
        }

        public int GetSquadSize()
        {
            int squadSize = Faction.MaxSoldiers / 5;

            if (squadSize > 6) squadSize = 6;
            if (squadSize < 2) squadSize = 2;

            return squadSize;
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
                if (squad.isEmpty()) continue;

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
                if (squad.isEmpty()) squad.Destroy();

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
