using System;
using UnityEngine;

namespace YashGame.Shooting
{
    public enum MapId
    {
        DesertWarfare,
        ModernCity,
        MilitaryBase,
        ForestSurvival
    }

    /// <summary>
    /// Configuration data for each 3D battlefield map:
    /// Visual style, lighting, fog, environment layout, threat parameters, and unlock conditions.
    /// </summary>
    [System.Serializable]
    public class MapData
    {
        public string id;
        public MapId mapType;
        public string displayName;
        public string subtitle;
        [TextArea(3, 5)] public string description;
        public string tacticalIntel;
        public int requiredMissionsCleared;
        public int baseEnemyCount;

        [Header("Colors & Theme")]
        public Color themeAccentColor;
        public Color groundColor;
        public Color roadColor;
        public Color buildingColor;
        public Color roofColor;
        public Color barrierColor;

        [Header("Atmosphere & Lighting")]
        public Color skyColor;
        public Color sunColor;
        public float sunIntensity;
        public Vector3 sunRotation;
        public Color fogColor;
        public float fogDensity;

        public MapData() { }

        public MapData(string id, MapId mapType, string displayName, string subtitle, string description, string tacticalIntel, int requiredMissionsCleared, int baseEnemyCount, Color themeAccentColor, Color groundColor, Color roadColor, Color buildingColor, Color roofColor, Color barrierColor, Color skyColor, Color sunColor, float sunIntensity, Vector3 sunRotation, Color fogColor, float fogDensity)
        {
            this.id = id;
            this.mapType = mapType;
            this.displayName = displayName;
            this.subtitle = subtitle;
            this.description = description;
            this.tacticalIntel = tacticalIntel;
            this.requiredMissionsCleared = requiredMissionsCleared;
            this.baseEnemyCount = baseEnemyCount;
            this.themeAccentColor = themeAccentColor;
            this.groundColor = groundColor;
            this.roadColor = roadColor;
            this.buildingColor = buildingColor;
            this.roofColor = roofColor;
            this.barrierColor = barrierColor;
            this.skyColor = skyColor;
            this.sunColor = sunColor;
            this.sunIntensity = sunIntensity;
            this.sunRotation = sunRotation;
            this.fogColor = fogColor;
            this.fogDensity = fogDensity;
        }

        #region Factory Presets

        /// <summary>
        /// Map 1: Desert Warfare (Unlocked by default)
        /// Large open desert battlefield with dunes, sandstone ruins, defensive sandbags, and warm golden sun.
        /// </summary>
        public static MapData CreateDesertWarfare()
        {
            return new MapData(
                id: "desert",
                mapType: MapId.DesertWarfare,
                displayName: "DESERT WARFARE",
                subtitle: "OPERATION: SCORCHED SANDS",
                description: "A vast arid combat zone featuring abandoned sandstone structures, rocky outcroppings, defensive sandbag trenches, and rusted military cargo containers.",
                tacticalIntel: "Long sightlines favor Sniper Rifle and Assault Rifle. Use container corridors for close-quarters Shotgun ambushes.",
                requiredMissionsCleared: 0,
                baseEnemyCount: 6,
                themeAccentColor: new Color(1.0f, 0.75f, 0.2f),     // Warm Gold
                groundColor: new Color(0.76f, 0.62f, 0.42f),         // Golden Desert Sand
                roadColor: new Color(0.60f, 0.48f, 0.32f),           // Packed dirt pathway
                buildingColor: new Color(0.68f, 0.54f, 0.38f),       // Weathered Sandstone
                roofColor: new Color(0.48f, 0.38f, 0.26f),           // Dark adobe roof
                barrierColor: new Color(0.55f, 0.45f, 0.30f),        // Sandbag brown
                skyColor: new Color(0.85f, 0.78f, 0.65f),            // Hazy desert sky
                sunColor: new Color(1.0f, 0.94f, 0.80f),             // Blazing desert sun
                sunIntensity: 1.35f,
                sunRotation: new Vector3(50f, -40f, 0f),
                fogColor: new Color(0.82f, 0.72f, 0.55f),            // Dust haze
                fogDensity: 0.008f
            );
        }

        /// <summary>
        /// Map 2: Modern City (Unlocked after Desert Warfare)
        /// Urban warfare featuring asphalt avenues, multi-story skyscraper blocks, road dividers, and vehicles.
        /// </summary>
        public static MapData CreateModernCity()
        {
            return new MapData(
                id: "city",
                mapType: MapId.ModernCity,
                displayName: "MODERN CITY",
                subtitle: "OPERATION: CONCRETE JUNGLE",
                description: "Dense metropolitan warzone with multi-tier office towers, asphalt cross-avenues, abandoned commuter vehicles, and concrete street barriers.",
                tacticalIntel: "Tight street intersections and alleys demand fast reflexes. Switch to Pistol or Shotgun when clearing vehicle checkpoints.",
                requiredMissionsCleared: 1,
                baseEnemyCount: 8,
                themeAccentColor: new Color(0.0f, 0.85f, 1.0f),     // Cyan Neon
                groundColor: new Color(0.18f, 0.20f, 0.24f),         // Urban Asphalt
                roadColor: new Color(0.11f, 0.13f, 0.16f),           // Dark roadway
                buildingColor: new Color(0.25f, 0.30f, 0.38f),       // Steel & concrete towers
                roofColor: new Color(0.14f, 0.16f, 0.20f),           // Carbon roof
                barrierColor: new Color(0.42f, 0.45f, 0.52f),        // Concrete road barriers
                skyColor: new Color(0.35f, 0.45f, 0.60f),            // Overcast metropolis sky
                sunColor: new Color(0.92f, 0.95f, 1.0f),             // Cool urban light
                sunIntensity: 1.15f,
                sunRotation: new Vector3(45f, 30f, 0f),
                fogColor: new Color(0.30f, 0.36f, 0.45f),            // City smog / fog
                fogDensity: 0.012f
            );
        }

        /// <summary>
        /// Map 3: Military Base (Unlocked after Modern City)
        /// Fortified training and defense installation with hangars, observation watchtowers, bunkers, and armory boxes.
        /// </summary>
        public static MapData CreateMilitaryBase()
        {
            return new MapData(
                id: "military",
                mapType: MapId.MilitaryBase,
                displayName: "MILITARY BASE",
                subtitle: "OPERATION: IRON FORTRESS",
                description: "Heavily fortified combat outpost featuring arched aircraft hangars, reinforced blast bunkers, perimeter sniper watchtowers, and radar arrays.",
                tacticalIntel: "Enemy heavy juggernauts guard central hangars. Eliminate watchtower sentries first to secure high ground advantages.",
                requiredMissionsCleared: 2,
                baseEnemyCount: 9,
                themeAccentColor: new Color(1.0f, 0.25f, 0.35f),    // Crimson Red
                groundColor: new Color(0.22f, 0.25f, 0.22f),         // Military tarmac / compound ground
                roadColor: new Color(0.15f, 0.17f, 0.15f),           // Reinforced runway
                buildingColor: new Color(0.28f, 0.32f, 0.28f),       // Camouflage olive drab
                roofColor: new Color(0.12f, 0.14f, 0.12f),           // Reinforced steel roof
                barrierColor: new Color(0.35f, 0.38f, 0.32f),        // Heavy blast shields
                skyColor: new Color(0.40f, 0.42f, 0.38f),            // Stormy military horizon
                sunColor: new Color(0.98f, 0.96f, 0.90f),            // High-noon stark sunlight
                sunIntensity: 1.25f,
                sunRotation: new Vector3(60f, -20f, 0f),
                fogColor: new Color(0.28f, 0.30f, 0.28f),            // Tactical gunsmoke haze
                fogDensity: 0.010f
            );
        }

        /// <summary>
        /// Map 4: Forest Survival (Unlocked after Military Base)
        /// Dense wilderness featuring tall pine trees, natural boulder cover, wooden log cabins, and ranger lookout towers.
        /// </summary>
        public static MapData CreateForestSurvival()
        {
            return new MapData(
                id: "forest",
                mapType: MapId.ForestSurvival,
                displayName: "FOREST SURVIVAL",
                subtitle: "OPERATION: DEEP VERDANT",
                description: "Dense woodland terrain with pine tree clusters, massive mossy rock formations, rustic wooden cabins, campsite shelters, and natural hill crests.",
                tacticalIntel: "Dense tree canopies obscure long-range visibility. Beware of fast skirmishers flanking through the tree lines.",
                requiredMissionsCleared: 3,
                baseEnemyCount: 10,
                themeAccentColor: new Color(0.2f, 0.95f, 0.45f),    // Emerald Neon
                groundColor: new Color(0.16f, 0.26f, 0.14f),         // Mossy forest earth
                roadColor: new Color(0.28f, 0.22f, 0.14f),           // Muddy forest trail
                buildingColor: new Color(0.34f, 0.24f, 0.16f),       // Wooden timber logs
                roofColor: new Color(0.20f, 0.14f, 0.08f),           // Dark shingle roof
                barrierColor: new Color(0.38f, 0.40f, 0.36f),        // Natural granite boulders
                skyColor: new Color(0.45f, 0.58f, 0.50f),            // Verdant morning sky
                sunColor: new Color(1.0f, 0.96f, 0.85f),             // Filtered canopy rays
                sunIntensity: 1.10f,
                sunRotation: new Vector3(38f, -65f, 0f),
                fogColor: new Color(0.22f, 0.32f, 0.24f),            // Dense morning mist
                fogDensity: 0.015f
            );
        }

        #endregion
    }
}
