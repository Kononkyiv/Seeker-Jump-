using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class CreateDefaultDifficultySettings
{
    static List<EnemyTypeDifficultyEntry> PerEnemy(params (EnemyType type, bool on, float weight, float speed)[] entries)
    {
        var list = new List<EnemyTypeDifficultyEntry>();
        foreach (var e in entries)
            list.Add(new EnemyTypeDifficultyEntry { enemyType = e.type, enabled = e.on, spawnWeightMultiplier = e.weight, speedMultiplier = e.speed });
        return list;
    }

    [MenuItem("Game/Create Default Game Difficulty Settings")]
    static void Create()
    {
        var asset = ScriptableObject.CreateInstance<GameDifficultySettings>();

        var level0 = new DifficultyLevelData
        {
            extraPlatformsPerRowMax = 3,
            maxHorizontalStep = 2.5f,
            safePathNormal = 0.6f, safePathMoving = 0.25f, safePathVanish = 0.15f,
            extraNormal = 0.55f, extraMoving = 0.25f, extraVanish = 0.15f, extraBreakable = 0.05f,
            movingPlatformSpeedMultiplier = 0.9f,
            springChance = 0.08f, hatChance = 0.05f, backpackChance = 0.05f, shieldChance = 0.04f,
            enemiesEnabled = true, enemyBaseSpawnChance = 0.3f, enemyMaxSpawnChance = 0.5f,
            enemyDifficultyGrowthRate = 0.002f, maxEnemiesOnScreen = 2,
            perEnemySettings = PerEnemy(
                (EnemyType.Robo, true, 1f, 0.9f),
                (EnemyType.Whale, true, 0.8f, 0.9f)),
            blackHolesEnabled = false, blackHoleSpawnChance = 0f, maxBlackHolesOnScreen = 0, blackHolePullForceMultiplier = 1f
        };
        var level1 = new DifficultyLevelData
        {
            extraPlatformsPerRowMax = 2,
            maxHorizontalStep = 2.2f,
            safePathNormal = 0.55f, safePathMoving = 0.25f, safePathVanish = 0.2f,
            extraNormal = 0.5f, extraMoving = 0.23f, extraVanish = 0.19f, extraBreakable = 0.08f,
            movingPlatformSpeedMultiplier = 1f,
            springChance = 0.05f, hatChance = 0.03f, backpackChance = 0.03f, shieldChance = 0.025f,
            enemiesEnabled = true, enemyBaseSpawnChance = 0.45f, enemyMaxSpawnChance = 0.65f,
            enemyDifficultyGrowthRate = 0.003f, maxEnemiesOnScreen = 3,
            perEnemySettings = PerEnemy(
                (EnemyType.Robo, true, 1f, 1f),
                (EnemyType.Whale, true, 1f, 1f),
                (EnemyType.Ghost, true, 0.6f, 1f)),
            blackHolesEnabled = true, blackHoleSpawnChance = 0.05f, maxBlackHolesOnScreen = 1, blackHolePullForceMultiplier = 1f
        };
        var level2 = new DifficultyLevelData
        {
            extraPlatformsPerRowMax = 2,
            maxHorizontalStep = 2f,
            safePathNormal = 0.5f, safePathMoving = 0.28f, safePathVanish = 0.22f,
            extraNormal = 0.45f, extraMoving = 0.25f, extraVanish = 0.22f, extraBreakable = 0.08f,
            movingPlatformSpeedMultiplier = 1.2f,
            springChance = 0.03f, hatChance = 0.02f, backpackChance = 0.02f, shieldChance = 0.015f,
            enemiesEnabled = true, enemyBaseSpawnChance = 0.5f, enemyMaxSpawnChance = 0.75f,
            enemyDifficultyGrowthRate = 0.004f, maxEnemiesOnScreen = 4,
            perEnemySettings = PerEnemy(
                (EnemyType.Robo, true, 1f, 1.2f),
                (EnemyType.Whale, true, 1f, 1.2f),
                (EnemyType.Ghost, true, 0.8f, 1.2f),
                (EnemyType.Candle, true, 0.7f, 1.1f)),
            blackHolesEnabled = true, blackHoleSpawnChance = 0.1f, maxBlackHolesOnScreen = 2, blackHolePullForceMultiplier = 1.2f
        };
        var level3 = new DifficultyLevelData
        {
            extraPlatformsPerRowMax = 1,
            maxHorizontalStep = 1.8f,
            safePathNormal = 0.45f, safePathMoving = 0.3f, safePathVanish = 0.25f,
            extraNormal = 0.4f, extraMoving = 0.28f, extraVanish = 0.24f, extraBreakable = 0.08f,
            movingPlatformSpeedMultiplier = 1.4f,
            springChance = 0.02f, hatChance = 0.01f, backpackChance = 0.01f, shieldChance = 0.01f,
            enemiesEnabled = true, enemyBaseSpawnChance = 0.55f, enemyMaxSpawnChance = 0.85f,
            enemyDifficultyGrowthRate = 0.005f, maxEnemiesOnScreen = 5,
            perEnemySettings = PerEnemy(
                (EnemyType.Robo, true, 1f, 1.4f),
                (EnemyType.Whale, true, 1f, 1.4f),
                (EnemyType.Ghost, true, 1f, 1.4f),
                (EnemyType.Candle, true, 0.9f, 1.3f)),
            blackHolesEnabled = true, blackHoleSpawnChance = 0.12f, maxBlackHolesOnScreen = 3, blackHolePullForceMultiplier = 1.4f
        };

        asset.tiers = new List<DifficultyTier>
        {
            new DifficultyTier { scoreThreshold = 0, level = level0 },
            new DifficultyTier { scoreThreshold = 100, level = level1 },
            new DifficultyTier { scoreThreshold = 300, level = level2 },
            new DifficultyTier { scoreThreshold = 500, level = level3 }
        };

        string path = "Assets/GameDifficultySettings.asset";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"Created {path}. Assign to GameDifficultyController. Edit Per Enemy Settings per tier (which enemies, weight and speed).");
    }
}
