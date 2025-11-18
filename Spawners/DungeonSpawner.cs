using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
/// <summary>
/// PerfectMaze3D
/// - Maze generation (recursive backtracker)
/// - Maze build with non-overlapping wall segments that match cellSize
/// - Floor scaling to fill the maze
/// - Player spawn
/// - Strategic turret position caching + spawn/pool by distance
/// - Integrates with RoomSettings.SelectedDifficulty (safe enum mapping)
/// </summary>
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

    [Header("Multiplayer Spawn Points")]
    public Vector3 hostSpawnOffset = new Vector3(0, 1, 0);  
    public Vector3 clientSpawnOffset = new Vector3(3, 1, 3);


    [Header("Wall Fit Settings (tweak for your wall model)")]
    [Tooltip("Height of the wall (world units).")]
    public float wallHeight = 2f;
    [Tooltip("Thickness of the wall (world units). Small value like 0.15 - 0.3 usually.")]
    public float wallThickness = 0.2f;

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

    // Internal map/grid
    private Cell[,] grid;
    private System.Random rng = new System.Random();
    private Transform player;

    // Turret handling
    private List<Vector3> turretPositions = new List<Vector3>();
    private Dictionary<Vector3, GameObject> activeTurrets = new Dictionary<Vector3, GameObject>();

    // Helper to avoid duplicate walls
    private HashSet<string> spawnedWallKeys = new HashSet<string>();

    private class Cell
    {
        public bool visited = false;
        public bool wallN = true, wallS = true, wallE = true, wallW = true;
    }

    void Start()
    {
        // Try to read RoomSettings if present and map its difficulty to this script's Difficulty.
    //     RoomSettings rs = FindObjectOfType<RoomSettings>();
    // if (rs != null)
    //     difficulty = (Difficulty)rs.SelectedDifficulty.Value;

        GenerateMaze();
        BuildMaze();
        BuildFloor();
        SpawnPlayer();
        CacheTurretPositions();
        FindPlayer();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }

        // spawn / despawn turrets around the player based on spawnRange
        for (int i = 0; i < turretPositions.Count; i++)
        {
            Vector3 pos = turretPositions[i];
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

    // -------------------------
    // Maze generation
    // -------------------------
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

    // -------------------------
    // Maze build (walls + avoid duplicates)
    // -------------------------
    void BuildMaze()
    {
        // parent for maze
        Transform mazeParent = new GameObject("Generated_Maze").transform;
        mazeParent.SetParent(transform);

        spawnedWallKeys.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellCenter = new Vector3(x * cellSize, 0, y * cellSize);

                // For each side that has a wall, compute a canonical key (position + rotation)
                // and instantiate only if key wasn't already used. This prevents double walls.

                // NORTH wall (along local X axis, centered at Z + cellSize/2)
                if (grid[x, y].wallN)
                {
                    Vector3 pos = cellCenter + new Vector3(0f, wallHeight * 0.5f, cellSize * 0.5f);
                    Quaternion rot = Quaternion.identity; // facing along +Z (wall spans X)
                    TrySpawnWall(pos, rot, mazeParent);
                }

                // SOUTH wall
                if (grid[x, y].wallS)
                {
                    Vector3 pos = cellCenter + new Vector3(0f, wallHeight * 0.5f, -cellSize * 0.5f);
                    Quaternion rot = Quaternion.identity;
                    TrySpawnWall(pos, rot, mazeParent);
                }

                // EAST wall (along local Z axis, centered at X + cellSize/2)
                if (grid[x, y].wallE)
                {
                    Vector3 pos = cellCenter + new Vector3(cellSize * 0.5f, wallHeight * 0.5f, 0f);
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f); // rotated to span Z
                    TrySpawnWall(pos, rot, mazeParent);
                }

                // WEST wall
                if (grid[x, y].wallW)
                {
                    Vector3 pos = cellCenter + new Vector3(-cellSize * 0.5f, wallHeight * 0.5f, 0f);
                    Quaternion rot = Quaternion.Euler(0f, 90f, 0f);
                    TrySpawnWall(pos, rot, mazeParent);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to spawn a wall at the given pos+rot only if we haven't already spawned one
    /// near the same canonical location. This avoids duplicates where two adjacent cells
    /// would both request the same wall.
    /// Also fits wall segment scale to cellSize.
    /// </summary>
    void TrySpawnWall(Vector3 pos, Quaternion rot, Transform parent)
    {
        if (wallPrefab == null) return;

        // Build a stable key using rounded pos (to avoid floating noise) and rotation yaw
        Vector3 keyPos = new Vector3(Round(pos.x, 3), Round(pos.y, 3), Round(pos.z, 3));
        float yaw = Mathf.Round(rot.eulerAngles.y); // canonical yaw (0 or 90)
        string key = $"{keyPos.x:F3}_{keyPos.y:F3}_{keyPos.z:F3}_Y{yaw}";

        if (spawnedWallKeys.Contains(key))
            return;

        spawnedWallKeys.Add(key);

        GameObject w = Instantiate(wallPrefab, pos, rot, parent);

        // Fit wall scale so the length covers exactly cellSize and height/thickness as provided.
        // We assume the original wallPrefab was modelled with local length along X=1 unit and
        // Y=1 for height, Z=1 for thickness. If your prefab differs, adjust the scale mapping.
        Vector3 baseScale = w.transform.localScale;

        // If rotation yaw ~ 90 -> wall oriented along Z; otherwise along X
        if (Mathf.Abs(Mathf.DeltaAngle(yaw, 90f)) < 1f)
        {
            // oriented along Z: scale.z = cellSize, scale.x = thickness
            w.transform.localScale = new Vector3(wallThickness, wallHeight, cellSize);
        }
        else
        {
            // oriented along X: scale.x = cellSize, scale.z = thickness
            w.transform.localScale = new Vector3(cellSize, wallHeight, wallThickness);
        }

        // Ensure collider (if exists) matches size: try to resize BoxCollider for better collisions
        var bc = w.GetComponent<BoxCollider>();
        if (bc != null)
        {
            bc.size = new Vector3(1f, 1f, 1f); // keep 1 and rely on transform scale for final size
            bc.center = Vector3.zero;
        }
    }

    // -------------------------
    // Floor build
    // -------------------------
    void BuildFloor()
    {
        if (!floorPrefab) return;
        float w = width * cellSize;
        float h = height * cellSize;
        Vector3 center = new Vector3((w - cellSize) / 2f, -0.5f, (h - cellSize) / 2f);

        GameObject floor = Instantiate(floorPrefab, center, Quaternion.identity, transform);

        // Try to scale floor to cover entire maze area. We use renderer bounds as reference.
        var mr = floor.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Vector3 size = mr.bounds.size;
            Vector3 scale = floor.transform.localScale;

            // Protect against zero-size meshes
            if (size.x > 0.001f && size.z > 0.001f)
            {
                floor.transform.localScale = new Vector3((w / size.x) * scale.x, scale.y, (h / size.z) * scale.z);
            }
        }
    }

    // -------------------------
    // Player spawn + find
    // -------------------------
void SpawnPlayer()
{
    if (playerPrefab == null)
    {
        Debug.LogError("❌ Player Prefab missing.");
        return;
    }

    // Only host is allowed to spawn network players
    if (!NetworkManager.Singleton.IsServer)
        return;

    // Calculate maze center
    Vector3 center = new Vector3((width - 1) * cellSize / 2f, 1f, (height - 1) * cellSize / 2f);

    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
    {
        Vector3 spawnPos = (client.ClientId == NetworkManager.Singleton.LocalClientId)
            ? center + hostSpawnOffset
            : center + clientSpawnOffset;

        // Spawn network player
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

        var netObj = player.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(client.ClientId, true);
    }
}


    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    // -------------------------
    // Turret placement & spawn
    // -------------------------
    void CacheTurretPositions()
    {
        turretPositions.Clear();

        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                Cell c = grid[x, y];

                // Count open sides (no walls)
                int openSides = 0;
                if (!c.wallN) openSides++;
                if (!c.wallS) openSides++;
                if (!c.wallE) openSides++;
                if (!c.wallW) openSides++;

                // Identify cell type
                bool isDeadEnd = openSides == 1;
                bool isCorner = (openSides == 2) && (
                    (!c.wallN && !c.wallE) ||
                    (!c.wallE && !c.wallS) ||
                    (!c.wallS && !c.wallW) ||
                    (!c.wallW && !c.wallN)
                );
                bool isIntersection = openSides >= 3;

                // Strategic selection weights
                float chance = 0f;
                switch (difficulty)
                {
                    case Difficulty.Easy:
                        if (isDeadEnd) chance = 0.4f;
                        if (isCorner) chance = 0.25f;
                        if (isIntersection) chance = 0.1f;
                        break;
                    case Difficulty.Medium:
                        if (isDeadEnd) chance = 0.5f;
                        if (isCorner) chance = 0.35f;
                        if (isIntersection) chance = 0.25f;
                        break;
                    case Difficulty.Hard:
                        if (isDeadEnd) chance = 0.6f;
                        if (isCorner) chance = 0.5f;
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
            Difficulty.Easy => RandomFrom(easyTurrets),
            Difficulty.Medium => RandomFrom(mediumTurrets),
            Difficulty.Hard => RandomFrom(hardTurrets),
            _ => RandomFrom(easyTurrets)
        };

        if (prefab == null) return;

        // Rotate to face the nearest open path (simple heuristic)
        Quaternion faceDir = Quaternion.identity;
        int cx = Mathf.RoundToInt(pos.x / cellSize);
        int cy = Mathf.RoundToInt(pos.z / cellSize);
        Vector2Int cellCoord = new Vector2Int(cx, cy);

        if (cellCoord.x >= 0 && cellCoord.x < width && cellCoord.y >= 0 && cellCoord.y < height)
        {
            Cell c = grid[cellCoord.x, cellCoord.y];
            if (!c.wallN) faceDir = Quaternion.Euler(0, 0, 0);
            else if (!c.wallS) faceDir = Quaternion.Euler(0, 180, 0);
            else if (!c.wallE) faceDir = Quaternion.Euler(0, 90, 0);
            else if (!c.wallW) faceDir = Quaternion.Euler(0, -90, 0);
        }

        GameObject turret = Instantiate(prefab, pos, faceDir);
        activeTurrets[pos] = turret;
    }

    GameObject RandomFrom(GameObject[] arr) =>
        (arr != null && arr.Length > 0) ? arr[rng.Next(arr.Length)] : null;

    // -------------------------
    // Helpers
    // -------------------------
    static float Round(float v, int digits)
    {
        float mul = Mathf.Pow(10f, digits);
        return Mathf.Round(v * mul) / mul;
    }
}
