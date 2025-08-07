using GangWarSandbox.Core;
using GangWarSandbox.Utilities;
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
    static partial class ConfigParser
    {
        public static Dictionary<string, Faction> LoadFactions()
        {
            LoadVehicleSets(); // vehicle sets should be loaded first, to be applied to factions

            Logger.Parser("Parsing Factions from INI files...");
            Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

            try
            {
                string path = "scripts/GangWarSandbox/Factions";

                string[] files = Directory.GetFiles(path, "*.ini");


                foreach (var file in files)
                {
                    Logger.Parser("Trying to parse new faction file: " + file);
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
                                else
                                {
                                    Logger.ParserError("Error in faction definition of " + currentFaction + "! VehicleSet " + value + " is not a valid VehicleSet.");
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
                            default:
                                Logger.Parser($"Unknown key '{key}' in faction file '{file}'.");
                                break;
                        }


                    }

                    // Check if all required fields were set
                    if (faction != null)
                    {
                        bool result = CheckRequirements(file, "FACTION", models, t1wp, t2wp, t3wp, maxPeds, bsHP, accBonus);

                        if (!result) Factions.Remove(currentFaction);
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

    }
}
