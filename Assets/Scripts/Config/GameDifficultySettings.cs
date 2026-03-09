using UnityEngine;
using System.Collections.Generic;

/// <summary>Настройка одного типа врага на уровне сложности.</summary>
[System.Serializable]
public class EnemyTypeDifficultyEntry
{
    [Tooltip("Тип врага (Candle, Ghost, Robo, Whale).")]
    public EnemyType enemyType;
    [Tooltip("Может ли этот враг появляться на этом уровне очков.")]
    public bool enabled = true;
    [Tooltip("Во сколько раз умножить вес спавна из EnemyConfig (0 = не появляется).")]
    [Range(0f, 3f)] public float spawnWeightMultiplier = 1f;
    [Tooltip("Множитель скорости движения этого врага.")]
    [Range(0.3f, 3f)] public float speedMultiplier = 1f;
}

/// <summary>Данные одного уровня сложности (платформы, предметы, враги, чёрные дыры).</summary>
[System.Serializable]
public class DifficultyLevelData
{
    [Header("Platforms")]
    [Tooltip("Макс. доп. платформ в ряду (0–4).")]
    [Range(0, 6)]
    public int extraPlatformsPerRowMax = 2;
    [Tooltip("Макс. шаг по горизонтали для safe path.")]
    [Range(0.5f, 5f)]
    public float maxHorizontalStep = 2f;

    [Header("Platform Types (safe path) — сумма = 1")]
    [Range(0f, 1f)] public float safePathNormal = 0.55f;
    [Range(0f, 1f)] public float safePathMoving = 0.25f;
    [Range(0f, 1f)] public float safePathVanish = 0.2f;

    [Header("Platform Types (extra platforms) — сумма = 1")]
    [Range(0f, 1f)] public float extraNormal = 0.5f;
    [Range(0f, 1f)] public float extraMoving = 0.23f;
    [Range(0f, 1f)] public float extraVanish = 0.19f;
    [Range(0f, 1f)] public float extraBreakable = 0.08f;

    [Tooltip("Множитель скорости двигающихся платформ на этом уровне очков (1 = без изменений).")]
    [Range(0.3f, 3f)] public float movingPlatformSpeedMultiplier = 1f;

    [Header("Item Chances (0–1)")]
    [Range(0f, 1f)] public float springChance = 0.025f;
    [Range(0f, 1f)] public float hatChance = 0.015f;
    [Range(0f, 1f)] public float backpackChance = 0.015f;
    [Range(0f, 1f)] public float shieldChance = 0.02f;

    [Header("Enemies (общее)")]
    [Tooltip("Включить ли спавн врагов на этом уровне (если выкл — не спавнятся вообще).")]
    public bool enemiesEnabled = true;
    [Range(0f, 1f)] public float enemyBaseSpawnChance = 0.45f;
    [Range(0f, 1f)] public float enemyMaxSpawnChance = 0.75f;
    [Range(0f, 0.01f)] public float enemyDifficultyGrowthRate = 0.003f;
    [Range(0, 6)] public int maxEnemiesOnScreen = 3;

    [Header("Enemies по типам (какие враги и с каким весом/скоростью на этом уровне очков)")]
    [Tooltip("Список настроек по каждому типу. Тип не в списке = выключен на этом уровне.")]
    public List<EnemyTypeDifficultyEntry> perEnemySettings = new List<EnemyTypeDifficultyEntry>();

    [Header("Black Holes")]
    public bool blackHolesEnabled = true;
    [Range(0f, 1f)] public float blackHoleSpawnChance = 0.08f;
    [Tooltip("Макс. число чёрных дыр одновременно на экране на этом уровне очков.")]
    [Range(0, 6)] public int maxBlackHolesOnScreen = 2;
    [Range(0.3f, 40f)] public float blackHolePullForceMultiplier = 1f;
}

/// <summary>Порог по очкам и данные сложности после его достижения.</summary>
[System.Serializable]
public class DifficultyTier
{
    [Tooltip("При score >= этому значению используется этот уровень.")]
    public int scoreThreshold;
    public DifficultyLevelData level;
}

[CreateAssetMenu(fileName = "GameDifficultySettings", menuName = "Game/Game Difficulty Settings")]
public class GameDifficultySettings : ScriptableObject
{
    [Tooltip("Упорядочить по возрастанию scoreThreshold. Первый tier — с 0 очков.")]
    public List<DifficultyTier> tiers = new List<DifficultyTier>();

    /// <summary>Индекс уровня для данного счёта (0 = первый tier и т.д.).</summary>
    public int GetLevelIndexForScore(int score)
    {
        if (tiers == null || tiers.Count == 0) return 0;
        int index = 0;
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].scoreThreshold <= score)
                index = i;
        }
        return index;
    }

    public DifficultyLevelData GetLevelForScore(int score)
    {
        int i = GetLevelIndexForScore(score);
        if (tiers == null || i >= tiers.Count || tiers[i].level == null)
            return null;
        return tiers[i].level;
    }
}
