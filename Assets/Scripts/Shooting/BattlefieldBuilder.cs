using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Procedurally constructs an optimized 3D military battlefield test map.
    /// Builds compound buildings, asphalt roads, shipping containers, concrete cover walls,
    /// supply crates, barrels, and enemy patrol waypoints.
    /// </summary>
    public class BattlefieldBuilder : MonoBehaviour
    {
        #region Palette & Material Cache

        private Material _groundMat;
        private Material _roadMat;
        private Material _buildingMat;
        private Material _roofMat;
        private Material _containerBlueMat;
        private Material _containerRedMat;
        private Material _concreteMat;
        private Material _crateMat;
        private Material _barrelMat;
        private Material _fenceMat;

        #endregion

        #region Public Properties

        public List<Transform[]> EnemyPatrolRoutes { get; private set; } = new List<Transform[]>();
        public Transform PlayerSpawnPoint { get; private set; }
        public List<Vector3> EnemySpawnPoints { get; private set; } = new List<Vector3>();

        #endregion

        #region Public Generation Entry

        public void BuildBattlefield()
        {
            if (transform.Find("BattlefieldRoot") != null)
            {
                Debug.Log("[BattlefieldBuilder] Battlefield already exists. Skipping build.");
                return;
            }

            Debug.Log("[BattlefieldBuilder] Constructing optimized 3D military battlefield map...");

            InitMaterials();

            GameObject root = new GameObject("BattlefieldRoot");
            root.transform.SetParent(transform, false);

            BuildGroundAndRoads(root.transform);
            BuildPerimeterFences(root.transform);
            BuildMilitaryBuildings(root.transform);
            BuildCargoContainers(root.transform);
            BuildCoverBarriersAndCrates(root.transform);
            BuildWaypointsAndSpawns(root.transform);

            Debug.Log("[BattlefieldBuilder] 3D Battlefield map generation complete!");
        }

        #endregion

        #region Ground & Roads

        private void BuildGroundAndRoads(Transform parent)
        {
            // Main Battlefield Ground (120m x 120m)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "BattlefieldGround";
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(120f, 1f, 120f);
            ground.GetComponent<Renderer>().material = _groundMat;

            // Central Crossroad (North-South & East-West)
            GameObject roadNS = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadNS.name = "Road_NorthSouth";
            roadNS.transform.SetParent(parent, false);
            roadNS.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            roadNS.transform.localScale = new Vector3(14f, 0.05f, 116f);
            roadNS.GetComponent<Renderer>().material = _roadMat;
            Destroy(roadNS.GetComponent<Collider>());

            GameObject roadEW = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roadEW.name = "Road_EastWest";
            roadEW.transform.SetParent(parent, false);
            roadEW.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            roadEW.transform.localScale = new Vector3(116f, 0.05f, 14f);
            roadEW.GetComponent<Renderer>().material = _roadMat;
            Destroy(roadEW.GetComponent<Collider>());
        }

        private void BuildPerimeterFences(Transform parent)
        {
            float halfSize = 58f;
            float fenceHeight = 4f;

            CreateWall(parent, "Fence_North", new Vector3(0f, fenceHeight * 0.5f, halfSize), new Vector3(116f, fenceHeight, 1.2f), _fenceMat);
            CreateWall(parent, "Fence_South", new Vector3(0f, fenceHeight * 0.5f, -halfSize), new Vector3(116f, fenceHeight, 1.2f), _fenceMat);
            CreateWall(parent, "Fence_East", new Vector3(halfSize, fenceHeight * 0.5f, 0f), new Vector3(1.2f, fenceHeight, 116f), _fenceMat);
            CreateWall(parent, "Fence_West", new Vector3(-halfSize, fenceHeight * 0.5f, 0f), new Vector3(1.2f, fenceHeight, 116f), _fenceMat);
        }

        #endregion

        #region Buildings & Watchtowers

        private void BuildMilitaryBuildings(Transform parent)
        {
            // Building 1: Main Armory / Warehouse (North-East sector)
            CreateBuilding(parent, "Warehouse_Armory", new Vector3(28f, 0f, 28f), new Vector3(22f, 8f, 18f), _buildingMat, _roofMat);

            // Building 2: Tactical Headquarters (North-West sector)
            CreateBuilding(parent, "Tactical_HQ", new Vector3(-28f, 0f, 28f), new Vector3(20f, 10f, 20f), _buildingMat, _roofMat);

            // Building 3: Communications Bunker (South-West sector)
            CreateBuilding(parent, "Comms_Bunker", new Vector3(-28f, 0f, -28f), new Vector3(18f, 6.5f, 16f), _buildingMat, _roofMat);

            // Watchtowers at tactical vantage points
            CreateWatchtower(parent, "Watchtower_NE", new Vector3(45f, 0f, 45f));
            CreateWatchtower(parent, "Watchtower_NW", new Vector3(-45f, 0f, 45f));
            CreateWatchtower(parent, "Watchtower_SW", new Vector3(-45f, 0f, -45f));
            CreateWatchtower(parent, "Watchtower_SE", new Vector3(45f, 0f, -45f));
        }

        private void CreateBuilding(Transform parent, string name, Vector3 pos, Vector3 size, Material wallMat, Material roofMat)
        {
            GameObject bld = new GameObject(name);
            bld.transform.SetParent(parent, false);
            bld.transform.position = pos;

            // Main Walls Box
            GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.name = "Walls";
            walls.transform.SetParent(bld.transform, false);
            walls.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            walls.transform.localScale = size;
            walls.GetComponent<Renderer>().material = wallMat;

            // Overhanging Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(bld.transform, false);
            roof.transform.localPosition = new Vector3(0f, size.y + 0.25f, 0f);
            roof.transform.localScale = new Vector3(size.x + 1.6f, 0.5f, size.z + 1.6f);
            roof.GetComponent<Renderer>().material = roofMat;

            // Entrance Door Alcove
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "EntranceBay";
            door.transform.SetParent(bld.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.6f, -size.z * 0.5f - 0.1f);
            door.transform.localScale = new Vector3(3.5f, 3.2f, 0.3f);
            door.GetComponent<Renderer>().material = _roofMat;
            Destroy(door.GetComponent<Collider>());
        }

        private void CreateWatchtower(Transform parent, string name, Vector3 pos)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.position = pos;

            // 4 Corner Pillars
            float h = 9f;
            float span = 2.4f;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.transform.SetParent(tower.transform, false);
                    pillar.transform.localPosition = new Vector3(x * span, h * 0.5f, z * span);
                    pillar.transform.localScale = new Vector3(0.35f, h * 0.5f, 0.35f);
                    pillar.GetComponent<Renderer>().material = _concreteMat;
                }
            }

            // Platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.SetParent(tower.transform, false);
            platform.transform.localPosition = new Vector3(0f, h, 0f);
            platform.transform.localScale = new Vector3(6.5f, 0.4f, 6.5f);
            platform.GetComponent<Renderer>().material = _roofMat;

            // Guard Railing
            CreateWall(tower.transform, "Railing_N", new Vector3(0f, h + 0.6f, 3.1f), new Vector3(6.2f, 1.2f, 0.2f), _concreteMat);
            CreateWall(tower.transform, "Railing_S", new Vector3(0f, h + 0.6f, -3.1f), new Vector3(6.2f, 1.2f, 0.2f), _concreteMat);
            CreateWall(tower.transform, "Railing_E", new Vector3(3.1f, h + 0.6f, 0f), new Vector3(0.2f, 1.2f, 6.2f), _concreteMat);
            CreateWall(tower.transform, "Railing_W", new Vector3(-3.1f, h + 0.6f, 0f), new Vector3(0.2f, 1.2f, 6.2f), _concreteMat);
        }

        #endregion

        #region Cargo Containers & Cover

        private void BuildCargoContainers(Transform parent)
        {
            // Shipping containers placed at tactical intersections
            // Blue Container (East Road Chokepoint)
            CreateCargoContainer(parent, "Container_Blue_1", new Vector3(16f, 0f, 4f), 15f, _containerBlueMat);
            CreateCargoContainer(parent, "Container_Blue_2", new Vector3(17f, 2.7f, 4f), 15f, _containerBlueMat);

            // Red Container (West Road Chokepoint)
            CreateCargoContainer(parent, "Container_Red_1", new Vector3(-16f, 0f, -4f), -25f, _containerRedMat);
            CreateCargoContainer(parent, "Container_Red_2", new Vector3(-15f, 0f, 6f), 70f, _containerRedMat);

            // Center Courtyard Containers
            CreateCargoContainer(parent, "Container_Center_1", new Vector3(5f, 0f, 18f), 0f, _containerBlueMat);
            CreateCargoContainer(parent, "Container_Center_2", new Vector3(-6f, 0f, -18f), 90f, _containerRedMat);
        }

        private void CreateCargoContainer(Transform parent, string name, Vector3 pos, float rotY, Material mat)
        {
            GameObject container = GameObject.CreatePrimitive(PrimitiveType.Cube);
            container.name = name;
            container.transform.SetParent(parent, false);
            container.transform.position = pos + Vector3.up * 1.3f;
            container.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            container.transform.localScale = new Vector3(2.6f, 2.6f, 6.5f);
            container.GetComponent<Renderer>().material = mat;
        }

        private void BuildCoverBarriersAndCrates(Transform parent)
        {
            // Concrete Jersey Barriers for chest-height crouch/aim cover
            Vector3[] barrierPositions = new Vector3[]
            {
                new Vector3(4f, 0f, 6f),
                new Vector3(-4f, 0f, -6f),
                new Vector3(0f, 0f, 12f),
                new Vector3(0f, 0f, -12f),
                new Vector3(14f, 0f, -14f),
                new Vector3(-14f, 0f, 14f),
                new Vector3(28f, 0f, 0f),
                new Vector3(-28f, 0f, 0f)
            };

            for (int i = 0; i < barrierPositions.Length; i++)
            {
                CreateJerseyBarrier(parent, $"Barrier_{i}", barrierPositions[i], (i % 2 == 0) ? 0f : 90f);
            }

            // Supply Crates & Fuel Barrels
            Vector3[] crateStacks = new Vector3[]
            {
                new Vector3(8f, 0f, 8f),
                new Vector3(-8f, 0f, -8f),
                new Vector3(22f, 0f, 18f),
                new Vector3(-22f, 0f, 20f),
                new Vector3(18f, 0f, -22f),
                new Vector3(-18f, 0f, -22f)
            };

            foreach (var pos in crateStacks)
            {
                CreateCrateCluster(parent, pos);
            }
        }

        private void CreateJerseyBarrier(Transform parent, string name, Vector3 pos, float rotY)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.SetParent(parent, false);
            barrier.transform.position = pos + Vector3.up * 0.6f;
            barrier.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            barrier.transform.localScale = new Vector3(4.2f, 1.2f, 0.6f);
            barrier.GetComponent<Renderer>().material = _concreteMat;
        }

        private void CreateCrateCluster(Transform parent, Vector3 pos)
        {
            // Base crate
            GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1.transform.SetParent(parent, false);
            c1.transform.position = pos + Vector3.up * 0.65f;
            c1.transform.localScale = Vector3.one * 1.3f;
            c1.GetComponent<Renderer>().material = _crateMat;

            // Second crate stacked
            GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2.transform.SetParent(parent, false);
            c2.transform.position = pos + new Vector3(0.9f, 0.65f, 0f);
            c2.transform.localScale = Vector3.one * 1.3f;
            c2.GetComponent<Renderer>().material = _crateMat;

            // Fuel Barrel next to crates
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(parent, false);
            barrel.transform.position = pos + new Vector3(-0.8f, 0.65f, 0.8f);
            barrel.transform.localScale = new Vector3(0.75f, 0.65f, 0.75f);
            barrel.GetComponent<Renderer>().material = _barrelMat;
        }

        private void CreateWall(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().material = mat;
        }

        #endregion

        #region Waypoints & Spawn Points

        private void BuildWaypointsAndSpawns(Transform parent)
        {
            GameObject spawnRoot = new GameObject("SpawnPointsAndWaypoints");
            spawnRoot.transform.SetParent(parent, false);

            // Player Spawn Point: South Road entry
            GameObject pSpawn = new GameObject("PlayerSpawn");
            pSpawn.transform.SetParent(spawnRoot.transform, false);
            pSpawn.transform.position = new Vector3(0f, 0.1f, -38f);
            pSpawn.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            PlayerSpawnPoint = pSpawn.transform;

            // 5 Strategic Enemy Spawn Locations & Patrol Routes
            EnemySpawnPoints.Clear();
            EnemyPatrolRoutes.Clear();

            // Route 1: North Courtyard Sentinel
            CreatePatrolRoute(spawnRoot.transform, "Patrol_North", new Vector3[]
            {
                new Vector3(0f, 0f, 26f),
                new Vector3(12f, 0f, 32f),
                new Vector3(-12f, 0f, 32f)
            });

            // Route 2: North-East Warehouse Guard
            CreatePatrolRoute(spawnRoot.transform, "Patrol_East", new Vector3[]
            {
                new Vector3(28f, 0f, 12f),
                new Vector3(36f, 0f, 22f),
                new Vector3(20f, 0f, 16f)
            });

            // Route 3: North-West HQ Sentinel
            CreatePatrolRoute(spawnRoot.transform, "Patrol_West", new Vector3[]
            {
                new Vector3(-28f, 0f, 12f),
                new Vector3(-36f, 0f, 22f),
                new Vector3(-20f, 0f, 16f)
            });

            // Route 4: South-East Containers Patroller
            CreatePatrolRoute(spawnRoot.transform, "Patrol_SE", new Vector3[]
            {
                new Vector3(22f, 0f, -14f),
                new Vector3(34f, 0f, -24f),
                new Vector3(15f, 0f, -22f)
            });

            // Route 5: Center Road Vanguard
            CreatePatrolRoute(spawnRoot.transform, "Patrol_Center", new Vector3[]
            {
                new Vector3(0f, 0f, 6f),
                new Vector3(-10f, 0f, 0f),
                new Vector3(10f, 0f, 0f)
            });
        }

        private void CreatePatrolRoute(Transform parent, string name, Vector3[] points)
        {
            GameObject routeObj = new GameObject(name);
            routeObj.transform.SetParent(parent, false);

            Transform[] transforms = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject wp = new GameObject($"WP_{i}");
                wp.transform.SetParent(routeObj.transform, false);
                wp.transform.position = points[i];
                transforms[i] = wp.transform;
            }

            EnemySpawnPoints.Add(points[0]);
            EnemyPatrolRoutes.Add(transforms);
        }

        #endregion

        #region Material Initialization

        private void InitMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            _groundMat = new Material(litShader) { color = new Color(0.18f, 0.20f, 0.24f) }; // Dark asphalt
            _roadMat = new Material(litShader) { color = new Color(0.12f, 0.14f, 0.17f) };   // Smooth roadway
            _buildingMat = new Material(litShader) { color = new Color(0.24f, 0.28f, 0.35f) }; // Military compound walls
            _roofMat = new Material(litShader) { color = new Color(0.08f, 0.10f, 0.14f) };     // Dark carbon roofs
            _concreteMat = new Material(litShader) { color = new Color(0.38f, 0.40f, 0.45f) }; // Concrete barriers
            _crateMat = new Material(litShader) { color = new Color(0.42f, 0.32f, 0.18f) };    // Military wooden crate
            _barrelMat = new Material(litShader) { color = new Color(0.72f, 0.18f, 0.18f) };   // Hazard red barrel
            _fenceMat = new Material(litShader) { color = new Color(0.06f, 0.08f, 0.12f) };    // Security boundary

            // YASH GAME signature theme containers
            _containerBlueMat = new Material(litShader) { color = new Color(0.05f, 0.35f, 0.65f) }; // Cyan/Blue container
            _containerRedMat = new Material(litShader) { color = new Color(0.65f, 0.12f, 0.22f) };  // Crimson container
        }

        #endregion
    }
}
