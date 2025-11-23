using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class PerfectMaze3D : MonoBehaviour
{
    [Header("Maze Settings")]
    public int width = 15;
    public int height = 15;
    public float cellSize = 3f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject playerPrefab;

    [Header("Difficulty-based Player Spacing (world units)")]
    [Tooltip("Max distance between players on EASY (they spawn close / together).")]
    public float easyMaxDistance = 6f;   // (kept for tuning, logic uses offsets in same cell)

    [Tooltip("Desired MIN distance on MEDIUM.")]
    public float mediumMinDistance = 12f;

    [Tooltip("Desired MAX distance on MEDIUM.")]
    public float mediumMaxDistance = 24f;

    [Tooltip("Minimum distance on HARD (we pick farthest pair anyway).")]
    public float hardMinDistance = 20f;

    [Header("Turret Spawn Settings")]
    public GameObject[] easyTurrets;
    public GameObject[] mediumTurrets;
    public GameObject[] hardTurrets;

    [Tooltip("How close the player must be before turrets spawn.")]
    public float spawnRange = 25f;

    [Tooltip("Vertical offset for turret height placement.")]
    public float turretHeightOffset = 0.5f;

    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty difficulty = Difficulty.Medium;

    private Cell[,] grid;
    private System.Random rng = new System.Random();
    private Transform player;

    private List<Vector3> turretPositions = new List<Vector3>();
    private Dictionary<Vector3, GameObject> activeTurrets = new Dictionary<Vector3, GameObject>();

    private class Cell
    {
        public bool visited = false;
        public bool wallN = true, wallS = true, wallE = true, wallW = true;
    }

    void Start()
    {
        GenerateMaze();     // ✅ unchanged
        BuildMaze();        // ✅ unchanged
        BuildFloor();       // ✅ unchanged

        // 🔁 NEW: if we have Netcode and this instance is the server, spawn multiplayer players.
        // Otherwise, fall back to your old single-player spawn.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            SpawnPlayersMultiplayer();
        }
        else
        {
            SpawnPlayer();
        }

        CacheTurretPositions();  // ✅ unchanged
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        foreach (var pos in turretPositions)
        {
            float dist = Vector3.Distance(player.position, pos);
            bool shouldExist = dist <= spawnRange;
            bool exists = activeTurrets.ContainsKey(pos);

            if (shouldExist && !exists)
                SpawnTurretAt(pos);

            if (!shouldExist && exists)
            {
                Destroy(activeTurrets[pos]);
                activeTurrets.Remove(pos);
            }
        }
    }

    // =========================================================
    //  MAZE GENERATION  (UNCHANGED)
    // =========================================================
    void GenerateMaze()
    {
        grid = new Cell[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = new Cell();

        Vector2Int current = new Vector2Int(rng.Next(width), rng.Next(height));
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        grid[current.x, current.y].visited = true;
        stack.Push(current);

        Vector2Int[] dirs = {
            new Vector2Int(0, 1), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        while (stack.Count > 0)
        {
            current = stack.Peek();
            List<Vector2Int> unvisited = new List<Vector2Int>();

            foreach (var d in dirs)
            {
                int nx = current.x + d.x;
                int ny = current.y + d.y;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height && !grid[nx, ny].visited)
                    unvisited.Add(d);
            }

            if (unvisited.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var chosen = unvisited[rng.Next(unvisited.Count)];
            int newX = current.x + chosen.x;
            int newY = current.y + chosen.y;

            if (chosen.x == 1) { grid[current.x, current.y].wallE = false; grid[newX, newY].wallW = false; }
            if (chosen.x == -1) { grid[current.x, current.y].wallW = false; grid[newX, newY].wallE = false; }
            if (chosen.y == 1) { grid[current.x, current.y].wallN = false; grid[newX, newY].wallS = false; }
            if (chosen.y == -1) { grid[current.x, current.y].wallS = false; grid[newX, newY].wallN = false; }

            grid[newX, newY].visited = true;
            stack.Push(new Vector2Int(newX, newY));
        }
    }

    // =========================================================
    //  MAZE BUILD  (UNCHANGED)
    // =========================================================
    void BuildMaze()
    {
        Transform mazeParent = new GameObject("Generated_Maze").transform;
        mazeParent.SetParent(transform);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                if (grid[x, y].wallN) Instantiate(wallPrefab, pos + new Vector3(0, 0, cellSize / 2f), Quaternion.identity, mazeParent);
                if (grid[x, y].wallS) Instantiate(wallPrefab, pos + new Vector3(0, 0, -cellSize / 2f), Quaternion.identity, mazeParent);
                if (grid[x, y].wallE) Instantiate(wallPrefab, pos + new Vector3(cellSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0), mazeParent);
                if (grid[x, y].wallW) Instantiate(wallPrefab, pos + new Vector3(-cellSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0), mazeParent);
            }
    }

    // =========================================================
    //  FLOOR  (UNCHANGED)
    // =========================================================
    void BuildFloor()
    {
        if (!floorPrefab) return;
        float w = width * cellSize;
        float h = height * cellSize;
        Vector3 center = new Vector3((w - cellSize) / 2f, -0.5f, (h - cellSize) / 2f);

        GameObject floor = Instantiate(floorPrefab, center, Quaternion.identity, transform);
        Vector3 size = floor.GetComponent<MeshRenderer>().bounds.size;
        Vector3 scale = floor.transform.localScale;

        floor.transform.localScale = new Vector3(w / size.x * scale.x, scale.y, h / size.z * scale.z);
    }

    // =========================================================
    //  ORIGINAL SINGLE-PLAYER SPAWN  (KEPT AS FALLBACK)
    // =========================================================
    void SpawnPlayer()
    {
        if (!playerPrefab) return;
        Vector3 c = new Vector3((width - 1) * cellSize / 2f, 1f, (height - 1) * cellSize / 2f);
        Instantiate(playerPrefab, c, Quaternion.identity);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    // =========================================================
    //  🔥 NEW: MULTIPLAYER SPAWN (BASED ON DIFFICULTY)
    // =========================================================
    void SpawnPlayersMultiplayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("❌ Player Prefab missing on PerfectMaze3D.");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            // Only the server/host spawns player objects
            return;
        }

        // All cells that have at least one open side = walkable/path cells
        List<Vector2Int> pathCells = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell c = grid[x, y];
                bool hasPath = !c.wallN || !c.wallS || !c.wallE || !c.wallW;
                if (hasPath)
                    pathCells.Add(new Vector2Int(x, y));
            }
        }

        if (pathCells.Count == 0)
        {
            Debug.LogError("❌ No valid path cells found for spawning players.");
            return;
        }

        Vector3 hostWorldPos, clientWorldPos;
        ChooseSpawnPositionsByDifficulty(pathCells, out hostWorldPos, out clientWorldPos);

        // Spawn for each connected client
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Vector3 spawnPos = (client.ClientId == NetworkManager.Singleton.LocalClientId)
                ? hostWorldPos
                : clientWorldPos;

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            var netObj = playerObj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError("❌ PlayerPrefab has no NetworkObject component!");
                Destroy(playerObj);
                continue;
            }

            netObj.SpawnAsPlayerObject(client.ClientId, true);
        }
    }

    /// <summary>
    /// Easy  -> both players in same cell, side-by-side  
    /// Medium -> moderate distance between path cells  
    /// Hard  -> farthest two path cells in the maze
    /// </summary>
    void ChooseSpawnPositionsByDifficulty(List<Vector2Int> pathCells, out Vector3 hostPos, out Vector3 clientPos)
    {
        // Converts grid coords to world center of cell
        System.Func<Vector2Int, Vector3> CellToWorld = cell =>
            new Vector3(cell.x * cellSize, 1f, cell.y * cellSize);

        if (difficulty == Difficulty.Easy)
        {
            // Both in same cell, small offset so they’re side-by-side
            Vector2Int cell = pathCells[rng.Next(pathCells.Count)];
            Vector3 basePos = CellToWorld(cell);

            hostPos   = basePos + new Vector3(-0.75f, 0f, 0f);
            clientPos = basePos + new Vector3( 0.75f, 0f, 0f);
            return;
        }

        if (difficulty == Difficulty.Hard)
        {
            // Find the farthest pair of path cells
            float maxDistSq = 0f;
            Vector2Int cA = pathCells[0], cB = pathCells[0];

            for (int i = 0; i < pathCells.Count; i++)
            {
                for (int j = i + 1; j < pathCells.Count; j++)
                {
                    float dx = (pathCells[i].x - pathCells[j].x) * cellSize;
                    float dz = (pathCells[i].y - pathCells[j].y) * cellSize;
                    float distSq = dx * dx + dz * dz;

                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                        cA = pathCells[i];
                        cB = pathCells[j];
                    }
                }
            }

            hostPos   = CellToWorld(cA);
            clientPos = CellToWorld(cB);

            if (Vector3.Distance(hostPos, clientPos) < hardMinDistance)
            {
                Debug.LogWarning("⚠ HARD: Maze too small for huge separation, using max-distance pair anyway.");
            }

            return;
        }

        // MEDIUM difficulty: try to find a pair within [mediumMinDistance, mediumMaxDistance]
        Vector2Int anchor = pathCells[rng.Next(pathCells.Count)];
        Vector3 anchorWorld = CellToWorld(anchor);

        List<Vector2Int> mediumCandidates = new List<Vector2Int>();
        foreach (var cell in pathCells)
        {
            if (cell == anchor) continue;
            Vector3 pos = CellToWorld(cell);
            float dist = Vector3.Distance(anchorWorld, pos);
            if (dist >= mediumMinDistance && dist <= mediumMaxDistance)
                mediumCandidates.Add(cell);
        }

        Vector2Int secondCell;
        if (mediumCandidates.Count > 0)
        {
            secondCell = mediumCandidates[rng.Next(mediumCandidates.Count)];
        }
        else
        {
            // Fallback: pick farthest from anchor if there are no "medium" candidates
            float best = 0f;
            secondCell = anchor;

            foreach (var cell in pathCells)
            {
                if (cell == anchor) continue;
                float dx = (cell.x - anchor.x) * cellSize;
                float dz = (cell.y - anchor.y) * cellSize;
                float d2 = dx * dx + dz * dz;
                if (d2 > best)
                {
                    best = d2;
                    secondCell = cell;
                }
            }
        }

        hostPos   = anchorWorld;
        clientPos = CellToWorld(secondCell);
    }

    // =========================================================
    //  TURRETS (UNCHANGED)
    // =========================================================
    void CacheTurretPositions()
    {
        turretPositions.Clear();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                Cell c = grid[x, y];

                int openSides = 0;
                if (!c.wallN) openSides++;
                if (!c.wallS) openSides++;
                if (!c.wallE) openSides++;
                if (!c.wallW) openSides++;

                bool isDeadEnd = openSides == 1;
                bool isCorner = (openSides == 2) && (
                    (!c.wallN && !c.wallE) ||
                    (!c.wallE && !c.wallS) ||
                    (!c.wallS && !c.wallW) ||
                    (!c.wallW && !c.wallN)
                );
                bool isIntersection = openSides >= 3;

                float chance = 0f;
                switch (difficulty)
                {
                    case Difficulty.Easy:
                        if (isDeadEnd) chance = 0.4f;
                        if (isCorner)   chance = 0.25f;
                        if (isIntersection) chance = 0.1f;
                        break;
                    case Difficulty.Medium:
                        if (isDeadEnd) chance = 0.5f;
                        if (isCorner)   chance = 0.35f;
                        if (isIntersection) chance = 0.25f;
                        break;
                    case Difficulty.Hard:
                        if (isDeadEnd) chance = 0.6f;
                        if (isCorner)   chance = 0.5f;
                        if (isIntersection) chance = 0.4f;
                        break;
                }

                if (Random.value < chance)
                {
                    Vector3 pos = new Vector3(x * cellSize, turretHeightOffset, y * cellSize);
                    turretPositions.Add(pos);
                }
            }
        }

        Debug.Log($"🎯 Cached {turretPositions.Count} strategic turret positions.");
    }

    void SpawnTurretAt(Vector3 pos)
    {
        GameObject prefab = difficulty switch
        {
            Difficulty.Easy   => RandomFrom(easyTurrets),
            Difficulty.Medium => RandomFrom(mediumTurrets),
            Difficulty.Hard   => RandomFrom(hardTurrets),
            _                 => RandomFrom(easyTurrets)
        };

        if (prefab == null) return;

        Quaternion faceDir = Quaternion.identity;
        Vector2Int cellCoord = new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.z / cellSize));

        if (cellCoord.x >= 0 && cellCoord.x < width && cellCoord.y >= 0 && cellCoord.y < height)
        {
            Cell c = grid[cellCoord.x, cellCoord.y];
            if (!c.wallN)      faceDir = Quaternion.Euler(0, 0, 0);
            else if (!c.wallS) faceDir = Quaternion.Euler(0, 180, 0);
            else if (!c.wallE) faceDir = Quaternion.Euler(0, 90, 0);
            else if (!c.wallW) faceDir = Quaternion.Euler(0, -90, 0);
        }

        GameObject turret = Instantiate(prefab, pos, faceDir);
        activeTurrets[pos] = turret;
    }

    GameObject RandomFrom(GameObject[] arr) =>
        (arr != null && arr.Length > 0) ? arr[rng.Next(arr.Length)] : null;
}
