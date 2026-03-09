using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player")]
    public float jumpForce = 15f;
    public float gravityScale = 2.5f;

    [Header("World")]
    public float verticalSpacing = 3.5f;
    public float spawnOffset = 10f;
    public float minX = -2.5f;
    public float maxX = 2.5f;

    [Header("Platform Generation")]
    public float verticalGapMin = 1.8f;
    public float verticalGapMax = 3.2f;
    public int initialPlatformCount = 12;
    public float spawnBufferAboveCamera = 6f;

    [Header("Horizontal Layout")]
    public float horizontalSpawnRange = 4f;
    public float branchChance = 0.4f;
    public float branchOffset = 2f;

    [Header("Enemy Spawn")]
    public float enemySpawnHeightMin = 6f;
    public float enemySpawnHeightMax = 20f;
    public int maxEnemiesPerSegment = 2;
}