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

    [Header("Slime Spawning Settings")]
    public GameObject[] basicSlimes;
    public GameObject[] fastSlimes;
    public GameObject[] turretSlimes;

    [Tooltip("Rotation applied ONLY to turret slimes.")]
    public Vector3 turretSpawnRotation = new Vector3(0, 90, 0);

    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty difficulty = Difficulty.Medium;

    private Cell[,] grid;
    private System.Random rng = new System.Random();

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
        SpawnSlimesStrategically();
    }

    // ---------------- MAZE GENERATION ----------------
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

    // ---------------- MAZE BUILD ----------------
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

    // ---------------- FLOOR & PLAYER ----------------
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

    void SpawnPlayer()
    {
        if (!playerPrefab) return;
        Vector3 c = new Vector3((width - 1) * cellSize / 2f, 1f, (height - 1) * cellSize / 2f);
        Instantiate(playerPrefab, c, Quaternion.identity);
    }

    // ---------------- SLIME SPAWNING ----------------
    void SpawnSlimesStrategically()
    {
        List<Vector3> points = new List<Vector3>();

        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
            {
                bool intersection = (!grid[x, y].wallN && !grid[x, y].wallS) ||
                                    (!grid[x, y].wallE && !grid[x, y].wallW);

                if (intersection)
                    points.Add(new Vector3(x * cellSize, 0, y * cellSize));
            }

        int count = difficulty switch
        {
            Difficulty.Easy => points.Count / 8,
            Difficulty.Medium => points.Count / 4,
            Difficulty.Hard => points.Count / 2,
            _ => 5
        };

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = points[rng.Next(points.Count)] + Vector3.up * 0.5f;
            GameObject prefab = ChooseSlimePrefab();

            Quaternion rot = prefab != null && IsTurret(prefab)
                ? Quaternion.Euler(turretSpawnRotation)
                : Quaternion.identity;

            Instantiate(prefab, pos, rot);
        }
    }

    GameObject ChooseSlimePrefab()
    {
        return difficulty switch
        {
            Difficulty.Easy => RandomFrom(basicSlimes),
            Difficulty.Medium => (Random.value < 0.6f ? RandomFrom(basicSlimes) : RandomFrom(fastSlimes)),
            Difficulty.Hard => (Random.value < 0.4f ? RandomFrom(turretSlimes) : RandomFrom(fastSlimes)),
            _ => RandomFrom(basicSlimes)
        };
    }

    GameObject RandomFrom(GameObject[] arr) => (arr != null && arr.Length > 0) ? arr[rng.Next(arr.Length)] : null;
    bool IsTurret(GameObject prefab) => System.Array.IndexOf(turretSlimes, prefab) >= 0;
}
