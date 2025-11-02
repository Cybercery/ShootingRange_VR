using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] targetPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxActiveTargets = 5;

    [Header("Target Movement")]
    [SerializeField] private bool enableMovement = true;
    [SerializeField] private float movementChance = 0.7f;
    [SerializeField] private float minMoveSpeed = 1f;
    [SerializeField] private float maxMoveSpeed = 3f;
    [SerializeField] private float moveDistance = 3f;

    [Header("Difficulty")]
    [SerializeField] private float targetLifetime = 5f;
    [SerializeField] private bool increaseDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.95f;
    [SerializeField] private float minimumSpawnInterval = 0.5f;

    private int activeTargetCount = 0;
    private float currentSpawnInterval;
    private bool isSpawning = false;
    private List<Transform> availableSpawnPoints = new List<Transform>();

    void Start()
    {
        currentSpawnInterval = spawnInterval;

        if (spawnPoints.Length > 0)
        {
            availableSpawnPoints.AddRange(spawnPoints);
            Debug.Log($"TargetSpawner initialized with {spawnPoints.Length} spawn points");
        }
        else
        {
            Debug.LogError("TargetSpawner: No spawn points assigned!");
        }

        if (targetPrefabs.Length == 0)
        {
            Debug.LogError("TargetSpawner: No target prefabs assigned!");
        }
    }

    public void StartSpawning()
    {
        if (isSpawning)
        {
            Debug.LogWarning("Already spawning!");
            return;
        }

        isSpawning = true;
        currentSpawnInterval = spawnInterval; // Reset difficulty
        Debug.Log("TargetSpawner: Starting spawn routine");
        StartCoroutine(ContinuousSpawnRoutine());
    }

    public void StopSpawning()
    {
        Debug.Log("TargetSpawner: Stopping spawn routine");
        isSpawning = false;
        StopAllCoroutines();
    }

    IEnumerator ContinuousSpawnRoutine()
    {
        while (isSpawning)
        {
            yield return new WaitForSeconds(currentSpawnInterval);

            if (activeTargetCount < maxActiveTargets && isSpawning)
            {
                SpawnTarget();

                if (increaseDifficulty)
                {
                    currentSpawnInterval = Mathf.Max(
                        minimumSpawnInterval,
                        currentSpawnInterval * difficultyIncreaseRate
                    );
                }
            }
        }
    }

    public void SpawnTarget()
    {
        if (targetPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogError("Cannot spawn - missing prefabs or spawn points!");
            return;
        }

        // Pick random target prefab
        GameObject prefab = targetPrefabs[Random.Range(0, targetPrefabs.Length)];

        // Pick random spawn point
        Transform spawnPoint = GetRandomSpawnPoint();

        // Spawn target
        GameObject targetObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        activeTargetCount++;

        Debug.Log($"Spawned target at {spawnPoint.name} (Active: {activeTargetCount})");

        // Register with score manager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterTarget();
        }

        // Configure target
        Target targetScript = targetObj.GetComponent<Target>();
        if (targetScript != null)
        {
            // Add movement if enabled
            if (enableMovement && Random.value < movementChance)
            {
                Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
                Vector3 randomDirection = directions[Random.Range(0, directions.Length)];
                float randomSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

                targetScript.SetMovement(true, randomDirection, randomSpeed, moveDistance);
            }

            // Subscribe to hit event to track destruction
            targetScript.onTargetHit.AddListener((points, isWeakPoint) =>
            {
                OnTargetHit(targetObj);
            });
        }
        else
        {
            Debug.LogError($"Spawned target {targetObj.name} has no Target script!");
        }

        // Auto-destroy after lifetime
        StartCoroutine(DestroyAfterTime(targetObj, targetLifetime));
    }

    Transform GetRandomSpawnPoint()
    {
        if (availableSpawnPoints.Count == 0)
        {
            availableSpawnPoints.AddRange(spawnPoints);
        }

        int randomIndex = Random.Range(0, availableSpawnPoints.Count);
        Transform selected = availableSpawnPoints[randomIndex];
        availableSpawnPoints.RemoveAt(randomIndex);

        return selected;
    }

    IEnumerator DestroyAfterTime(GameObject target, float time)
    {
        yield return new WaitForSeconds(time);

        if (target != null)
        {
            Debug.Log($"Target {target.name} lifetime expired");
            OnTargetDestroyed(target);
            Destroy(target);
        }
    }

    void OnTargetHit(GameObject target)
    {
        // Target was hit, stop the lifetime timer
        StopCoroutine(DestroyAfterTime(target, targetLifetime));
        OnTargetDestroyed(target);
    }

    void OnTargetDestroyed(GameObject target)
    {
        activeTargetCount = Mathf.Max(0, activeTargetCount - 1);
        Debug.Log($"Target removed (Active: {activeTargetCount})");
    }

    public int GetActiveTargetCount() => activeTargetCount;
    public bool IsSpawning() => isSpawning;

    void OnDrawGizmos()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.3f);
                Gizmos.DrawLine(point.position, point.position + point.forward * 0.5f);

                if (enableMovement)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(point.position, new Vector3(moveDistance, 0.5f, moveDistance));
                    Gizmos.color = Color.cyan;
                }
            }
        }
    }
}