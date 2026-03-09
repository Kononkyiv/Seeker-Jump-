using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-100)]
/// <summary>Применяет уровень сложности к спавнерам по текущему счёту. Добавь на сцену и укажи GameDifficultySettings.</summary>
public class GameDifficultyController : MonoBehaviour
{
    [Header("References")]
    public GameUI gameUI;
    public PlatformSpawner platformSpawner;
    public EnemySpawner enemySpawner;

    [Header("Settings")]
    public GameDifficultySettings difficultySettings;

    /// <summary>Множитель силы притяжения чёрной дыры (читают BlackHole).</summary>
    public static float BlackHolePullForceMultiplier = 1f;
    /// <summary>Множитель скорости двигающихся платформ (читает Platform).</summary>
    public static float MovingPlatformSpeedMultiplier = 1f;

    static Dictionary<EnemyType, bool> _enemyEnabled = new Dictionary<EnemyType, bool>();
    static Dictionary<EnemyType, float> _enemyWeightMult = new Dictionary<EnemyType, float>();
    static Dictionary<EnemyType, float> _enemySpeedMult = new Dictionary<EnemyType, float>();

    /// <summary>Если список perEnemySettings пуст — все типы включены. Иначе только перечисленные с enabled.</summary>
    public static bool IsEnemyEnabled(EnemyType type)
    {
        if (_enemyEnabled.Count == 0) return true;
        return _enemyEnabled.TryGetValue(type, out bool v) && v;
    }
    /// <summary>Если списка нет — вес 1. Иначе 0 для неперечисленных.</summary>
    public static float GetEnemyWeightMultiplier(EnemyType type)
    {
        if (_enemyWeightMult.Count == 0) return 1f;
        return _enemyWeightMult.TryGetValue(type, out float v) ? v : 0f;
    }
    public static float GetEnemySpeedMultiplier(EnemyType type)
    {
        if (_enemySpeedMult.Count == 0) return 1f;
        return _enemySpeedMult.TryGetValue(type, out float v) ? v : 1f;
    }

    private int _lastAppliedLevelIndex = -1;

    void Awake()
    {
        if (difficultySettings != null)
        {
            DifficultyLevelData level0 = difficultySettings.GetLevelForScore(0);
            if (level0 != null)
            {
                ApplyLevel(level0);
                _lastAppliedLevelIndex = difficultySettings.GetLevelIndexForScore(0);
            }
        }
    }

    void Start()
    {
        ApplyLevelForCurrentScore();
    }

    void Update()
    {
        ApplyLevelForCurrentScore();
    }

    void ApplyLevelForCurrentScore()
    {
        if (difficultySettings == null || gameUI == null || platformSpawner == null)
            return;

        int score = gameUI.GetCurrentScore();
        int index = difficultySettings.GetLevelIndexForScore(score);
        if (index == _lastAppliedLevelIndex)
            return;

        _lastAppliedLevelIndex = index;
        DifficultyLevelData level = difficultySettings.GetLevelForScore(score);
        if (level == null) return;

        ApplyLevel(level);
    }

    public void ApplyLevel(DifficultyLevelData level)
    {
        if (level == null) return;

        if (platformSpawner != null)
        {
            platformSpawner.extraPlatformsPerRowMax = level.extraPlatformsPerRowMax;
            platformSpawner.maxHorizontalStep = level.maxHorizontalStep;
            platformSpawner.springChance = level.springChance;
            platformSpawner.hatChance = level.hatChance;
            platformSpawner.backpackChance = level.backpackChance;
            platformSpawner.shieldChance = level.shieldChance;
            platformSpawner.safePathNormal = level.safePathNormal;
            platformSpawner.safePathMoving = level.safePathMoving;
            platformSpawner.safePathVanish = level.safePathVanish;
            platformSpawner.extraNormal = level.extraNormal;
            platformSpawner.extraMoving = level.extraMoving;
            platformSpawner.extraVanish = level.extraVanish;
            platformSpawner.extraBreakable = level.extraBreakable;
            platformSpawner.blackHolesEnabled = level.blackHolesEnabled;
            platformSpawner.blackHoleSpawnChanceOverride = level.blackHoleSpawnChance;
            platformSpawner.maxBlackHolesOnScreenOverride = level.maxBlackHolesOnScreen;
        }

        BlackHolePullForceMultiplier = level.blackHolePullForceMultiplier;
        MovingPlatformSpeedMultiplier = level.movingPlatformSpeedMultiplier;

        _enemyEnabled.Clear();
        _enemyWeightMult.Clear();
        _enemySpeedMult.Clear();
        if (level.perEnemySettings != null)
        {
            foreach (var e in level.perEnemySettings)
            {
                _enemyEnabled[e.enemyType] = e.enabled && e.spawnWeightMultiplier > 0f;
                _enemyWeightMult[e.enemyType] = e.spawnWeightMultiplier;
                _enemySpeedMult[e.enemyType] = e.speedMultiplier;
            }
        }

        if (enemySpawner != null)
        {
            enemySpawner.baseSpawnChance = level.enemyBaseSpawnChance;
            enemySpawner.maxSpawnChance = level.enemyMaxSpawnChance;
            enemySpawner.difficultyGrowthRate = level.enemyDifficultyGrowthRate;
            enemySpawner.maxEnemiesOnScreen = level.maxEnemiesOnScreen;
            enemySpawner.enemiesEnabled = level.enemiesEnabled;
        }
    }
}
