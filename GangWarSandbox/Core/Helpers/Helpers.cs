using GTA.Math;
using GTA;
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

        private static Vector3 FindRandomEnemySpawnpoint(Team team)
        {
            for (int i = 0; i < Teams.Count; i++)
            {
                if (Teams[i] == team)
                {
                    continue;
                }
                else
                {
                    return Teams[i].SpawnPoints[rand.Next(Teams[i].SpawnPoints.Count)];
                }
            }
            return new Vector3(0, 0, 0);
        }


        private static Ped FindNearbyEnemy(Ped self, Team team)
        {
            Team enemyTeam = Teams.FirstOrDefault(t => t != team);
            if (enemyTeam == null)
                return null;

            const float searchRadius = 80f;

            return enemyTeam.Peds
                .Where(p => p.Exists() && !p.IsDead && p.Position.DistanceTo(self.Position) <= searchRadius)
                .OrderBy(p => p.Position.DistanceTo(self.Position))
                .FirstOrDefault();
        }

    }
}
