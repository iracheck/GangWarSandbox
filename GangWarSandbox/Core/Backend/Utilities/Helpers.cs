using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox
{
    static class Helpers
    {
        static Random rand = new Random();

        public static string GetRandom(string[] array)
        {
            return array != null && array.Length > 0 ? array[rand.Next(array.Length)] : null;
        }

        public static int Clamp(int num, int max = 0, int min = 100)
        {
            if (num > max) return max;
            if (num < min) return min;
            else return num;
        }

        public static float Clamp(float num, float max = 0, float min = 100)
        {
            if (num > max) return max;
            if (num < min) return min;
            else return num;
        }

        public static double RoundToNearestTen(double num)
        {
            return Math.Round(num / 10) * 10;
        }

        /// <summary>
        /// Generates a random number from 0 to 100. If the value is less than parameter "chance," returns false, otherwise true.
        /// </summary>
        /// <param name="chance"></param>
        /// <returns></returns>
        public static bool RandomChance(int chance)
        {
            return rand.Next(0, 100) > chance;
        }
    }
}
