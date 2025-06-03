using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GangWarSandbox;
using System.Windows.Forms;

namespace GangWarSandbox
{
    public class CapturePoint
    {
        // Helper classes
        static GangWarSandbox ModData = GangWarSandbox.Instance;
        static Random rand = new Random();

        public int PointID; // Unique ID for the capture point
        public Blip PointBlip;
        public Team Owner;
        public Vector3 Location;

        public float Radius = 10.0f; // Radius of the capture point area

        // Capture Information
        public const float CAPTURE_RATE = 50f; // Rate at which capture progresses per second, per ped

        public float CaptureProgress; // Progress of capture from 0 to 100
        public Team CaptureTeam; // Team currently capturing the point, null if not being captured
        public bool IsCapturing; // Whether the point is currently being captured
        public bool IsContested; // Whether the point is contested by multiple teams

        // Capture Point Benefits
        public int HEALING_RATE = 25; // per second


        // Capture Point Icons
        BlipSprite[] CapturePointIcons =
        {
            BlipSprite.TargetA,
            BlipSprite.TargetB,
            BlipSprite.TargetC,
            BlipSprite.TargetD,
            BlipSprite.TargetE,
            BlipSprite.TargetF,
            BlipSprite.TargetG,
            BlipSprite.TargetH
        };

        BlipSprite FallbackIcon = BlipSprite.CaptureFlag; // Fallback icon if no specific icon is set for the point


        public Dictionary<Team, int> PedsNearby = new Dictionary<Team, int>(); // Track how many squad members are nearby

        public CapturePoint(Vector3 location, Team startingOwner = null)
        {
            Location = location;
            Owner = startingOwner; // Teams will only own points initially in certain gamemodes

            ResetCaptureProgress(); // Ensure capture progress is reset when created

            PointBlip = World.CreateBlip(Location);

            PointBlip.Position = Location; // Set the blip position to the capture point location

            PointID = ModData.CapturePoints.Count + 1;
            PointBlip.Name = "Capture Point " + PointID; // Set the blip name

            if (PointID >= CapturePointIcons.Length) PointBlip.Sprite = FallbackIcon; 
            else PointBlip.Sprite = CapturePointIcons[PointID - 1];
        }

        public void CapturePointHandler()
        {
            CaptureProgress = Helpers.Clamp(CaptureProgress); // ensure its between 0 and 100
            PedsNearby = PedAI.GetNearbyPeds(Location, Radius); // update PedsNearby

            int numTeamsNearby = PedsNearby.Count(team => team.Value > 0); // Count how many teams have peds nearby

            // If a team is capturing it, but it has no peds nearby
            if (CaptureTeam != null && PedsNearby[CaptureTeam] == 0)
            {
                CaptureTeam = null;
            }

            for (int i = 0; i < ModData.Teams.Count; i++)
            {
                Team team = ModData.Teams[i];

                if (team.SpawnPoints.Count == 0) continue; // Skip teams with no spawn points

                if (!PedsNearby.ContainsKey(team) || PedsNearby[team] == 0) continue;
                
                if (numTeamsNearby > 1) IsContested = true; // If multiple teams are nearby, the point is contested
                else if (CaptureTeam == null && numTeamsNearby == 1) // Start capturing, if the team has peds nearby and Capture Team is null
                {
                    CaptureTeam = team;
                    IsCapturing = true; // Start capturing if no team is currently capturing
                    CaptureProgress = 0.0f;
                    IsContested = false; // Reset contested state
                }
                else if (CaptureTeam == team && !IsContested && PedsNearby[team] > 0)
                {
                    // If the same team is capturing and has peds nearby, increase capture progress
                    CaptureProgress += PedsNearby[team] * CAPTURE_RATE;
                }
                else if (CaptureTeam == team && PedsNearby[team] == 0) {
                    // If the same team is capturing but has no peds nearby, stop capturing
                    ResetCaptureProgress();
                }
                else if (CaptureTeam == team && !IsContested && CaptureProgress == 100f) // if the team has finished capturing the point
                {
                    Owner = CaptureTeam;

                    PointBlip.Color = Owner.BlipColor;

                    ResetCaptureProgress();
                    GTA.UI.Screen.ShowSubtitle("Capture Point " + PointID + " captured by " + Owner.Name, 5000);
                }
                else
                {
                    // Implement healing logic
                }

                if (IsContested || IsCapturing)
                {
                    // Implement pulsing logic for the blip
                }

            }
        }

        public void ResetCaptureProgress()
        {
            CaptureTeam = null;
            IsCapturing = false;
            IsContested = false;
            CaptureProgress = 0.0f; // Reset capture progress
        }

    }
}

