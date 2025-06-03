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
    }
}
