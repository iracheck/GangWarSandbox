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
                    Logger.ParserError("Trying to parse new vehicle set file: " + file);

                    var lines = File.ReadAllLines(file);
                    if (lines.Length == 0) continue; // skip empty files

                    VehicleSet currentSet = null;
                    string Name = null;

                    bool vehicles = false,
                        weaponizedVehicles = false,
                        helicopters = false;

                    foreach (var line in lines)
                    {

                        if (string.IsNullOrWhiteSpace(line) || line[0].Equals("=") || !line.Contains("=") && !line.StartsWith("[")) continue;

                        string thisLine = PurgeComments(line); // remove comments and trim whitespace

                        if (thisLine.StartsWith("["))
                        {
                            if (currentSet != null)
                            {
                                CheckRequirements(file, "VehicleSet", vehicles || weaponizedVehicles || helicopters);
                                vehicles = weaponizedVehicles = helicopters = false;
                            }

                            Name = thisLine.Trim('[', ']').ToLower();
                            currentSet = new VehicleSet();
                            VehicleSets[Name] = currentSet;
                            continue;
                        }



                        if (currentSet == null) continue;

                        int equalsIndex = thisLine.IndexOf('=');
                        if (equalsIndex == -1 || equalsIndex == thisLine.Length - 1) continue; // skip lines that do not have an equalsindex or nothing after the equals sign

                        string key = thisLine.Substring(0, equalsIndex).Trim();
                        string value = thisLine.Substring(equalsIndex + 1).Trim(); // gets everything after the equals sign

                        switch (key)
                        {
                            case "Vehicles":
                                AddValidVehicles(value, file, currentSet.Vehicles);
                                vehicles = true;
                                break;
                            case "WeaponizedVehicles":
                                AddValidVehicles(value, file, currentSet.WeaponizedVehicles);
                                weaponizedVehicles = true;
                                break;
                            case "Helicopters":
                                AddValidVehicles(value, file, currentSet.Helicopters);
                                helicopters = true;
                                break;
                            default:
                                Logger.ParserError($"Unknown key '{key}' in vehicle set file {file}.");
                                break;
                        }

                    }

                    if (currentSet != null)
                    {
                        // Check if all required fields were set
                        if (currentSet != null)
                        {
                            CheckRequirements(file, "VehicleSet", vehicles || weaponizedVehicles || helicopters);
                        }
                        else
                        {
                            Logger.ParserError($"No valid vehicle set data found in file '{file}'.");
                        }
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
                    Logger.ParserError("Trying to parse new faction file: " + file);
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
                            if (currentFaction != null) CheckRequirements(file, "FACTION", models, t1wp, t2wp, t3wp, maxPeds, bsHP, accBonus, blpClr);
                            models = t1wp = t2wp = t3wp = maxPeds = bsHP = accBonus = blpClr = false;

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
                                Logger.Log($"Unknown key '{key}' in faction file '{file}'.");
                                break;
                        }


                    }

                    // Check if all required fields were set
                    if (faction != null)
                    {
                        CheckRequirements(file, "FACTION", models, t1wp, t2wp, t3wp, maxPeds, bsHP, accBonus, blpClr);
                    }
                    else
                    {
                        Logger.ParserError($"No valid faction data found in file '{file}'.");
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

        public static void LoadConfiguration()
        {
            Logger.LogDebug("Loading mod config file...");
            try
            {
                string path = ModFiles.ConfigPath;

                string[] file = (string[])File.ReadLines(path);

                foreach (var line in file)
                {
                    Logger.Log("");
                }
            }
            catch (Exception e)
            {
                Logger.ParserError("Failed to parse mod configuration file. Using defaults instead...");
            }
        }

        public static void ReloadAll()
        {
            VehicleSets.Clear();

            Logger.ParserError("Reloading all files...");
            GangWarSandbox.Instance.Factions = LoadFactions();
            LoadConfiguration();
            Logger.ParserError("Reload complete");

        }
        // Below are parser methods used to parse the INI files for vehicle sets and factions that are not directly linked to a specific dataset. E.g. removing comments is a universal requirement



        // Purging comments means that anything following the hashtag is removed.
        // e.g. "VehicleSet = MySet # This is a comment" becomes "VehicleSet = MySet"
        // "# VehicleSet = MySet" becomes an empty string-- nothing happens!

        // This also removes extra whitespace, saving a few bytes of memory.
        private static string PurgeComments(string line)
        {
            int commentIndex = line.IndexOf('#');
            if (commentIndex != -1)
            {
                return line.Substring(0, commentIndex).Trim();
            }
            return line.Trim();
        }

        // Below are helper methods used to verify the information parsed


        private static void AddValidVehicles(string value, string file, List<Model> currentSet)
        {
            string[] list = value.Split(',').Select(s => s.Trim()).ToArray();

            for (int i = 0; i < list.Length; i++)
            {
                Model model = new Model(list[i]);
                if (!model.IsValid || !model.IsVehicle)
                {
                    Logger.ParserError($"Invalid vehicle model '{list[i]}' in vehicle set from file '{file}'. Skipping this model to prevent errors.");
                    continue;
                }

                currentSet.Add(model);
            }
        }

        private static void CheckRequirements(string fileName, string fileType, params bool[] checkedValues)
        {
            int counter = 0;

            foreach (var value in checkedValues)
            {
                if (value != true)
                {
                    counter++;
                }
            }

            if (counter > 0)
            {
                Logger.ParserError($"Missing " + counter + " required field(s) in {fileType} file '{fileName}'. Please ensure all required fields are set.");
            }
            else
            {
                Logger.Log($"Successfully parsed {fileType} file {fileName}.");
            }
        }


    }
}
