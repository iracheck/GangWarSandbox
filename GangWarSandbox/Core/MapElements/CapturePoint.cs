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

        public int PointID; // Unique ID for the capture point-- used to determine sprite and name

        public String Name; // "A", "B", "C", etc.
        public Blip PointBlip;
        public Team Owner;
        public Vector3 Position;

        public float Radius = 10f; // Radius of the capture point area

        // Capture Information
        public const float CAPTURE_RATE = 10f; // Rate at which capture progresses per second, per ped

        public float CaptureProgress; // Progress of capture from 0 to 100
        public Team CaptureTeam; // Team currently capturing the point, null if not being captured
        public bool IsCapturing; // Whether the point is currently being captured
        public bool IsContested; // Whether the point is contested by multiple teams

        // Capture Point Benefits
        public const int HEALING_RATE = 25; // per second

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

        String[] CapturePointNames =
        {
            "A", "B", "C", "D", "E", "F", "G", "H"
        };

        BlipSprite FallbackIcon = BlipSprite.CaptureFlag; // Fallback icon if no specific icon is set for the point


        public Dictionary<Team, int> PedsNearby = new Dictionary<Team, int>(); // Track how many squad members are nearby

        public CapturePoint(Vector3 position, Team startingOwner = null)
        {
            Position = position;
            Owner = startingOwner; // Teams will only own points initially in certain gamemodes

            ResetCaptureProgress(); // Ensure capture progress is reset when created

            PointBlip = World.CreateBlip(position);

            PointBlip.Position = position; // Set the blip position to the capture point location
            PointBlip.Scale = 0.8f;

            PointID = ModData.CapturePoints.Count + 1;

            if (PointID >= CapturePointIcons.Length) PointBlip.Sprite = FallbackIcon; 
            else PointBlip.Sprite = CapturePointIcons[PointID - 1];

            if (PointID >= CapturePointNames.Length) Name = "Point " + CapturePointNames[CapturePointNames.Length - 1];
            else Name = "Capture Point";

            PointBlip.Name = "Capture Point " + Name;

        }

        public void CapturePointHandler()
        {
            CaptureProgress = Math.Min(100f, Math.Max(0f, CaptureProgress));
            PedsNearby = PedAI.GetNearbyPeds(Position, Radius); // update PedsNearby
            if (PedsNearby == null) return;
            Team nearbyTeam;

            int numTeamsNearby = PedsNearby.Count(team => team.Value > 0); // Count how many teams have peds nearby

            IsContested = numTeamsNearby > 1; // If more than one team has peds nearby, the point is contested

            if (IsContested) return;

            // If the capturing team has no peds left nearby, cancel the capture
            if (CaptureTeam != null && (!PedsNearby.ContainsKey(CaptureTeam) || PedsNearby[CaptureTeam] <= 0))
            {
                ResetCaptureProgress();
            }

            int numPeds = PedsNearby.Values.Sum(); // Total number of peds nearby
            var keyValue = PedsNearby.FirstOrDefault(v => v.Value > 0);
            nearbyTeam = keyValue.Key; // Get the first team with peds nearby

            // If no team is nearby or the nearby team is the owner, do nothing
            if (nearbyTeam == null || nearbyTeam == Owner) return;

            if (CaptureProgress >= 100f && CaptureTeam == nearbyTeam)
            {
                // Capture completed
                Owner = nearbyTeam; // Set the owner to the capturing team

                PointBlip.Color = Owner.BlipColor;
                ResetCaptureProgress(); // Reset capture progress after capture is complete

                return;
            }

            if (CaptureTeam != nearbyTeam && Owner != nearbyTeam)
            {
                ResetCaptureProgress();

                CaptureTeam = nearbyTeam;
                IsCapturing = true;
                GTA.UI.Screen.ShowSubtitle("Team " + CaptureTeam.Name + " is capturing Point " + PointID, 5000); // Show capture message
            }

            if (CaptureTeam == nearbyTeam)
            {
                CaptureProgress += CAPTURE_RATE; // Increment capture progress based on number of peds and time elapsed
                CaptureProgress = Math.Min(100f, CaptureProgress); // Ensure capture progress does not exceed 100
                GTA.UI.Screen.ShowSubtitle("Capture Progress: " + CaptureProgress, 5000); // Show capture message

            }
        }

        public void ResetCaptureProgress()
        {
            CaptureTeam = null;
            IsCapturing = false;
            IsContested = false;
            CaptureProgress = 0.0f; // Reset capture progress
        }

        public void BattleStart()
        {
            Owner = null;
            PointBlip.Color = BlipColor.White; // Reset blip color to white
            ResetCaptureProgress();
        }

    }
}

