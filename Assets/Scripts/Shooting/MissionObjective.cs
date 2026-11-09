using System;
using UnityEngine;

namespace YashGame.Shooting
{
    public enum MissionType
    {
        Elimination,   // Eliminate target number of enemies
        Survival,      // Survive for a duration against continuous reinforcements
        Assassination  // Hunt and eliminate a designated High Value Target (HVT)
    }

    [System.Serializable]
    public class MissionData
    {
        public string id;
        public string title;
        public string subtitle;
        [TextArea(2, 4)] public string briefing;
        public MissionType type;
        public int targetCount;
        public float timeLimitSeconds;
        public int rewardCoins;
        public string hvtName;

        public MissionData() { }

        public MissionData(string id, string title, string subtitle, string briefing, MissionType type, int targetCount, float timeLimitSeconds, int rewardCoins, string hvtName = "")
        {
            this.id = id;
            this.title = title;
            this.subtitle = subtitle;
            this.briefing = briefing;
            this.type = type;
            this.targetCount = targetCount;
            this.timeLimitSeconds = timeLimitSeconds;
            this.rewardCoins = rewardCoins;
            this.hvtName = hvtName;
        }

        #region Factory Presets

        /// <summary>
        /// Mission 1: Sector Purge (Elimination)
        /// Eliminate 8 enemy combatants.
        /// </summary>
        public static MissionData CreateSectorPurge()
        {
            return new MissionData(
                id: "mission_purge",
                title: "OPERATION: SECTOR PURGE",
                subtitle: "HOSTILE ELIMINATION",
                briefing: "Hostile forces have established an armed outpost in Sector 4. Sweep the perimeter and eliminate all 8 hostile combatants.",
                type: MissionType.Elimination,
                targetCount: 8,
                timeLimitSeconds: 0f,
                rewardCoins: 150
            );
        }

        /// <summary>
        /// Mission 2: Last Stand (Survival)
        /// Survive 60 seconds against escalating waves of enemies.
        /// </summary>
        public static MissionData CreateLastStand()
        {
            return new MissionData(
                id: "mission_survival",
                title: "OPERATION: LAST STAND",
                subtitle: "TACTICAL SURVIVAL",
                briefing: "Reinforcements are inbound. Hold your position against continuous enemy waves for 60 seconds until gunship extraction arrives.",
                type: MissionType.Survival,
                targetCount: 0,
                timeLimitSeconds: 60f,
                rewardCoins: 250
            );
        }

        /// <summary>
        /// Mission 3: Cyber Warlord (Assassination)
        /// Hunt down and destroy the heavy Juggernaut commander.
        /// </summary>
        public static MissionData CreateAssassination()
        {
            return new MissionData(
                id: "mission_assassination",
                title: "OPERATION: CYBER WARLORD",
                subtitle: "HIGH-VALUE TARGET",
                briefing: "Warlord Commander Malakor has landed with his armored vanguard. Locate and eliminate the Commander at all costs.",
                type: MissionType.Assassination,
                targetCount: 1,
                timeLimitSeconds: 120f,
                rewardCoins: 350,
                hvtName: "Commander Malakor"
            );
        }

        #endregion
    }
}
