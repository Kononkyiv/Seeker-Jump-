using UnityEngine;

[CreateAssetMenu(menuName = "Game/BlackHole Config")]
public class BlackHoleConfig : ScriptableObject
{
    [Header("Spawn")]
    [Range(0f, 1f)]
    public float spawnChance = 0.08f;
    public int maxBlackHolesOnScreen = 2;
    public float minDistanceFromPlatform = 1.5f;

    [Header("Behavior")]
    [Tooltip("Сила притяжения к центру (чем больше — тем сильнее тянет).")]
    [Min(0.1f)]
    public float pullForce = 15f;
    public float pullRadius = 5f;
    [Tooltip("Радиус, внутри которого игрок мгновенно погибает. Только здесь — смерть, не вдали от центра.")]
    [Min(0.5f)]
    public float killRadius = 1.5f;
    [Tooltip("Степень притяжения по расстоянию: 1 = линейно, 2 = сильнее у центра (быстрее затягивает).")]
    [Min(0.5f)]
    public float pullCurveExponent = 1.8f;
    public float rotationSpeed = 45f;
}
