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
        private static Dictionary<string, VehicleSet> VehicleSets = new Dictionary<string, VehicleSet>();

        /// <summary>
        /// Vehicle Sets are loaded during the initialization of the mod, before initializing factions. 
        /// </summary>
        private static Dictionary<string, VehicleSet> LoadVehicleSets()
        {
            Logger.Parser("Parsing VehicleSets from INI files...");
            try
            {
                string path = "scripts/GangWarSandbox/VehicleSets";
                string[] files = Directory.GetFiles(path, "*.ini");

                foreach (var file in files)
                {
                    Logger.Parser("Trying to parse new vehicle set file: " + file);

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
                                Logger.Parser($"Unknown key '{key}' in vehicle set file {file}.");
                                break;
                        }

                    }

                    if (currentSet != null)
                    {
                        // Check if all required fields were set
                        if (currentSet != null)
                        {
                            bool result = CheckRequirements(file, "VehicleSet", vehicles || weaponizedVehicles || helicopters);

                            if (!result) VehicleSets.Remove(Name);
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

    }
}
