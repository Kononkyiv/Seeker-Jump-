using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Identification")]
    public EnemyType type;

    [Header("Stats")]
    public int maxHealth = 1;
    public int damage = 1;
    public float moveSpeed = 0f;
    public int scoreReward = 100;
    [Tooltip("Сколько очков снимается при попадании по игроку (используется, например, свечой).")]
    public int scorePenaltyOnHit = 50;

    [Header("Movement")]
    [Tooltip("Если включено — враг ходит по всему горизонтальному диапазону (moveRangeX). Если выключено — только в зоне ±2 от точки спавна.")]
    public bool moveFullWidth = false;
    [Tooltip("Полуширина движения по X при moveFullWidth (например 4 = от -4 до +4).")]
    public float moveRangeX = 4f;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float spawnWeight = 0.25f;

    [Header("Visuals")]
    public Sprite[] idleSprites;
    public Sprite deadSprite;

    [Header("Prefab")]
    public GameObject enemyPrefab;
}
