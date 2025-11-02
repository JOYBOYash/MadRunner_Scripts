using UnityEngine;
using System.Collections.Generic;

public class SlimeSpawner_3D : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject slimePrefab;
    public int maxSlimeCount = 10;
    public Vector3 spawnAreaMin;
    public Vector3 spawnAreaMax;
    public float safeRadiusFromPlayer = 5f;

    [Header("Spawn Timing")]
    public float spawnInterval = 3f;
    public float spawnCheckInterval = 1f;
    public float spawnHeightOffset = 0.5f;

    private Transform player;
    private float spawnTimer = 0f;
    private float checkTimer = 0f;
    private int currentSlimeCount = 0;
    private List<GameObject> spawnedSlimes = new List<GameObject>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!slimePrefab)
        {
            Debug.LogError("❌ SlimeSpawner_3D: Missing slime prefab!");
            enabled = false;
            return;
        }

        for (int i = 0; i < maxSlimeCount / 2; i++)
            SpawnSlime();
    }

    void Update()
    {
        // 🧱 Stop spawning once the player dies
        if (PlayerHealth.IsPlayerDead)
            return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= spawnCheckInterval)
        {
            spawnedSlimes.RemoveAll(slime => slime == null);
            currentSlimeCount = spawnedSlimes.Count;
            checkTimer = 0f;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            if (currentSlimeCount < maxSlimeCount)
                SpawnSlime();
        }
    }

    void SpawnSlime()
    {
        if (slimePrefab == null || player == null) return;

        Vector3 spawnPos = GetSafeSpawnPosition();
        Quaternion randomRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        GameObject newSlime = Instantiate(slimePrefab, spawnPos, randomRot);
        spawnedSlimes.Add(newSlime);

        Debug.Log($"🟢 Spawned Slime at {spawnPos}");
    }

    Vector3 GetSafeSpawnPosition()
    {
        Vector3 pos = Vector3.zero;
        int attempts = 0;

        do
        {
            pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnAreaMin.y + spawnHeightOffset,
                Random.Range(spawnAreaMin.z, spawnAreaMax.z)
            );
            attempts++;
        }
        while (player && Vector3.Distance(pos, player.position) < safeRadiusFromPlayer && attempts < 30);

        pos += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        return pos;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = (spawnAreaMin + spawnAreaMax) / 2f;
        Vector3 size = spawnAreaMax - spawnAreaMin;
        Gizmos.DrawWireCube(center, size);
    }
}
