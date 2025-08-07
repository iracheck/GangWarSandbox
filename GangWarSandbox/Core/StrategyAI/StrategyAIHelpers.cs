using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GangWarSandbox;
using GangWarSandbox.Peds;
using GTA;

namespace GangWarSandbox.Core.StrategyAI
{
    static class StrategyAIHelpers
    {
        static GangWarSandbox ModData = GangWarSandbox.Instance;

        static public int GetNumberOfSquadsWithRole(Team team, Squad.SquadRole role)
        {
            int count = 0;

            foreach (var squad in team.Squads)
            {
                if (squad == null) continue;

                if (squad.Role == role)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Calculates the need for a team to put additional squads on taking capture points. This method is used within Strategy AI calculations for deciding what role to give new squads.
        /// </summary>
        static public int CalculateNeedToAssaultPoint(Team ownTeam)
        {
            List<CapturePoint> hostilePoints = new List<CapturePoint>();

            // Collect all non-owned capture points
            foreach (var point in ModData.CapturePoints)
            {
                if (point == null) continue;

                if (point.Owner != ownTeam || point.Owner == null)
                {
                    hostilePoints.Add(point);
                }
            }

            if (hostilePoints.Count == 0) return 0; // No hostile capture points

            float percentOwned = 1.0f - (hostilePoints.Count / ModData.CapturePoints.Count);

            int squadsWithRole = GetNumberOfSquadsWithRole(ownTeam, Squad.SquadRole.AssaultCapturePoint);

            // Calculate the need based on the percentage of owned points
            if (percentOwned == 0 && squadsWithRole < 4)
            {
                return 60;
            }
            else if (squadsWithRole == 0) // try to have atleast one squad that is assaulting
            {
                return 50;
            }
            else if (percentOwned <= 20f && squadsWithRole < 4)
            {
                return 45;
            }
            else if (percentOwned <= 50f && squadsWithRole < 4)
            {
                return 20;
            }
            else if (percentOwned <= 80f && squadsWithRole < 3)
            {
                return 5;
            }
            else
            {
                return 0; // More than 80% owned, no need for additional focus
            }
        }

        /// <summary>
        /// Calculates the need for a team to put additional squads on defending capture points. This method is used within Strategy AI calculations for deciding what role to give new squads.
        /// </summary>
        static public int CalculateNeedToDefendPoint(Team ownTeam)
        {
            List<CapturePoint> friendlyPoints = new List<CapturePoint>();

            // Collect all non-owned capture points
            foreach (var point in ModData.CapturePoints)
            {
                if (point == null) continue;

                if (point.Owner == ownTeam)
                {
                    friendlyPoints.Add(point);
                }
            }

            float percentOwned = (float)friendlyPoints.Count/ModData.CapturePoints.Count;
            int squadsWithRole = GetNumberOfSquadsWithRole(ownTeam, Squad.SquadRole.DefendCapturePoint);

            // Calculate the need based on the percentage of owned points
            if (percentOwned == 0 || squadsWithRole > 3)
            {
                return 0;
            }
            else if (percentOwned >= 80)
            {
                if (squadsWithRole < 3)
                {
                    return 30; // Need more squads to defend
                }
                else
                {
                    return 0;
                }
            }
            else if (percentOwned >= 50 && squadsWithRole < 3)
            {
                return 15;
            }
            else if (percentOwned >= 30 && squadsWithRole == 1)
            {
                return 5; // not much to defend, so no prioritization neded
            }
            else
            {
                return 0; // More than 80% owned, no need for additional focus
            }
        }
    }
}