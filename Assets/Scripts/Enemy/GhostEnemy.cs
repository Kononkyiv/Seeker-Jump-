using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Призрак: ходит влево-вправо по экрану. Лидер оставляет за собой цепочку копий (гуськом),
/// пока не займёт полэкрана. Копии не размножаются.
/// idleSprites: 0=left, 1=left_move, 2=right, 3=right_move (минимум 4).
/// </summary>
public class GhostEnemy : BaseEnemy
{
    [Header("Ghost Trail")]
    [Tooltip("Префаб призрака (тот же, что спавнится) — для создания копий цепочки.")]
    public GameObject ghostPrefab;
    [Tooltip("Зазор между призраками в цепочке (по ширине).")]
    public float trailGap = 0.6f;

    private float direction = 1f;
    private float distanceTraveled;
    private bool isLeader = true;
    private GhostEnemy leader;
    private readonly List<GhostEnemy> followers = new List<GhostEnemy>();
    private float startY;
    private Camera mainCam;
    private bool isDead;

    void Start()
    {
        startY = transform.position.y;
        mainCam = Camera.main;
        if (isLeader && ghostPrefab == null && config != null && config.enemyPrefab != null)
            ghostPrefab = config.enemyPrefab;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isDead = false;
        if (Application.isPlaying)
            startY = transform.position.y;
    }

    void Update()
    {
        if (isDead || config == null)
            return;

        if (mainCam == null)
            mainCam = Camera.main;
        if (isLeader && ghostPrefab == null && config != null && config.enemyPrefab != null)
            ghostPrefab = config.enemyPrefab;
        float halfW = mainCam != null ? mainCam.orthographicSize * mainCam.aspect : 5f;

        if (!isLeader && leader != null)
            direction = leader.direction;
        else if (!isLeader && leader == null)
            { /* keep last direction */ }

        float speed = (config.moveSpeed > 0f ? config.moveSpeed : 1.2f) * GameDifficultyController.GetEnemySpeedMultiplier(config.type);
        float dx = direction * speed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x + dx, startY, transform.position.z);

        if (isLeader)
        {
            float x = transform.position.x;
            if (x >= halfW - 0.2f) direction = -1f;
            else if (x <= -halfW + 0.2f) direction = 1f;

            distanceTraveled += Mathf.Abs(dx);
            float width = GetWidth();
            float step = Mathf.Min(width + trailGap, 3f);
            if (step > 0.1f && followers.Count < GetMaxFollowers(halfW, width))
            {
                while (distanceTraveled >= step)
                {
                    distanceTraveled -= step;
                    SpawnFollower();
                    if (followers.Count >= GetMaxFollowers(halfW, width))
                        break;
                }
            }
        }

        UpdateSprite();
    }

    float GetWidth()
    {
        if (sr != null && sr.sprite != null)
        {
            float w = sr.sprite.bounds.size.x * transform.lossyScale.x;
            return Mathf.Clamp(w, 0.4f, 15f);
        }
        return 1f;
    }

    int GetMaxFollowers(float halfScreenWidth, float width)
    {
        float step = width + trailGap;
        if (step <= 0f) return 0;
        int totalSlots = Mathf.FloorToInt(halfScreenWidth / step);
        totalSlots = Mathf.Max(2, totalSlots);
        return totalSlots - 1;
    }

    void SpawnFollower()
    {
        GameObject prefab = ghostPrefab != null ? ghostPrefab : (config != null ? config.enemyPrefab : null);
        if (prefab == null) return;

        float width = GetWidth();
        float step = width + trailGap;
        Vector3 behind = transform.position - new Vector3(direction * step, 0f, 0f);
        GameObject go = Instantiate(prefab, behind, Quaternion.identity);
        go.transform.SetParent(null);
        GhostEnemy g = go.GetComponent<GhostEnemy>();
        if (g == null) return;

        g.isLeader = false;
        g.leader = this;
        g.config = config;
        g.direction = direction;
        g.startY = startY;
        g.mainCam = mainCam;
        if (g.sr != null && config.idleSprites != null && config.idleSprites.Length > 0)
            g.sr.sprite = config.idleSprites[direction > 0 ? 2 : 0];
        followers.Add(g);
    }

    void UpdateSprite()
    {
        if (config?.idleSprites == null || config.idleSprites.Length < 4)
            return;
        int index = direction > 0 ? 3 : 1;
        sr.sprite = config.idleSprites[index];
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        base.Die();
    }

    void OnDisable()
    {
        if (isLeader && followers.Count > 0)
        {
            for (int i = followers.Count - 1; i >= 0; i--)
                if (followers[i] != null)
                    followers[i].leader = null;
            followers.Clear();
        }
    }
}
