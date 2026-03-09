using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera mainCamera;

    [Header("Enemy Data")]
    public List<EnemyConfig> enemyConfigs;

    [Header("Spawn Settings")]
    public float spawnOffset = 5f;
    public float verticalSpacing = 14f;

    [Header("Difficulty")]
    public float baseSpawnChance = 0.45f;
    public float difficultyGrowthRate = 0.003f;
    public float maxSpawnChance = 0.75f;

    [Header("Limits")]
    public int maxEnemiesOnScreen = 3;
    [Tooltip("Выключено при сложности, где враги отключены.")]
    public bool enemiesEnabled = true;

    private float highestSpawnY;
    private readonly List<GameObject> activeEnemies = new();

    void Start()
    {
        highestSpawnY = player.position.y;
    }

    void Update()
    {
        TrySpawnEnemy();
        CleanupEnemies();
    }

    void TrySpawnEnemy()
    {
        if (!enemiesEnabled)
            return;
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null && (pc.IsFlying() || pc.IsNoEnemySpawnPeriod()))
            return; // во время полёта и немного после — враги не спавнятся

        if (player.position.y + spawnOffset < highestSpawnY)
            return;

        highestSpawnY += verticalSpacing;

        // Враги всегда сверху: не ниже верха камеры (слегка за экраном)
        float cameraTop = mainCamera.transform.position.y + mainCamera.orthographicSize;
        float minSpawnY = cameraTop + 1f;
        if (highestSpawnY < minSpawnY)
            highestSpawnY = minSpawnY;

        if (activeEnemies.Count >= maxEnemiesOnScreen)
            return;

        float height = player.position.y;
        float spawnChance = baseSpawnChance + height * difficultyGrowthRate;
        spawnChance = Mathf.Min(spawnChance, maxSpawnChance);

        if (Random.value > spawnChance)
            return;

        EnemyConfig config = GetRandomEnemyByWeight();
        if (config == null || config.enemyPrefab == null)
            return;

        float screenHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float randomX = Random.Range(-screenHalfWidth, screenHalfWidth);

        Vector3 spawnPos = new Vector3(randomX, highestSpawnY, 0f);

        GameObject enemy = Instantiate(config.enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);
    }

    EnemyConfig GetRandomEnemyByWeight()
    {
        if (enemyConfigs == null || enemyConfigs.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (var config in enemyConfigs)
        {
            if (!GameDifficultyController.IsEnemyEnabled(config.type))
                continue;
            float w = config.spawnWeight * GameDifficultyController.GetEnemyWeightMultiplier(config.type);
            totalWeight += w;
        }
        if (totalWeight <= 0f)
            return null;

        float randomValue = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (var config in enemyConfigs)
        {
            if (!GameDifficultyController.IsEnemyEnabled(config.type))
                continue;
            cumulative += config.spawnWeight * GameDifficultyController.GetEnemyWeightMultiplier(config.type);
            if (randomValue <= cumulative)
                return config;
        }
        return enemyConfigs[0];
    }

    void CleanupEnemies()
    {
        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize - 2f;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (activeEnemies[i].transform.position.y < cameraBottom)
            {
                Destroy(activeEnemies[i]);
                activeEnemies.RemoveAt(i);
            }
        }
    }
}
