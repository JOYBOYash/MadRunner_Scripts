using UnityEngine;
using System.Collections.Generic;

public class DungeonMazeSpawner_3D : MonoBehaviour
{
    [Header("Maze Settings")]
    [Tooltip("Width and height must be odd numbers for proper maze generation.")]
    public int width = 21;
    public int height = 21;
    public float cellSize = 3f;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject exitPrefab;

    [Header("Trap Wall Settings")]
    [Tooltip("List of trap wall prefabs — each can have its own unique behavior script.")]
    public GameObject[] trapWallPrefabs;

    [Range(0f, 0.3f)]
    public float trapWallChance = 0.12f; // 12% of walls become traps

    [Header("Positioning")]
    public Vector3 origin = Vector3.zero;
    public float wallHeightOffset = 1f;

    private int[,] mazeGrid; // 0 = path, 1 = wall
    private System.Random rng;

    void Start()
    {
        GenerateMaze();
        BuildMaze();
        SpawnExit();
    }

    // ---------------------- MAZE GENERATION ---------------------- //
    void GenerateMaze()
    {
        rng = new System.Random();
        mazeGrid = new int[width, height];

        // Fill everything as walls
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                mazeGrid[x, z] = 1;

        // Start point (odd cells)
        int startX = rng.Next(width / 2) * 2 + 1;
        int startZ = rng.Next(height / 2) * 2 + 1;
        CarvePath(startX, startZ);
    }

    void CarvePath(int x, int z)
    {
        mazeGrid[x, z] = 0;

        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };
        Shuffle(directions);

        foreach (var dir in directions)
        {
            int newX = x + dir.x;
            int newZ = z + dir.y;

            if (IsInBounds(newX, newZ) && mazeGrid[newX, newZ] == 1)
            {
                mazeGrid[x + dir.x / 2, z + dir.y / 2] = 0;
                CarvePath(newX, newZ);
            }
        }
    }

    bool IsInBounds(int x, int z)
    {
        return x > 0 && x < width - 1 && z > 0 && z < height - 1;
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randIndex = rng.Next(i, list.Count);
            (list[i], list[randIndex]) = (list[randIndex], list[i]);
        }
    }

    // ---------------------- MAZE BUILDING ---------------------- //
    void BuildMaze()
    {
        Transform mazeParent = new GameObject("Generated_Maze").transform;
        mazeParent.SetParent(transform);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 pos = origin + new Vector3(x * cellSize, 0, z * cellSize);

                // Always spawn floor
                if (floorPrefab)
                    Instantiate(floorPrefab, pos, Quaternion.identity, mazeParent);

                // Spawn wall
                if (mazeGrid[x, z] == 1 && wallPrefab)
                {
                    Vector3 wallPos = pos + new Vector3(0, wallHeightOffset, 0);

                    // Don’t turn outer walls into traps
                    if (x > 1 && x < width - 2 && z > 1 && z < height - 2)
                    {
                        if (trapWallPrefabs.Length > 0 && Random.value < trapWallChance)
                        {
                            // Randomly select trap type prefab
                            int trapIndex = rng.Next(trapWallPrefabs.Length);
                            GameObject selectedTrap = trapWallPrefabs[trapIndex];
                            Instantiate(selectedTrap, wallPos, Quaternion.identity, mazeParent);
                            continue;
                        }
                    }

                    // Spawn normal wall
                    Instantiate(wallPrefab, wallPos, Quaternion.identity, mazeParent);
                }
            }
        }

        Debug.Log("✅ Maze generated with multiple trap wall types!");
    }

    // ---------------------- EXIT SPAWNING ---------------------- //
    void SpawnExit()
    {
        if (!exitPrefab) return;

        Vector2Int farthestCell = new Vector2Int(1, 1);
        float maxDist = 0f;

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                if (mazeGrid[x, z] == 0)
                {
                    float dist = Vector2.Distance(new Vector2(1, 1), new Vector2(x, z));
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        farthestCell = new Vector2Int(x, z);
                    }
                }
            }
        }

        Vector3 exitPos = origin + new Vector3(farthestCell.x * cellSize, 0, farthestCell.y * cellSize);
        Instantiate(exitPrefab, exitPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            origin + new Vector3(width * cellSize / 2f, 0, height * cellSize / 2f),
            new Vector3(width * cellSize, 0.1f, height * cellSize)
        );
    }
}
