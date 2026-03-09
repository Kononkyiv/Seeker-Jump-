using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject platformPrefab;
    public Transform player;
    public Camera mainCamera;
    public GameConfig config;

    [Header("Path Settings")]
    public float maxHorizontalStep = 2f;

    [Header("Platform Density")]
    [Tooltip("Max extra platforms per row (beyond safe path). 2 = 0/1/2 extras (original).")]
    public int extraPlatformsPerRowMax = 2;

    [Header("No-overlap Settings")]
    public float extraGap = 0.5f;
    public int maxTriesPerPlatform = 25;

    [Header("Item Chances")]
    [Range(0f, 1f)] public float springChance = 0.025f;
    [Range(0f, 1f)] public float hatChance = 0.015f;
    [Range(0f, 1f)] public float backpackChance = 0.015f;
    [Range(0f, 1f)] public float shieldChance = 0.02f;

    [Header("Hazards")]
    public GameObject blackHolePrefab;
    public BlackHoleConfig blackHoleConfig;

    [Header("Difficulty Overrides (set by GameDifficultyController)")]
    [HideInInspector] public bool blackHolesEnabled = true;
    [HideInInspector] public float blackHoleSpawnChanceOverride = -1f;
    [HideInInspector] public int maxBlackHolesOnScreenOverride = -1;
    [HideInInspector] public float safePathNormal = 0.55f, safePathMoving = 0.25f, safePathVanish = 0.2f;
    [HideInInspector] public float extraNormal = 0.5f, extraMoving = 0.23f, extraVanish = 0.19f, extraBreakable = 0.08f;

    private float highestY;
    private float lastSafeX;
    private PlayerController playerController;

    private readonly List<GameObject> activePlatforms = new();
    private readonly List<GameObject> platformPool = new();
    private readonly List<GameObject> activeBlackHoles = new();
    private readonly List<GameObject> blackHolePool = new();

    private struct XSpan
    {
        public float min, max;
        public XSpan(float min, float max) { this.min = min; this.max = max; }
    }

    void Start()
    {
        highestY = player.position.y;
        lastSafeX = 0f;
        if (player != null)
            playerController = player.GetComponent<PlayerController>();

        SpawnPlatformAt(player.position.y - 1.5f, 0f, PlatformType.Normal);

        int count = config != null ? config.initialPlatformCount : 12;
        for (int i = 0; i < count; i++)
            SpawnRow();
    }

    void Update()
    {
        float buffer = config != null ? config.spawnBufferAboveCamera : 6f;
        if (player.position.y + buffer > highestY)
            SpawnRow();

        CleanupPlatforms();
    }

    void SpawnRow()
    {
        float gapMin = config != null ? config.verticalGapMin : 1.8f;
        float gapMax = config != null ? config.verticalGapMax : 3.2f;
        highestY += Random.Range(gapMin, gapMax);

        var spans = new List<XSpan>();

        bool movingAlreadyOnRow = false;
        bool springAlreadyOnRow = false;
        bool hatAlreadyOnRow = false;
        bool backpackAlreadyOnRow = false;
        bool shieldAlreadyOnRow = false;

        float hRange = config != null ? config.horizontalSpawnRange : 4f;
        float cameraHalfW = mainCamera != null ? mainCamera.orthographicSize * mainCamera.aspect : hRange;
        hRange = Mathf.Max(1.5f, Mathf.Min(hRange, cameraHalfW - 0.2f));
        float branchChance = config != null ? config.branchChance : 0.4f;
        float branchOffset = config != null ? config.branchOffset : 2f;

        // ---- SAFE PATH ----
        float safeX = lastSafeX + Random.Range(-maxHorizontalStep, maxHorizontalStep);
        if (Random.value < branchChance)
            safeX += Random.Range(-branchOffset, branchOffset);
        safeX = Mathf.Clamp(safeX, -hRange, hRange);

        PlatformType safeType = GetSafeType();
        if (safeType == PlatformType.Moving) movingAlreadyOnRow = true;

        if (!TrySpawnNonOverlapping(highestY, safeX, safeType, spans, true, hRange, ref springAlreadyOnRow, ref hatAlreadyOnRow, ref backpackAlreadyOnRow, ref shieldAlreadyOnRow))
            TrySpawnNonOverlapping(highestY, safeX, PlatformType.Normal, spans, true, hRange, ref springAlreadyOnRow, ref hatAlreadyOnRow, ref backpackAlreadyOnRow, ref shieldAlreadyOnRow);

        lastSafeX = safeX;

        // ---- EXTRAS ----
        int extraCount = Random.Range(0, extraPlatformsPerRowMax + 1);
        for (int i = 0; i < extraCount; i++)
        {
            PlatformType t = GetRandomPlatformType();

            if (t == PlatformType.Moving)
            {
                if (movingAlreadyOnRow) t = PlatformType.Normal;
                else movingAlreadyOnRow = true;
            }

            TrySpawnNonOverlapping(highestY, 0f, t, spans, false, hRange, ref springAlreadyOnRow, ref hatAlreadyOnRow, ref backpackAlreadyOnRow, ref shieldAlreadyOnRow);
        }

        XSpan safePathSpan = spans.Count > 0 ? spans[0] : new XSpan(safeX - SafePathMargin, safeX + SafePathMargin);
        TrySpawnBlackHole(highestY, safeX, hRange, safePathSpan);
    }

    bool TrySpawnNonOverlapping(
        float y, float desiredX, PlatformType type,
        List<XSpan> spans, bool forceX, float horizontalRange,
        ref bool springAlreadyOnRow, ref bool hatAlreadyOnRow, ref bool backpackAlreadyOnRow, ref bool shieldAlreadyOnRow)
    {
        for (int attempt = 0; attempt < maxTriesPerPlatform; attempt++)
        {
            float x = forceX ? desiredX : Random.Range(-horizontalRange, horizontalRange);

            GameObject platform = GetPlatformFromPool();
            platform.transform.position = new Vector2(x, y);

            Platform script = platform.GetComponent<Platform>();
            script.SetType(type);

            XSpan span = GetSpanForPlatform(platform, script);

            if (IntersectsAny(span, spans))
            {
                ReturnToPool(platform);
                continue;
            }

            // ---- Items (один предмет на платформу) ----
            bool spawnSpring = false;
            bool spawnHat = false;
            bool spawnBackpack = false;
            bool spawnShield = false;

            if (type != PlatformType.Breakable)
            {
                if (!backpackAlreadyOnRow && backpackChance > 0f && Random.value < backpackChance)
                {
                    spawnBackpack = true;
                    backpackAlreadyOnRow = true;
                }
                else if (!hatAlreadyOnRow && hatChance > 0f && Random.value < hatChance)
                {
                    spawnHat = true;
                    hatAlreadyOnRow = true;
                }
                else if (!shieldAlreadyOnRow && shieldChance > 0f && Random.value < shieldChance)
                {
                    spawnShield = true;
                    shieldAlreadyOnRow = true;
                }
                else if (!springAlreadyOnRow && Random.value < springChance)
                {
                    spawnSpring = true;
                    springAlreadyOnRow = true;
                }
            }

            script.SetupItems(spawnSpring, spawnHat, spawnBackpack, spawnShield);

            spans.Add(span);
            activePlatforms.Add(platform);
            return true;
        }

        return false;
    }

    XSpan GetSpanForPlatform(GameObject platform, Platform script)
    {
        float width = script.GetPlatformWidth() * 0.9f; // safety gap to prevent overlap when scaling
        float half = width * 0.5f;
        float x = platform.transform.position.x;
        float min = x - half - extraGap;
        float max = x + half + extraGap;

        if (script.type == PlatformType.Moving)
        {
            float range = script.moveRange;
            min -= range;
            max += range;
        }

        return new XSpan(min, max);
    }

    bool IntersectsAny(XSpan span, List<XSpan> spans)
    {
        for (int i = 0; i < spans.Count; i++)
            if (span.min < spans[i].max && span.max > spans[i].min)
                return true;
        return false;
    }

    PlatformType GetSafeType()
    {
        float r = Random.value;
        if (r < safePathNormal) return PlatformType.Normal;
        if (r < safePathNormal + safePathMoving) return PlatformType.Moving;
        return PlatformType.Vanish;
    }

    PlatformType GetRandomPlatformType()
    {
        float rand = Random.value;
        if (rand < extraNormal) return PlatformType.Normal;
        if (rand < extraNormal + extraMoving) return PlatformType.Moving;
        if (rand < extraNormal + extraMoving + extraVanish) return PlatformType.Vanish;
        return PlatformType.Breakable;
    }

    void SpawnPlatformAt(float y, float x, PlatformType type)
    {
        GameObject platform = GetPlatformFromPool();
        platform.transform.position = new Vector2(x, y);

        Platform script = platform.GetComponent<Platform>();
        script.SetType(type);
        script.SetupItems(false, false, false, false);

        activePlatforms.Add(platform);
    }

    GameObject GetPlatformFromPool()
    {
        if (platformPool.Count > 0)
        {
            GameObject obj = platformPool[0];
            platformPool.RemoveAt(0);
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(platformPrefab);
    }

    void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
        platformPool.Add(obj);
    }

    void CleanupPlatforms()
    {
        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize - 2f;

        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            GameObject platform = activePlatforms[i];
            if (platform.transform.position.y < cameraBottom)
            {
                ReturnToPool(platform);
                activePlatforms.RemoveAt(i);
            }
        }

        for (int i = activeBlackHoles.Count - 1; i >= 0; i--)
        {
            GameObject bh = activeBlackHoles[i];
            if (bh != null && bh.transform.position.y < cameraBottom)
            {
                ReturnBlackHoleToPool(bh);
                activeBlackHoles.RemoveAt(i);
            }
        }
    }

    const float SafePathMargin = 2.5f;
    const float BlackHoleSpawnHeightAbove = 2f;

    void TrySpawnBlackHole(float rowY, float safePathX, float horizontalRange, XSpan safePathSpan)
    {
        if (!blackHolesEnabled || blackHolePrefab == null || blackHoleConfig == null)
            return;
        if (playerController != null && (playerController.IsFlying() || playerController.IsNoEnemySpawnPeriod()))
            return;
        int maxAllowed = maxBlackHolesOnScreenOverride >= 0 ? maxBlackHolesOnScreenOverride : blackHoleConfig.maxBlackHolesOnScreen;
        if (activeBlackHoles.Count >= maxAllowed)
            return;
        float chance = blackHoleSpawnChanceOverride >= 0f ? blackHoleSpawnChanceOverride : blackHoleConfig.spawnChance;
        if (Random.value > chance)
            return;

        float cameraTop = mainCamera != null ? mainCamera.transform.position.y + mainCamera.orthographicSize : rowY + 10f;
        if (rowY < cameraTop - 0.5f)
            return;

        float minDistFromPlatform = blackHoleConfig.minDistanceFromPlatform;
        float safeMin = safePathSpan.min - SafePathMargin;
        float safeMax = safePathSpan.max + SafePathMargin;
        int attempts = 8;
        for (int a = 0; a < attempts; a++)
        {
            float x = Random.Range(-horizontalRange, horizontalRange);
            if (x >= safeMin && x <= safeMax)
                continue;

            Vector2 pos = new Vector2(x, rowY + BlackHoleSpawnHeightAbove);
            bool tooCloseToPlatform = false;
            for (int i = 0; i < activePlatforms.Count; i++)
            {
                if (activePlatforms[i] == null) continue;
                float d = Vector2.Distance(pos, activePlatforms[i].transform.position);
                if (d < minDistFromPlatform)
                {
                    tooCloseToPlatform = true;
                    break;
                }
            }
            if (tooCloseToPlatform)
                continue;

            GameObject bh = GetBlackHoleFromPool();
            bh.transform.position = pos;
            var bhScript = bh.GetComponent<BlackHole>();
            if (bhScript != null)
                bhScript.config = blackHoleConfig;
            activeBlackHoles.Add(bh);
            return;
        }
    }

    GameObject GetBlackHoleFromPool()
    {
        if (blackHolePool.Count > 0)
        {
            GameObject obj = blackHolePool[0];
            blackHolePool.RemoveAt(0);
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(blackHolePrefab);
    }

    void ReturnBlackHoleToPool(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        blackHolePool.Add(obj);
    }
}