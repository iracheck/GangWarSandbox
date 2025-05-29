using GTA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox
{
    public class Faction
    {


        public string Name;
        public string[] Models;
        public string Tier4Model;
        public string[] Tier1Weapons;
        public string[] Tier2Weapons;
        public string[] Tier3Weapons;

        public BlipColor Color = BlipColor.White;

        public int MaxSoldiers;
        public int BaseHealth;
        public int Accuracy;
        public float TierUpgradeMultiplier;
    }
}
