using UnityEngine;
using System.Collections.Generic;

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

    // Maze Generation
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

    // Maze Build
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

    // Floor
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

    // Player
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

    // Turret Placement
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

            // Random chance spawn
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

    // Rotate to face the nearest open path (optional but cool)
    Quaternion faceDir = Quaternion.identity;
    Vector2Int cellCoord = new Vector2Int(Mathf.RoundToInt(pos.x / cellSize), Mathf.RoundToInt(pos.z / cellSize));

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
}
