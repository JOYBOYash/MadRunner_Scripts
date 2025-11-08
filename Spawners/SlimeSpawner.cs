using UnityEngine;
using System.Collections.Generic;

public class SlimeSpawner_3D : MonoBehaviour
{
    [System.Serializable]
    public class SlimeType
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float spawnChance = 0.5f;
    }

    [Header("Slime Types (Add as many as you want)")]
    public List<SlimeType> slimeTypes;

    [Header("Population Settings")]
    public int totalSlimesToSpawn = 25;
    public float minimumDistanceBetweenSlimes = 2f;
    public float safeRadiusFromPlayer = 6f;

    [Header("Maze Area")]
    public Vector3 areaMin = new Vector3(-20, 20, -20);
    public Vector3 areaMax = new Vector3(20, 20, 20);

    [Header("Ground Detection")]
    public LayerMask groundLayer; 
    public float raycastDownDistance = 50f;

    private Transform player;
    private readonly List<GameObject> activeSlimes = new List<GameObject>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (slimeTypes.Count == 0)
        {
            Debug.LogError("❌ No slime types assigned.");
            enabled = false;
            return;
        }

        SpawnEntireMazePopulation();
    }

    void SpawnEntireMazePopulation()
    {
        int attempts = 0;

        while (activeSlimes.Count < totalSlimesToSpawn && attempts < totalSlimesToSpawn * 15)
        {
            attempts++;
            TrySpawnOneSlime();
        }

        Debug.Log($"✅ Finished populating maze with {activeSlimes.Count} slimes.");
    }

    void TrySpawnOneSlime()
    {
        GameObject slimePrefab = PickSlimeByWeight();
        if (slimePrefab == null) return;

        Vector3 spawnPoint;
        if (!FindValidGroundPosition(out spawnPoint)) return;

        GameObject slime = Instantiate(slimePrefab, spawnPoint, Quaternion.identity);
        activeSlimes.Add(slime);
    }

    GameObject PickSlimeByWeight()
    {
        float total = 0f;
        foreach (var s in slimeTypes) total += s.spawnChance;

        float r = Random.value * total;
        float running = 0f;

        foreach (var s in slimeTypes)
        {
            running += s.spawnChance;
            if (r <= running)
                return s.prefab;
        }
        return slimeTypes[Random.Range(0, slimeTypes.Count)].prefab;
    }

    bool FindValidGroundPosition(out Vector3 result)
    {
        for (int i = 0; i < 50; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(areaMin.x, areaMax.x),
                areaMax.y,
                Random.Range(areaMin.z, areaMax.z)
            );

            if (Physics.Raycast(randomPoint, Vector3.down, out RaycastHit hit, raycastDownDistance, groundLayer))
            {
                Vector3 pos = hit.point;

                if (player && Vector3.Distance(pos, player.position) < safeRadiusFromPlayer)
                    continue;

                foreach (var s in activeSlimes)
                {
                    if (s && Vector3.Distance(pos, s.transform.position) < minimumDistanceBetweenSlimes)
                        goto NextTry;
                }

                result = pos;
                return true;
            }
        NextTry: ;
        }

        result = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (areaMin + areaMax) / 2f;
        Vector3 size = areaMax - areaMin;
        Gizmos.DrawWireCube(center, size);
    }
}
