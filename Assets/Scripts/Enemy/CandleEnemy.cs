using UnityEngine;

/// <summary>
/// Свечной враг: двигается по X (экран или вокруг точки) и медленно спускается вниз по Y.
/// При столкновении не убивает игрока, а снимает очки и умирает.
/// idleSprites: 0=left, 1=left_move, 2=right, 3=right_move (минимум 4).
/// </summary>
public class CandleEnemy : BaseEnemy
{
    [Header("Candle Movement")]
    [Tooltip("Скорость спуска вниз по Y.")]
    public float verticalSpeed = 1f;

    private Vector3 spawnPosition;
    private bool isDead;
    private bool facingRight = true;

    void Start()
    {
        spawnPosition = transform.position;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isDead = false;
        if (Application.isPlaying)
            spawnPosition = transform.position;
    }

    void Update()
    {
        if (isDead || config == null)
            return;

        float speedX = (config.moveSpeed > 0f ? config.moveSpeed : 1f) * GameDifficultyController.GetEnemySpeedMultiplier(config.type);
        float x;
        if (config.moveFullWidth)
        {
            // Ходит по всему экрану по X
            Camera cam = Camera.main;
            float range = (config.moveRangeX > 0f) ? config.moveRangeX :
                (cam != null ? cam.orthographicSize * cam.aspect : 4f);
            float t = (Mathf.Sin(Time.time * speedX) + 1f) * 0.5f;
            x = Mathf.Lerp(-range, range, t);
            facingRight = Mathf.Cos(Time.time * speedX) >= 0f;
        }
        else
        {
            // Небольшое горизонтальное покачивание вокруг точки спавна
            float offset = Mathf.Sin(Time.time * speedX) * 1.5f;
            x = spawnPosition.x + offset;
            facingRight = offset >= 0f;
        }

        float y = transform.position.y - Mathf.Abs(verticalSpeed) * GameDifficultyController.GetEnemySpeedMultiplier(config.type) * Time.deltaTime;
        transform.position = new Vector3(x, y, spawnPosition.z);

        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (config?.idleSprites == null || config.idleSprites.Length < 4)
            return;

        int index = facingRight ? 3 : 1;
        sr.sprite = config.idleSprites[index];
    }

    public void OnHitPlayer()
    {
        if (isDead) return;

        int penalty = (config != null && config.scorePenaltyOnHit > 0) ? config.scorePenaltyOnHit : 10;
        GameUI ui = FindFirstObjectByType<GameUI>();
        if (ui != null)
            ui.AddScorePenalty(penalty);

        Die();
        isDead = true;
    }
}

