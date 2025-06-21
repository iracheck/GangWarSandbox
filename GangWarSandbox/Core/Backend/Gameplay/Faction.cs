using GangWarSandbox.Core;
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
        public string Tier4Model; // default will use one of the regular models (not req field)
        public string[] Tier1Weapons;
        public string[] Tier2Weapons;
        public string[] Tier3Weapons;

        public BlipColor Color = BlipColor.White;
        public VehicleSet VehicleSet = new VehicleSet(); // defaults an empty vehicle set (not req field) 

        public int MaxSoldiers;
        public int BaseHealth;
        public int Accuracy;
        public float TierUpgradeMultiplier = 1.0f; // default of 1.0f (not req field) 
    }
}
