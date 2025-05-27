using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GangWarSandbox {

    internal static class SquadManager
    {
        static int[] squadCount = new int[6];

        static List<Squad>[] squads = new List<Squad>[6];

        public static void TrySpawnSquads()
        {
            for (int i = 0; i < GangWarSandbox.Teams.Count; i++)
            {

            }
        }

        public static void ClearEmptySquads()
        {
            for (int i = 0; i < squads.Length; i++)
            {
                for (int j = 0; j < squads[i].Count; j++)
                {
                    if (squads[i][j].isEmpty())
                    {
                        squads[i].Remove(squads[i][j]);
                    }
                }
            }
        }
    }
}
