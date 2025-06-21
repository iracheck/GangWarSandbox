using GangWarSandbox.Core;
using GTA;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GangWarSandbox.Core
{
    static class ConfigParser 
    {
        private static Dictionary<string, VehicleSet> VehicleSets = new Dictionary<string, VehicleSet>();

        // Purging comments means that anything following the hashtag is removed.
        // e.g. "VehicleSet = MySet # This is a comment" becomes "VehicleSet = MySet"
        // "# VehicleSet = MySet" becomes an empty string-- nothing happens!
        private static string PurgeComments(string line)
        {
            int commentIndex = line.IndexOf('#');
            if (commentIndex != -1)
            {
                return line.Substring(0, commentIndex).Trim(); // remove comments and trim whitespace
            }
            return line.Trim();
        }

        /// <summary>
        /// Vehicle Sets are loaded during the initialization of the mod, before initializing factions. 
        /// </summary>
        private static Dictionary<string, VehicleSet> LoadVehicleSets()
        {
            Logger.Log("Parsing VehicleSets from INI files...");
            try
            {
                string path = "scripts/GangWarSandbox/VehicleSets";
                string[] files = Directory.GetFiles(path, "*.ini");

                foreach (var file in files)
                {
                    Logger.Log("Trying to parse new vehicle set file: " + file);
                    var lines = File.ReadAllLines(file);
                    if (lines.Length == 0) continue; // skip empty files

                    VehicleSet currentSet = null;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line[0].Equals("=") || !line.Contains("=") && !line.StartsWith("[")) continue;

                        string thisLine = PurgeComments(line);

                        if (thisLine.StartsWith("["))
                        {
                            string Name = thisLine.Trim('[', ']').ToLower();
                            currentSet = new VehicleSet();
                            VehicleSets[Name] = currentSet;
                            continue;
                        }

                        

                        if (currentSet == null) continue;

                        int equalsIndex = thisLine.IndexOf('=');
                        if (equalsIndex == -1 || equalsIndex == thisLine.Length - 1) continue; // skip invalid lines

                        string key = thisLine.Substring(0, equalsIndex).Trim();
                        string value = thisLine.Substring(equalsIndex + 1).Trim(); // gets everything after the equals sign

                        bool vehicles = false,
                            weaponizedVehicles = false,
                            helicopters = false;

                        switch (key)
                        {
                            case "Vehicles":
                                currentSet.Vehicles = value.Split(',').Select(s => s.Trim()).ToList();
                                vehicles = true;
                                break;
                            case "WeaponizedVehicles":
                                currentSet.WeaponizedVehicles = value.Split(',').Select(s => s.Trim()).ToList();
                                weaponizedVehicles = true;
                                break;
                            case "Helicopters":
                                currentSet.Helicopters = value.Split(',').Select(s => s.Trim()).ToList();
                                helicopters = true;
                                break;
                            default:
                                // Handle any other keys you might want to add in the future
                                Logger.ParserError($"Unknown key '{key}' in vehicle set file '{file}'.");
                                break;
                        }

                        // Check if all required fields were set
                        if (currentSet != null)
                        {
                            if (!(vehicles || weaponizedVehicles || helicopters))
                            {
                                Logger.ParserError($"Missing any fields in vehicle set from file '{file}'.");
                                VehicleSets.Remove(currentSet.Vehicles.FirstOrDefault().ToLower()); // remove the set if it has no valid vehicles
                            }
                            else
                            {
                                Logger.Log($"Successfully parsed vehicle set from file '{file}'.");
                            }
                        }
                        else
                        {
                            Logger.ParserError($"No valid vehicle set data found in file '{file}'.");
                        }

                        // Note: It's important to later set the faction of the vehicle set, when the vehicle set is actually initialized onto a team
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ParserError("Error in .ini definitions for VehicleSets. Please check your configuration file definitions. GangWarSandbox/VehicleSets/*.ini \nMore Info: " + ex);
                return null;
            }

            return VehicleSets;
        }

        public static Dictionary<string, Faction> LoadFactions()
        {
            LoadVehicleSets(); // vehicle sets should be loaded first, to be applied to factions

            Logger.Log("Parsing Factions from INI files...");
            Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

            try
            {
                string path = "scripts/GangWarSandbox/Factions";

                string[] files = Directory.GetFiles(path, "*.ini");


                foreach (var file in files)
                {
                    Logger.Log("Trying to parse new faction file: " + file);
                    string currentFaction = null;
                    Faction faction = null;

                    var lines = File.ReadAllLines(file);
                    if (lines.Length == 0) continue; // skip empty files

                    // Required fields for a faction
                    // Not required fields: Tier4Model, VehicleSet, TierUpgradeMultiplier
                    bool models = false,
                         t1wp = false,
                         t2wp = false,
                         t3wp = false,
                         maxPeds = false,
                         bsHP = false,
                         accBonus = false,
                         blpClr = false;

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("=") && !line.StartsWith("[")) continue;

                        string thisLine = PurgeComments(line);

                        if (thisLine.StartsWith("["))
                        {
                            currentFaction = thisLine.Trim('[', ']');
                            faction = new Faction { Name = currentFaction };
                            Factions[currentFaction] = faction;
                            continue;
                        }

                        if (faction == null) continue;

                        int equalsIndex = thisLine.IndexOf('=');
                        if (equalsIndex == -1 || equalsIndex == thisLine.Length - 1) continue; // skip invalid lines

                        string key = thisLine.Substring(0, equalsIndex).Trim();
                        string value = thisLine.Substring(equalsIndex + 1).Trim();

                        switch (key)
                        {
                            case "Models":
                                faction.Models = value.Split(',').Select(s => s.Trim()).ToArray();
                                models = true;
                                break;
                            case "Tier4Model":
                                faction.Tier4Model = value;
                                break;
                            case "Tier1Weapons":
                                faction.Tier1Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                t1wp = true;
                                break;
                            case "Tier2Weapons":
                                faction.Tier2Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                t2wp = true;
                                break;
                            case "Tier3Weapons":
                                faction.Tier3Weapons = value.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                                t3wp = true;
                                break;
                            case "MaxSoldiers":
                                if (int.TryParse(value, out int soldiers))
                                    faction.MaxSoldiers = soldiers;
                                maxPeds = true;
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
                                bsHP = true;
                                break;
                            case "AccuracyBonus":
                                if (int.TryParse(value, out int accuracy))
                                    faction.Accuracy = accuracy;
                                accBonus = true;
                                break;
                            case "TierUpgradeMultiplier":
                                if (float.TryParse(value, out float mult))
                                    faction.TierUpgradeMultiplier = mult;
                                break;
                            case "BlipColor":
                                if (Enum.TryParse(value, out BlipColor blipColor))
                                    faction.Color = blipColor;
                                blpClr = true;
                                break;
                            default:
                                // Handle any other keys you might want to add in the future
                                Logger.Log($"Unknown key '{key}' in faction file '{file}'.");
                                break;
                        }

                        // Check if all required fields were set
                        if (faction != null)
                        {
                            if (!models || !t1wp || !t2wp || !t3wp || !maxPeds || !bsHP || !accBonus || !blpClr)
                            {
                                Logger.ParserError($"Missing required fields in faction '{currentFaction}' from file '{file}'.");
                                Factions.Remove(currentFaction);
                            }
                            else
                            {
                                Logger.Log($"Successfully parsed faction '{currentFaction}' from file '{file}'.");
                            }
                        }
                        else
                        {
                            Logger.ParserError($"No valid faction data found in file '{file}'.");
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                Logger.ParserError("Error in .ini definitions for Factions. Please check your configuration file definitions. GangWarSandbox/Factions/*.ini \nMore Info: " + ex);
                return null;
            }

            return Factions;
        }
    }
}
