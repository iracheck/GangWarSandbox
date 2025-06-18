using GangWarSandbox.Core;
using GTA;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox.Core.Backend
{
    static class ConfigParser 
    {
        private static Dictionary<string, VehicleSet> VehicleSets = new Dictionary<string, VehicleSet>();

        /// <summary>
        /// Vehicle Sets are loaded during the initialization of the mod, before initializing factions. 
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, VehicleSet> LoadVehicleSets()
        {
            try
            {
                string path = "scripts/GangWarSandbox/VehicleSets";
                string[] files = Directory.GetFiles(path, "*.ini");

                foreach (var file in files)
                {
                    var lines = File.ReadAllLines(file);
                    if (lines.Length == 0) continue; // skip empty files

                    VehicleSet currentSet = null;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line[0].Equals("=") || !line.Contains("=") && !line.StartsWith("[")) continue;

                        if (line.StartsWith("["))
                        {
                            string Name = line.Trim('[', ']').ToLower();
                            currentSet = new VehicleSet();
                            VehicleSets[Name] = currentSet;
                            continue;
                        }

                        if (currentSet == null) continue;

                        int equalsIndex = line.IndexOf('=');
                        if (equalsIndex == -1 || equalsIndex == line.Length - 1) continue; // skip invalid lines

                        string key = line.Substring(0, equalsIndex).Trim();
                        string value = line.Substring(equalsIndex + 1).Trim(); // gets everything after the equals sign

                        switch (key)
                        {
                            case "Vehicles":
                                currentSet.Vehicles = value.Split(',').Select(s => s.Trim()).ToList();
                                break;
                            case "WeaponizedVehicles":
                                currentSet.WeaponizedVehicles = value.Split(',').Select(s => s.Trim()).ToList();
                                break;
                            case "Helicopters":
                                currentSet.Helicopters = value.Split(',').Select(s => s.Trim()).ToList();
                                break;
                        }

                        // Note: It's important to later set the faction of the vehicle set, when the vehicle set is actually initialized onto a team
                    }
                }
            }
            catch
            {
                ThrowParserError("Error parsing INI. Please check your configuration file definitions. GangWarSandbox/*/*.ini");
                return null;
            }

            return VehicleSets;
        }

        public static Dictionary<string, Faction> LoadFactions()
        {
            LoadVehicleSets(); // vehicle sets should be loaded first, to be applied to factions
            Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

            try
            {
                string path = "scripts/GangWarSandbox/Factions";

                string[] files = Directory.GetFiles(path, "*.ini");


                foreach (var file in files)
                {
                    string currentFaction = null;
                    Faction faction = null;

                    var lines = File.ReadAllLines(file);
                    if (lines.Length == 0) continue; // skip empty files

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("=") && !line.StartsWith("[")) continue;

                        if (line.StartsWith("["))
                        {
                            currentFaction = line.Trim('[', ']');
                            faction = new Faction { Name = currentFaction };
                            Factions[currentFaction] = faction;
                            continue;
                        }

                        if (faction == null) continue;

                        int equalsIndex = line.IndexOf('=');
                        if (equalsIndex == -1 || equalsIndex == line.Length - 1) continue; // skip invalid lines

                        string key = line.Substring(0, equalsIndex).Trim();
                        string value = line.Substring(equalsIndex + 1).Trim();

                        switch (key)
                        {
                            case "Models":
                                faction.Models = value.Split(',').Select(s => s.Trim()).ToArray();
                                break;
                            case "Tier4Model":
                                faction.Tier4Model = value;
                                break;
                            case "Tier1Weapons":
                                faction.Tier1Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                break;
                            case "Tier2Weapons":
                                faction.Tier2Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                break;
                            case "Tier3Weapons":
                                faction.Tier3Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                break;
                            case "MaxSoldiers":
                                if (int.TryParse(value, out int soldiers))
                                    faction.MaxSoldiers = soldiers;
                                break;
                            case "VehicleSet":
                                if (VehicleSets.TryGetValue(value.ToLower(), out var vehicleSet))
                                {
                                    faction.VehicleSet = vehicleSet;
                                }
                                break;
                            case "BaseHealth":
                                if (int.TryParse(value, out int health))
                                    faction.BaseHealth = health;
                                break;
                            case "AccuracyBonus":
                                if (int.TryParse(value, out int accuracy))
                                    faction.Accuracy = accuracy;
                                break;
                            case "TierUpgradeMultiplier":
                                if (float.TryParse(value, out float mult))
                                    faction.TierUpgradeMultiplier = mult;
                                break;
                            case "BlipColor":
                                if (Enum.TryParse(value, out BlipColor blipColor))
                                    faction.Color = blipColor;
                                break;
                        }
                    }
                }
            }
            catch
            {
                ThrowParserError("Error parsing INI. Please check your configuration file definitions. GangWarSandbox/*/*.ini");
                return null;
            }

            return Factions;
        }

        private static void ThrowParserError(string type, string file = "unknown", string description = "")
        {
            GTA.UI.Screen.ShowHelpText("GangWarSandbox: There was a critical error in parsing the configuration file " + file + "." +
                "Please check your configuration file definitions. \nAdditional Info: " + description);
        }
    }
}
