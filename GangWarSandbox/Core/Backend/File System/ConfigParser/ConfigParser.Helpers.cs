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
        public static void ReloadAll()
        {
            VehicleSets.Clear();

            Logger.Parser("Reloading all files...");
            GangWarSandbox.Instance.Factions = LoadFactions();
            LoadConfiguration();
            Logger.Parser("Reload complete");

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
                    Logger.Parser($"Invalid vehicle model '{list[i]}' in vehicle set from file '{file}'. Skipping this model to prevent errors.");
                    continue;
                }

                currentSet.Add(model);
            }
        }

        private static bool CheckRequirements(string fileName, string fileType, params bool[] checkedValues)
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
                Logger.Parser($"Missing " + counter + " required field(s) in " + fileType + " file " + fileName + ". Please ensure all required fields are set.");
                return false;
            }
            else
            {
                Logger.Log($"Successfully parsed {fileType} file {fileName}.");
                return true;
            }

        }



    }
}
