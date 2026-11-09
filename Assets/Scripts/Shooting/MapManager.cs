using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Master environment and map manager for the 4 selectable 3D battlefields:
    /// 1. Desert Warfare
    /// 2. Modern City
    /// 3. Military Base
    /// 4. Forest Survival
    /// Constructs procedural buildings, obstacles, terrain, lighting, fog,
    /// and generates map-specific player spawns and enemy patrol routes.
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        #region Public Properties

        public MapData CurrentMapData { get; private set; }
        public Transform PlayerSpawnPoint { get; private set; }
        public List<Vector3> EnemySpawnPoints { get; private set; } = new List<Vector3>();
        public List<Transform[]> EnemyPatrolRoutes { get; private set; } = new List<Transform[]>();

        #endregion

        #region Material Palette

        private Material _groundMat;
        private Material _roadMat;
        private Material _buildingMat;
        private Material _roofMat;
        private Material _barrierMat;
        private Material _containerMat1;
        private Material _containerMat2;
        private Material _metalMat;
        private Material _windowGlassMat;
        private Material _foliageMat;
        private Material _trunkMat;
        private Material _propMat;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Map Build Entry

        /// <summary>
        /// Builds the designated battlefield environment according to mapId.
        /// Reads PlayerPrefs("YashGame_SelectedMap") if mapId is empty.
        /// </summary>
        public void BuildMap(string mapId = "")
        {
            if (string.IsNullOrEmpty(mapId))
            {
                mapId = PlayerPrefs.GetString("YashGame_SelectedMap", "desert");
            }

            // Clean up previous map root if any exists
            Transform oldRoot = transform.Find("MapRoot");
            if (oldRoot != null)
            {
                DestroyImmediate(oldRoot.gameObject);
            }

            EnemySpawnPoints.Clear();
            EnemyPatrolRoutes.Clear();

            // Resolve MapData
            CurrentMapData = ResolveMapData(mapId);

            Debug.Log($"[MapManager] Constructing 3D Battlefield: {CurrentMapData.displayName} ({CurrentMapData.subtitle})...");

            InitMaterials(CurrentMapData);
            ApplyAtmosphere(CurrentMapData);

            GameObject mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(transform, false);

            switch (CurrentMapData.mapType)
            {
                case MapId.DesertWarfare:
                    BuildDesertWarfare(mapRoot.transform);
                    break;
                case MapId.ModernCity:
                    BuildModernCity(mapRoot.transform);
                    break;
                case MapId.MilitaryBase:
                    BuildMilitaryBase(mapRoot.transform);
                    break;
                case MapId.ForestSurvival:
                    BuildForestSurvival(mapRoot.transform);
                    break;
            }

            Debug.Log($"[MapManager] 3D Battlefield '{CurrentMapData.displayName}' successfully constructed with {EnemySpawnPoints.Count} enemy spawn points!");
        }

        public MapData ResolveMapData(string mapId)
        {
            switch (mapId.ToLower())
            {
                case "city": return MapData.CreateModernCity();
                case "military": return MapData.CreateMilitaryBase();
                case "forest": return MapData.CreateForestSurvival();
                case "desert":
                default:
                    return MapData.CreateDesertWarfare();
            }
        }

        #endregion

        #region Atmosphere & Lighting

        private void ApplyAtmosphere(MapData data)
        {
            // Lighting
            Light sun = FindObjectOfType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("SunLight");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.color = data.sunColor;
            sun.intensity = data.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(data.sunRotation);
            sun.shadows = LightShadows.Soft;

            // Fog & Ambient
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = data.fogColor;
            RenderSettings.fogDensity = data.fogDensity;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = data.skyColor * 0.9f;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = data.skyColor;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        private void InitMaterials(MapData data)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            _groundMat = new Material(litShader) { color = data.groundColor };
            _roadMat = new Material(litShader) { color = data.roadColor };
            _buildingMat = new Material(litShader) { color = data.buildingColor };
            _roofMat = new Material(litShader) { color = data.roofColor };
            _barrierMat = new Material(litShader) { color = data.barrierColor };

            _containerMat1 = new Material(litShader) { color = data.themeAccentColor * 0.75f };
            _containerMat2 = new Material(litShader) { color = new Color(0.18f, 0.22f, 0.30f) };
            _metalMat = new Material(litShader) { color = new Color(0.35f, 0.38f, 0.42f) };
            _windowGlassMat = new Material(litShader) { color = new Color(0.15f, 0.65f, 0.85f, 0.9f) };

            _foliageMat = new Material(litShader) { color = new Color(0.12f, 0.38f, 0.16f) }; // Pine Green
            _trunkMat = new Material(litShader) { color = new Color(0.28f, 0.18f, 0.10f) };   // Wood Bark
            _propMat = new Material(litShader) { color = new Color(0.48f, 0.38f, 0.22f) };    // Crate/Wood
        }

        #endregion

        #region Map 1: Desert Warfare Implementation

        private void BuildDesertWarfare(Transform root)
        {
            // Ground (120m x 120m golden sand)
            CreateBox(root, "DesertGround", new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), _groundMat);

            // Dirt road track cross
            CreateBox(root, "DirtTrack_NS", new Vector3(0f, 0.02f, 0f), new Vector3(12f, 0.05f, 120f), _roadMat, false);
            CreateBox(root, "DirtTrack_EW", new Vector3(0f, 0.02f, 0f), new Vector3(120f, 0.05f, 12f), _roadMat, false);

            // Sandstone Ruin Compounds
            BuildSandstoneRuin(root, new Vector3(-24f, 0f, 22f), new Vector2(18f, 14f), 7.5f);
            BuildSandstoneRuin(root, new Vector3(26f, 0f, 24f), new Vector2(16f, 16f), 9.0f);
            BuildSandstoneRuin(root, new Vector3(-26f, 0f, -22f), new Vector2(15f, 15f), 6.5f);
            BuildSandstoneRuin(root, new Vector3(25f, 0f, -24f), new Vector2(20f, 14f), 8.0f);

            // Center Adobe Outpost
            BuildSandstoneRuin(root, new Vector3(0f, 0f, 16f), new Vector2(14f, 12f), 8.5f);

            // Desert Rock Formations
            CreateRockFormation(root, new Vector3(-40f, 0f, 40f), 3.5f);
            CreateRockFormation(root, new Vector3(42f, 0f, 38f), 4.2f);
            CreateRockFormation(root, new Vector3(-38f, 0f, -38f), 3.0f);
            CreateRockFormation(root, new Vector3(40f, 0f, -40f), 4.5f);
            CreateRockFormation(root, new Vector3(0f, 0f, -15f), 2.4f);

            // Sandbag Bunkers & Defensive Barriers
            CreateSandbagTrench(root, new Vector3(-10f, 0f, 6f), 8f, 0f);
            CreateSandbagTrench(root, new Vector3(10f, 0f, 6f), 8f, 0f);
            CreateSandbagTrench(root, new Vector3(-12f, 0f, -8f), 10f, 45f);
            CreateSandbagTrench(root, new Vector3(12f, 0f, -8f), 10f, -45f);

            // Military Cargo Containers
            CreateCargoContainer(root, new Vector3(-16f, 0f, 4f), 25f, _containerMat1);
            CreateCargoContainer(root, new Vector3(16f, 0f, 4f), -20f, _containerMat2);
            CreateCargoContainer(root, new Vector3(-6f, 0f, 30f), 90f, _containerMat1);
            CreateCargoContainer(root, new Vector3(8f, 0f, 30f), 85f, _containerMat2);

            // Setup Spawns & Patrol Routes
            SetupSpawnsAndRoutes(root,
                playerPos: new Vector3(0f, 0.5f, -42f),
                routes: new Vector3[][]
                {
                    new Vector3[] { new Vector3(-24f, 0f, 22f), new Vector3(-15f, 0f, 30f), new Vector3(-32f, 0f, 15f) },
                    new Vector3[] { new Vector3(26f, 0f, 24f), new Vector3(35f, 0f, 15f), new Vector3(18f, 0f, 32f) },
                    new Vector3[] { new Vector3(0f, 0f, 16f), new Vector3(-8f, 0f, 12f), new Vector3(8f, 0f, 12f) },
                    new Vector3[] { new Vector3(-26f, 0f, -22f), new Vector3(-18f, 0f, -15f), new Vector3(-30f, 0f, -28f) },
                    new Vector3[] { new Vector3(25f, 0f, -24f), new Vector3(16f, 0f, -18f), new Vector3(32f, 0f, -30f) }
                }
            );
        }

        private void BuildSandstoneRuin(Transform parent, Vector3 pos, Vector2 size, float height)
        {
            GameObject ruin = new GameObject("SandstoneRuin");
            ruin.transform.SetParent(parent, false);
            ruin.transform.position = pos;

            // Walls
            float wallThick = 0.8f;
            // North wall with breach gap
            CreateBox(ruin.transform, "Wall_N_L", new Vector3(-size.x * 0.3f, height * 0.5f, size.y * 0.5f), new Vector3(size.x * 0.4f, height, wallThick), _buildingMat);
            CreateBox(ruin.transform, "Wall_N_R", new Vector3(size.x * 0.3f, height * 0.5f, size.y * 0.5f), new Vector3(size.x * 0.4f, height, wallThick), _buildingMat);

            // South wall
            CreateBox(ruin.transform, "Wall_S", new Vector3(0f, height * 0.5f, -size.y * 0.5f), new Vector3(size.x, height, wallThick), _buildingMat);

            // West & East walls
            CreateBox(ruin.transform, "Wall_W", new Vector3(-size.x * 0.5f, height * 0.5f, 0f), new Vector3(wallThick, height, size.y), _buildingMat);
            CreateBox(ruin.transform, "Wall_E", new Vector3(size.x * 0.5f, height * 0.5f, 0f), new Vector3(wallThick, height, size.y), _buildingMat);

            // Partial Adobe Roof (Sniper / Cover platform)
            CreateBox(ruin.transform, "RuinRoof", new Vector3(0f, height, 0f), new Vector3(size.x, 0.4f, size.y * 0.7f), _roofMat);
        }

        private void CreateSandbagTrench(Transform parent, Vector3 pos, float length, float rotY)
        {
            GameObject trench = new GameObject("SandbagTrench");
            trench.transform.SetParent(parent, false);
            trench.transform.position = pos;
            trench.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            CreateBox(trench.transform, "SandbagWall", Vector3.up * 0.6f, new Vector3(length, 1.2f, 0.9f), _barrierMat);
        }

        private void CreateRockFormation(Transform parent, Vector3 pos, float scale)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = "DesertRock";
            rock.transform.SetParent(parent, false);
            rock.transform.position = pos + Vector3.up * (scale * 0.35f);
            rock.transform.localScale = new Vector3(scale * 1.3f, scale * 0.85f, scale);
            rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(5f, 25f), UnityEngine.Random.Range(0f, 360f), 0f);
            rock.GetComponent<Renderer>().material = _barrierMat;
        }

        #endregion

        #region Map 2: Modern City Implementation

        private void BuildModernCity(Transform root)
        {
            // Asphalt Ground
            CreateBox(root, "CityGround", new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), _groundMat);

            // Major City Boulevard Grid
            CreateBox(root, "Avenue_Main_NS", new Vector3(0f, 0.02f, 0f), new Vector3(16f, 0.05f, 124f), _roadMat, false);
            CreateBox(root, "Avenue_Main_EW", new Vector3(0f, 0.02f, 0f), new Vector3(124f, 0.05f, 16f), _roadMat, false);

            // Skyscraper Blocks
            BuildSkyscraper(root, new Vector3(-28f, 0f, 28f), new Vector3(20f, 22f, 20f), 5);
            BuildSkyscraper(root, new Vector3(28f, 0f, 28f), new Vector3(22f, 26f, 18f), 6);
            BuildSkyscraper(root, new Vector3(-28f, 0f, -28f), new Vector3(18f, 18f, 22f), 4);
            BuildSkyscraper(root, new Vector3(28f, 0f, -28f), new Vector3(24f, 24f, 20f), 5);

            // Low-rise Metro Stations & Bank Structures
            BuildCityCommercialBuilding(root, new Vector3(-35f, 0f, 0f), new Vector3(14f, 9f, 22f));
            BuildCityCommercialBuilding(root, new Vector3(35f, 0f, 0f), new Vector3(14f, 9f, 22f));

            // Abandoned City Vehicles
            CreateVehicleProp(root, new Vector3(-4f, 0f, 12f), 15f, new Color(0.7f, 0.15f, 0.15f)); // Red Sedan
            CreateVehicleProp(root, new Vector3(5f, 0f, -14f), -35f, new Color(0.15f, 0.45f, 0.75f)); // Blue Sedan
            CreateVehicleProp(root, new Vector3(4f, 0f, 26f), 5f, new Color(0.85f, 0.75f, 0.2f)); // Yellow Cab
            CreateVehicleProp(root, new Vector3(-5f, 0f, -32f), -180f, new Color(0.25f, 0.28f, 0.32f)); // Gray SUV

            // Concrete Jersey Road Dividers & Railings
            CreateJerseyBarriers(root, new Vector3(0f, 0f, 8f), 14f);
            CreateJerseyBarriers(root, new Vector3(0f, 0f, -8f), 14f);
            CreateJerseyBarriers(root, new Vector3(-8f, 0f, 0f), 14f, true);
            CreateJerseyBarriers(root, new Vector3(8f, 0f, 0f), 14f, true);

            // Street Light Poles
            CreateStreetLight(root, new Vector3(-9f, 0f, 20f));
            CreateStreetLight(root, new Vector3(9f, 0f, 20f));
            CreateStreetLight(root, new Vector3(-9f, 0f, -20f));
            CreateStreetLight(root, new Vector3(9f, 0f, -20f));

            // Setup Spawns & Patrol Routes
            SetupSpawnsAndRoutes(root,
                playerPos: new Vector3(0f, 0.5f, -44f),
                routes: new Vector3[][]
                {
                    new Vector3[] { new Vector3(-28f, 0f, 28f), new Vector3(-16f, 0f, 20f), new Vector3(-34f, 0f, 16f) },
                    new Vector3[] { new Vector3(28f, 0f, 28f), new Vector3(18f, 0f, 22f), new Vector3(36f, 0f, 18f) },
                    new Vector3[] { new Vector3(0f, 0f, 18f), new Vector3(-6f, 0f, 8f), new Vector3(6f, 0f, 8f) },
                    new Vector3[] { new Vector3(-28f, 0f, -28f), new Vector3(-18f, 0f, -16f), new Vector3(-35f, 0f, -20f) },
                    new Vector3[] { new Vector3(28f, 0f, -28f), new Vector3(18f, 0f, -16f), new Vector3(35f, 0f, -20f) }
                }
            );
        }

        private void BuildSkyscraper(Transform parent, Vector3 pos, Vector3 size, int stories)
        {
            GameObject tower = new GameObject("Skyscraper");
            tower.transform.SetParent(parent, false);
            tower.transform.position = pos;

            // Main Tower Body
            CreateBox(tower.transform, "TowerCore", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);

            // Glass Window Strips
            float storyHeight = size.y / stories;
            for (int i = 0; i < stories; i++)
            {
                float y = (i + 0.5f) * storyHeight;
                // South Glass
                CreateBox(tower.transform, $"Glass_S_{i}", new Vector3(0f, y, size.z * 0.5f + 0.05f), new Vector3(size.x * 0.8f, storyHeight * 0.5f, 0.1f), _windowGlassMat, false);
                // North Glass
                CreateBox(tower.transform, $"Glass_N_{i}", new Vector3(0f, y, -size.z * 0.5f - 0.05f), new Vector3(size.x * 0.8f, storyHeight * 0.5f, 0.1f), _windowGlassMat, false);
            }

            // Rooftop Helipad / AC Units
            CreateBox(tower.transform, "RoofRailing", new Vector3(0f, size.y + 0.5f, 0f), new Vector3(size.x * 0.95f, 1.0f, size.z * 0.95f), _roofMat, false);
            CreateBox(tower.transform, "AC_Unit", new Vector3(-size.x * 0.2f, size.y + 1f, 0f), new Vector3(2.5f, 2f, 3.5f), _metalMat);
        }

        private void BuildCityCommercialBuilding(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject bld = new GameObject("CommercialBuilding");
            bld.transform.SetParent(parent, false);
            bld.transform.position = pos;

            CreateBox(bld.transform, "Structure", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);
            CreateBox(bld.transform, "FrontWindow", new Vector3(0f, size.y * 0.4f, size.z * 0.5f + 0.05f), new Vector3(size.x * 0.85f, size.y * 0.6f, 0.1f), _windowGlassMat, false);
        }

        private void CreateVehicleProp(Transform parent, Vector3 pos, float rotY, Color bodyColor)
        {
            GameObject car = new GameObject("CivilianVehicle");
            car.transform.SetParent(parent, false);
            car.transform.position = pos;
            car.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material carMat = new Material(lit) { color = bodyColor };

            // Chassis
            CreateBox(car.transform, "Chassis", new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 0.75f, 4.6f), carMat);
            // Cabin
            CreateBox(car.transform, "Cabin", new Vector3(0f, 1.25f, -0.2f), new Vector3(1.9f, 0.7f, 2.5f), _windowGlassMat);

            // 4 Wheels
            Vector3[] wheelOffsets = new Vector3[]
            {
                new Vector3(-1.15f, 0.35f, 1.3f),
                new Vector3(1.15f, 0.35f, 1.3f),
                new Vector3(-1.15f, 0.35f, -1.3f),
                new Vector3(1.15f, 0.35f, -1.3f)
            };

            foreach (var off in wheelOffsets)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(car.transform, false);
                wheel.transform.localPosition = off;
                wheel.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.GetComponent<Renderer>().material = _roadMat;
                Destroy(wheel.GetComponent<Collider>());
            }
        }

        private void CreateJerseyBarriers(Transform parent, Vector3 pos, float length, bool rotate90 = false)
        {
            GameObject barrier = new GameObject("JerseyBarriers");
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos;
            if (rotate90) barrier.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            CreateBox(barrier.transform, "ConcreteBlock", Vector3.up * 0.55f, new Vector3(length, 1.1f, 0.6f), _barrierMat);
        }

        private void CreateStreetLight(Transform parent, Vector3 pos)
        {
            GameObject pole = new GameObject("StreetLight");
            pole.transform.SetParent(parent, false);
            pole.transform.position = pos;

            CreateBox(pole.transform, "Pole", Vector3.up * 3.5f, new Vector3(0.25f, 7f, 0.25f), _metalMat);
            CreateBox(pole.transform, "Arm", new Vector3(1f, 7f, 0f), new Vector3(2.2f, 0.2f, 0.25f), _metalMat);
        }

        #endregion

        #region Map 3: Military Base Implementation

        private void BuildMilitaryBase(Transform root)
        {
            // Tarmac Ground
            CreateBox(root, "BaseGround", new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), _groundMat);

            // Aircraft Runway / Taxiway
            CreateBox(root, "RunwayStrip", new Vector3(0f, 0.02f, 0f), new Vector3(22f, 0.05f, 126f), _roadMat, false);

            // Aircraft Hangars (North-West & North-East)
            BuildAircraftHangar(root, new Vector3(-28f, 0f, 25f), new Vector3(24f, 11f, 28f));
            BuildAircraftHangar(root, new Vector3(28f, 0f, 25f), new Vector3(24f, 11f, 28f));

            // Reinforced Blast Bunkers (South-West & South-East)
            BuildUndergroundBunker(root, new Vector3(-26f, 0f, -24f), new Vector3(18f, 5.5f, 20f));
            BuildUndergroundBunker(root, new Vector3(26f, 0f, -24f), new Vector3(18f, 5.5f, 20f));

            // Central Armory & Headquarters
            BuildHeadquartersArmory(root, new Vector3(0f, 0f, 18f), new Vector3(18f, 8f, 14f));

            // Observation Watchtowers with elevated sniper platforms
            BuildObservationWatchtower(root, new Vector3(-44f, 0f, 44f), 10f);
            BuildObservationWatchtower(root, new Vector3(44f, 0f, 44f), 10f);
            BuildObservationWatchtower(root, new Vector3(-44f, 0f, -44f), 10f);
            BuildObservationWatchtower(root, new Vector3(44f, 0f, -44f), 10f);

            // Perimeter Security Fences
            BuildSecurityPerimeter(root, 58f);

            // Radar Dish Prop
            BuildRadarInstallation(root, new Vector3(0f, 0f, 36f));

            // Ammunition Crate Stacks & Fuel Barrels
            CreateAmmoCrateStack(root, new Vector3(-12f, 0f, 5f));
            CreateAmmoCrateStack(root, new Vector3(12f, 0f, 5f));
            CreateAmmoCrateStack(root, new Vector3(-8f, 0f, -12f));

            // Cargo Containers
            CreateCargoContainer(root, new Vector3(-18f, 0f, -5f), 0f, _containerMat1);
            CreateCargoContainer(root, new Vector3(18f, 0f, -5f), 180f, _containerMat2);

            // Setup Spawns & Patrol Routes
            SetupSpawnsAndRoutes(root,
                playerPos: new Vector3(0f, 0.5f, -46f),
                routes: new Vector3[][]
                {
                    new Vector3[] { new Vector3(-28f, 0f, 25f), new Vector3(-18f, 0f, 16f), new Vector3(-35f, 0f, 32f) },
                    new Vector3[] { new Vector3(28f, 0f, 25f), new Vector3(18f, 0f, 16f), new Vector3(35f, 0f, 32f) },
                    new Vector3[] { new Vector3(0f, 0f, 18f), new Vector3(-7f, 0f, 10f), new Vector3(7f, 0f, 10f) },
                    new Vector3[] { new Vector3(-26f, 0f, -24f), new Vector3(-16f, 0f, -14f), new Vector3(-34f, 0f, -28f) },
                    new Vector3[] { new Vector3(26f, 0f, -24f), new Vector3(16f, 0f, -14f), new Vector3(34f, 0f, -28f) }
                }
            );
        }

        private void BuildAircraftHangar(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject hangar = new GameObject("AircraftHangar");
            hangar.transform.SetParent(parent, false);
            hangar.transform.position = pos;

            // Main Vault Body
            CreateBox(hangar.transform, "HangarCore", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);

            // Arched curved roof top
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.name = "HangarArchRoof";
            roof.transform.SetParent(hangar.transform, false);
            roof.transform.localPosition = new Vector3(0f, size.y, 0f);
            roof.transform.localScale = new Vector3(size.x, size.z * 0.5f, size.y * 0.7f);
            roof.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            roof.GetComponent<Renderer>().material = _roofMat;
            Destroy(roof.GetComponent<Collider>());

            // Open Hangar Gate (South opening)
            CreateBox(hangar.transform, "HangarGateFrame", new Vector3(0f, size.y * 0.45f, -size.z * 0.5f), new Vector3(size.x * 0.8f, size.y * 0.9f, 0.4f), _metalMat, false);
        }

        private void BuildUndergroundBunker(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject bunker = new GameObject("ConcreteBunker");
            bunker.transform.SetParent(parent, false);
            bunker.transform.position = pos;

            // Reinforced concrete block
            CreateBox(bunker.transform, "BunkerStructure", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);
            // Angled blast roof
            CreateBox(bunker.transform, "BlastRoof", new Vector3(0f, size.y + 0.3f, 0f), new Vector3(size.x * 1.1f, 0.6f, size.z * 1.1f), _roofMat);
            // Narrow Bunker vision slits
            CreateBox(bunker.transform, "GunSlit", new Vector3(0f, size.y * 0.75f, size.z * 0.5f + 0.05f), new Vector3(size.x * 0.7f, 0.4f, 0.2f), _roadMat, false);
        }

        private void BuildHeadquartersArmory(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject hq = new GameObject("HeadquartersArmory");
            hq.transform.SetParent(parent, false);
            hq.transform.position = pos;

            CreateBox(hq.transform, "HQMain", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);
            CreateBox(hq.transform, "HQRoof", new Vector3(0f, size.y + 0.25f, 0f), new Vector3(size.x * 1.05f, 0.5f, size.z * 1.05f), _roofMat);
        }

        private void BuildObservationWatchtower(Transform parent, Vector3 pos, float height)
        {
            GameObject tower = new GameObject("ObservationWatchtower");
            tower.transform.SetParent(parent, false);
            tower.transform.position = pos;

            // 4 Steel Legs
            float spread = 3f;
            Vector3[] legOffsets = new Vector3[]
            {
                new Vector3(-spread, height * 0.5f, -spread),
                new Vector3(spread, height * 0.5f, -spread),
                new Vector3(-spread, height * 0.5f, spread),
                new Vector3(spread, height * 0.5f, spread)
            };

            foreach (var off in legOffsets)
            {
                CreateBox(tower.transform, "TowerLeg", off, new Vector3(0.5f, height, 0.5f), _metalMat);
            }

            // Sniper Platform Cabin
            CreateBox(tower.transform, "CabinPlatform", new Vector3(0f, height, 0f), new Vector3(spread * 2.4f, 0.4f, spread * 2.4f), _metalMat);
            CreateBox(tower.transform, "CabinRailing", new Vector3(0f, height + 0.6f, 0f), new Vector3(spread * 2.3f, 1.2f, spread * 2.3f), _barrierMat, false);
            CreateBox(tower.transform, "CabinRoof", new Vector3(0f, height + 2.8f, 0f), new Vector3(spread * 2.5f, 0.3f, spread * 2.5f), _roofMat, false);
        }

        private void BuildSecurityPerimeter(Transform parent, float halfExtent)
        {
            GameObject fence = new GameObject("SecurityFencePerimeter");
            fence.transform.SetParent(parent, false);

            float h = 3.5f;
            float t = 0.3f;
            float full = halfExtent * 2f;

            CreateBox(fence.transform, "Fence_N", new Vector3(0f, h * 0.5f, halfExtent), new Vector3(full, h, t), _barrierMat);
            CreateBox(fence.transform, "Fence_S", new Vector3(0f, h * 0.5f, -halfExtent), new Vector3(full, h, t), _barrierMat);
            CreateBox(fence.transform, "Fence_W", new Vector3(-halfExtent, h * 0.5f, 0f), new Vector3(t, h, full), _barrierMat);
            CreateBox(fence.transform, "Fence_E", new Vector3(halfExtent, h * 0.5f, 0f), new Vector3(t, h, full), _barrierMat);
        }

        private void BuildRadarInstallation(Transform parent, Vector3 pos)
        {
            GameObject radar = new GameObject("RadarInstallation");
            radar.transform.SetParent(parent, false);
            radar.transform.position = pos;

            CreateBox(radar.transform, "Pylon", Vector3.up * 4f, new Vector3(1.2f, 8f, 1.2f), _metalMat);

            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dish.name = "RadarDish";
            dish.transform.SetParent(radar.transform, false);
            dish.transform.localPosition = new Vector3(0f, 8.5f, 0f);
            dish.transform.localScale = new Vector3(5.5f, 5.5f, 1.2f);
            dish.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);
            dish.GetComponent<Renderer>().material = _containerMat1;
            Destroy(dish.GetComponent<Collider>());
        }

        private void CreateAmmoCrateStack(Transform parent, Vector3 pos)
        {
            GameObject stack = new GameObject("CrateStack");
            stack.transform.SetParent(parent, false);
            stack.transform.position = pos;

            CreateBox(stack.transform, "Crate1", new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1.0f, 1.2f), _propMat);
            CreateBox(stack.transform, "Crate2", new Vector3(1.3f, 0.5f, 0f), new Vector3(1.2f, 1.0f, 1.2f), _propMat);
            CreateBox(stack.transform, "CrateTop", new Vector3(0.65f, 1.5f, 0f), new Vector3(1.2f, 1.0f, 1.2f), _propMat);
        }

        #endregion

        #region Map 4: Forest Survival Implementation

        private void BuildForestSurvival(Transform root)
        {
            // Mossy Forest Ground
            CreateBox(root, "ForestGround", new Vector3(0f, -0.5f, 0f), new Vector3(130f, 1f, 130f), _groundMat);

            // Muddy Wilderness Trails
            CreateBox(root, "Trail_NS", new Vector3(0f, 0.02f, 0f), new Vector3(10f, 0.05f, 120f), _roadMat, false);
            CreateBox(root, "Trail_Fork", new Vector3(15f, 0.02f, 10f), new Vector3(45f, 0.05f, 8f), _roadMat, false);

            // Rustic Log Cabins
            BuildLogCabin(root, new Vector3(-24f, 0f, 22f), new Vector3(12f, 6.5f, 14f));
            BuildLogCabin(root, new Vector3(26f, 0f, -20f), new Vector3(14f, 7.0f, 12f));

            // Wooden Ranger Lookout Towers
            BuildWoodenLookoutTower(root, new Vector3(-32f, 0f, -28f), 8f);
            BuildWoodenLookoutTower(root, new Vector3(32f, 0f, 28f), 8f);

            // Campsite Shelters & Campfire
            BuildCampsite(root, new Vector3(0f, 0f, 16f));

            // Procedural Pine Forests (Clusters of trees)
            CreateTreeCluster(root, new Vector3(-38f, 0f, 32f), 12, 18f);
            CreateTreeCluster(root, new Vector3(38f, 0f, 36f), 14, 20f);
            CreateTreeCluster(root, new Vector3(-36f, 0f, -14f), 10, 16f);
            CreateTreeCluster(root, new Vector3(36f, 0f, -34f), 12, 18f);
            CreateTreeCluster(root, new Vector3(0f, 0f, -28f), 8, 14f);

            // Natural Mossy Granite Boulders
            CreateRockFormation(root, new Vector3(-12f, 0f, 6f), 3.2f);
            CreateRockFormation(root, new Vector3(14f, 0f, 6f), 3.8f);
            CreateRockFormation(root, new Vector3(-8f, 0f, -12f), 2.8f);
            CreateRockFormation(root, new Vector3(12f, 0f, -12f), 3.4f);

            // Fallen Logs (Natural cover)
            CreateFallenLog(root, new Vector3(-14f, 0f, 2f), 8f, 25f);
            CreateFallenLog(root, new Vector3(16f, 0f, -2f), 9f, -40f);

            // Setup Spawns & Patrol Routes
            SetupSpawnsAndRoutes(root,
                playerPos: new Vector3(0f, 0.5f, -42f),
                routes: new Vector3[][]
                {
                    new Vector3[] { new Vector3(-24f, 0f, 22f), new Vector3(-14f, 0f, 30f), new Vector3(-32f, 0f, 14f) },
                    new Vector3[] { new Vector3(26f, 0f, -20f), new Vector3(16f, 0f, -14f), new Vector3(34f, 0f, -26f) },
                    new Vector3[] { new Vector3(0f, 0f, 16f), new Vector3(-8f, 0f, 8f), new Vector3(8f, 0f, 8f) },
                    new Vector3[] { new Vector3(-32f, 0f, -28f), new Vector3(-22f, 0f, -18f), new Vector3(-36f, 0f, -34f) },
                    new Vector3[] { new Vector3(32f, 0f, 28f), new Vector3(22f, 0f, 18f), new Vector3(36f, 0f, 34f) }
                }
            );
        }

        private void BuildLogCabin(Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject cabin = new GameObject("LogCabin");
            cabin.transform.SetParent(parent, false);
            cabin.transform.position = pos;

            // Timber Walls
            CreateBox(cabin.transform, "CabinWalls", new Vector3(0f, size.y * 0.5f, 0f), size, _buildingMat);

            // Slanted Shingle Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "CabinRoof";
            roof.transform.SetParent(cabin.transform, false);
            roof.transform.localPosition = new Vector3(0f, size.y + 0.8f, 0f);
            roof.transform.localScale = new Vector3(size.x * 1.15f, 1.6f, size.z * 1.15f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            roof.GetComponent<Renderer>().material = _roofMat;
            Destroy(roof.GetComponent<Collider>());
        }

        private void BuildWoodenLookoutTower(Transform parent, Vector3 pos, float height)
        {
            GameObject tower = new GameObject("WoodenLookoutTower");
            tower.transform.SetParent(parent, false);
            tower.transform.position = pos;

            float w = 2.4f;
            Vector3[] postOffsets = new Vector3[]
            {
                new Vector3(-w, height * 0.5f, -w),
                new Vector3(w, height * 0.5f, -w),
                new Vector3(-w, height * 0.5f, w),
                new Vector3(w, height * 0.5f, w)
            };

            foreach (var off in postOffsets)
            {
                CreateBox(tower.transform, "TimberPost", off, new Vector3(0.45f, height, 0.45f), _trunkMat);
            }

            CreateBox(tower.transform, "DeckPlatform", new Vector3(0f, height, 0f), new Vector3(w * 2.3f, 0.35f, w * 2.3f), _trunkMat);
            CreateBox(tower.transform, "DeckRailing", new Vector3(0f, height + 0.5f, 0f), new Vector3(w * 2.2f, 1.0f, w * 2.2f), _buildingMat, false);
        }

        private void BuildCampsite(Transform parent, Vector3 pos)
        {
            GameObject camp = new GameObject("Campsite");
            camp.transform.SetParent(parent, false);
            camp.transform.position = pos;

            // Tent 1
            CreateAFrameTent(camp.transform, new Vector3(-4f, 0f, 0f), 30f);
            // Tent 2
            CreateAFrameTent(camp.transform, new Vector3(4f, 0f, 2f), -45f);

            // Campfire Pit
            GameObject firePit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            firePit.name = "FirePit";
            firePit.transform.SetParent(camp.transform, false);
            firePit.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            firePit.transform.localScale = new Vector3(2.2f, 0.3f, 2.2f);
            firePit.GetComponent<Renderer>().material = _barrierMat;
            Destroy(firePit.GetComponent<Collider>());

            // Campfire Light Glow
            GameObject fireLight = new GameObject("FireLight");
            fireLight.transform.SetParent(firePit.transform, false);
            fireLight.transform.localPosition = Vector3.up * 0.5f;
            Light fl = fireLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(1.0f, 0.55f, 0.15f);
            fl.range = 8f;
            fl.intensity = 1.6f;
        }

        private void CreateAFrameTent(Transform parent, Vector3 pos, float rotY)
        {
            GameObject tent = new GameObject("Tent");
            tent.transform.SetParent(parent, false);
            tent.transform.position = pos;
            tent.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            CreateBox(tent.transform, "CanvasRoof_L", new Vector3(-0.9f, 1.2f, 0f), new Vector3(0.1f, 2.6f, 3.2f), _containerMat1, false);
            CreateBox(tent.transform, "CanvasRoof_R", new Vector3(0.9f, 1.2f, 0f), new Vector3(0.1f, 2.6f, 3.2f), _containerMat1, false);
        }

        private void CreateTreeCluster(Transform parent, Vector3 center, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 treePos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                float height = UnityEngine.Random.Range(6.5f, 11.5f);
                CreatePineTree(parent, treePos, height);
            }
        }

        private void CreatePineTree(Transform parent, Vector3 pos, float height)
        {
            GameObject tree = new GameObject("PineTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            float trunkHeight = height * 0.4f;
            float trunkRadius = height * 0.05f;

            // Trunk (cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f);
            trunk.GetComponent<Renderer>().material = _trunkMat;

            // Conical Leaf Tiers (3 stacked tiers)
            int tiers = 3;
            float foliageStart = trunkHeight * 0.7f;
            float tierHeight = (height - foliageStart) / tiers;

            for (int t = 0; t < tiers; t++)
            {
                float tierY = foliageStart + (t * tierHeight) + (tierHeight * 0.5f);
                float tierRadius = (height * 0.35f) * (1f - (t * 0.25f));

                GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cone.name = $"CanopyTier_{t}";
                cone.transform.SetParent(tree.transform, false);
                cone.transform.localPosition = new Vector3(0f, tierY, 0f);
                cone.transform.localScale = new Vector3(tierRadius * 2f, tierHeight * 0.65f, tierRadius * 2f);
                cone.GetComponent<Renderer>().material = _foliageMat;
                Destroy(cone.GetComponent<Collider>());
            }
        }

        private void CreateFallenLog(Transform parent, Vector3 pos, float length, float rotY)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "FallenLog";
            log.transform.SetParent(parent, false);
            log.transform.position = pos + Vector3.up * 0.45f;
            log.transform.localScale = new Vector3(0.9f, length * 0.5f, 0.9f);
            log.transform.rotation = Quaternion.Euler(0f, rotY, 90f);
            log.GetComponent<Renderer>().material = _trunkMat;
        }

        #endregion

        #region Common Helpers & Spawning

        private void SetupSpawnsAndRoutes(Transform root, Vector3 playerPos, Vector3[][] routes)
        {
            // Player Spawn
            GameObject pSpawnObj = new GameObject("PlayerSpawnPoint");
            pSpawnObj.transform.SetParent(root, false);
            pSpawnObj.transform.position = playerPos;
            PlayerSpawnPoint = pSpawnObj.transform;

            // Enemy Patrol Routes
            GameObject spawnRoot = new GameObject("EnemyPatrolRoutes");
            spawnRoot.transform.SetParent(root, false);

            for (int r = 0; r < routes.Length; r++)
            {
                GameObject routeObj = new GameObject($"Route_{r + 1}");
                routeObj.transform.SetParent(spawnRoot.transform, false);

                Vector3[] points = routes[r];
                Transform[] transforms = new Transform[points.Length];

                for (int p = 0; p < points.Length; p++)
                {
                    GameObject wp = new GameObject($"WP_{p}");
                    wp.transform.SetParent(routeObj.transform, false);
                    wp.transform.position = points[p];
                    transforms[p] = wp.transform;
                }

                EnemySpawnPoints.Add(points[0]);
                EnemyPatrolRoutes.Add(transforms);
            }
        }

        private GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool addCollider = true)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = pos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().material = mat;

            if (!addCollider)
            {
                Collider col = box.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            return box;
        }

        private void CreateCargoContainer(Transform parent, Vector3 pos, float rotY, Material mat)
        {
            GameObject container = new GameObject("CargoContainer");
            container.transform.SetParent(parent, false);
            container.transform.position = pos;
            container.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

            CreateBox(container.transform, "Body", new Vector3(0f, 1.4f, 0f), new Vector3(2.6f, 2.8f, 6.2f), mat);
        }

        #endregion
    }
}
