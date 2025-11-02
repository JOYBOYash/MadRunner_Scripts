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

    [Header("Random Trap Wall Prefabs (Optional)")]
    public GameObject[] trapWallPrefabs;
    [Range(0f, 1f)] public float trapWallChance = 0.1f;

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

        Vector2Int[] directions = {
            new Vector2Int(0, 1),   // North
            new Vector2Int(1, 0),   // East
            new Vector2Int(0, -1),  // South
            new Vector2Int(-1, 0)   // West
        };

        while (stack.Count > 0)
        {
            current = stack.Peek();
            List<Vector2Int> unvisitedNeighbours = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                int nx = current.x + dir.x;
                int ny = current.y + dir.y;
                if (nx >= 0 && ny >= 0 && nx < width && ny < height && !grid[nx, ny].visited)
                    unvisitedNeighbours.Add(dir);
            }

            if (unvisitedNeighbours.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int chosenDir = unvisitedNeighbours[rng.Next(unvisitedNeighbours.Count)];
            int newX = current.x + chosenDir.x;
            int newY = current.y + chosenDir.y;

            // Remove walls between cells
            if (chosenDir.x == 1) { grid[current.x, current.y].wallE = false; grid[newX, newY].wallW = false; }
            if (chosenDir.x == -1) { grid[current.x, current.y].wallW = false; grid[newX, newY].wallE = false; }
            if (chosenDir.y == 1) { grid[current.x, current.y].wallN = false; grid[newX, newY].wallS = false; }
            if (chosenDir.y == -1) { grid[current.x, current.y].wallS = false; grid[newX, newY].wallN = false; }

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
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);

                // North Wall
                if (grid[x, y].wallN)
                    SpawnWall(cellPos + new Vector3(0, 0, cellSize / 2f), Quaternion.identity, mazeParent);

                // South Wall
                if (grid[x, y].wallS)
                    SpawnWall(cellPos + new Vector3(0, 0, -cellSize / 2f), Quaternion.identity, mazeParent);

                // East Wall
                if (grid[x, y].wallE)
                    SpawnWall(cellPos + new Vector3(cellSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0), mazeParent);

                // West Wall
                if (grid[x, y].wallW)
                    SpawnWall(cellPos + new Vector3(-cellSize / 2f, 0, 0), Quaternion.Euler(0, 90, 0), mazeParent);
            }
        }

        // Add single long boundary walls around maze
        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;
        float wallY = wallPrefab.transform.position.y;

        // North boundary
        SpawnLongWall(new Vector3(totalWidth / 2f - cellSize / 2f, wallY, totalHeight), totalWidth, 0, mazeParent);
        // South boundary
        SpawnLongWall(new Vector3(totalWidth / 2f - cellSize / 2f, wallY, -cellSize), totalWidth, 0, mazeParent);
        // West boundary
        SpawnLongWall(new Vector3(-cellSize, wallY, totalHeight / 2f - cellSize / 2f), totalHeight, 90, mazeParent);
        // East boundary
        SpawnLongWall(new Vector3(totalWidth, wallY, totalHeight / 2f - cellSize / 2f), totalHeight, 90, mazeParent);
    }

    void SpawnWall(Vector3 pos, Quaternion rot, Transform parent)
    {
        GameObject prefabToUse = wallPrefab;
        if (trapWallPrefabs != null && trapWallPrefabs.Length > 0 && Random.value < trapWallChance)
            prefabToUse = trapWallPrefabs[Random.Range(0, trapWallPrefabs.Length)];

        Instantiate(prefabToUse, pos, rot, parent);
    }

    void SpawnLongWall(Vector3 centerPos, float length, float rotationY, Transform parent)
    {
        GameObject wall = Instantiate(wallPrefab, centerPos, Quaternion.Euler(0, rotationY, 0), parent);
        wall.transform.localScale = new Vector3(length, wall.transform.localScale.y, wall.transform.localScale.z);
    }

    // ---------------- FLOOR & PLAYER ----------------
    void BuildFloor()
    {
        if (!floorPrefab) return;

        float totalWidth = width * cellSize;
        float totalHeight = height * cellSize;

        // Center the floor exactly under the maze
        Vector3 floorCenter = new Vector3((totalWidth - cellSize) / 2f, -0.5f, (totalHeight - cellSize) / 2f);
        GameObject floor = Instantiate(floorPrefab, floorCenter, Quaternion.identity, transform);
        floor.name = "MazeFloor";

        // Exact match with maze size
        Vector3 meshSize = floor.GetComponent<MeshRenderer>().bounds.size;
        Vector3 currentScale = floor.transform.localScale;

        float scaleX = totalWidth / meshSize.x * currentScale.x;
        float scaleZ = totalHeight / meshSize.z * currentScale.z;

        floor.transform.localScale = new Vector3(scaleX, currentScale.y, scaleZ);
    }

    void SpawnPlayer()
    {
        if (!playerPrefab) return;
        Vector3 center = new Vector3((width - 1) * cellSize / 2f, 1f, (height - 1) * cellSize / 2f);
        Instantiate(playerPrefab, center, Quaternion.identity);
    }
}
