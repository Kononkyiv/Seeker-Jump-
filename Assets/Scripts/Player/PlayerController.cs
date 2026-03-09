using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Config")]
    public GameConfig config;

    [Header("Movement")]
    public float moveSpeed = 8f;
    [Header("Mobile Tilt (Doodle Jump style)")]
    [Tooltip("Множитель наклона. Увеличь, если реакция слабая.")]
    [SerializeField] float mobileTiltSensitivity = 4f;
    [Tooltip("Наклон слабее этого не двигает (мёртвая зона).")]
    [SerializeField] float mobileTiltDeadZone = 0.04f;
    [Tooltip("0=X, 1=Y, 2=Z, 3=-X, 4=-Y, 5=-Z. Перебирай 0–5 на устройстве, пока не заработает наклон.")]
    [SerializeField] int mobileTiltAxis = 1;

    [Header("Animation Settings")]
    public float animationSpeed = 12f;

    [Header("Normal Sprites")]
    public Sprite[] normalRight;
    public Sprite[] normalLeft;

    [Header("Hat Sprites")]
    public Sprite[] hatRight;
    public Sprite[] hatLeft;

    [Header("Backpack Sprites")]
    public Sprite[] backpackRight;
    public Sprite[] backpackLeft;

    [Header("Shooting")]
    [SerializeField] BulletPool bulletPool;
    [SerializeField] Transform firePoint;
    [Tooltip("Пауза между выстрелами (меньше = больше пуль при частых кликах).")]
    [SerializeField] float fireCooldown = 0.08f;
    [SerializeField] Sprite leftAttackSprite;
    [SerializeField] Sprite rightAttackSprite;

    [Header("Death")]
    public Sprite playerDeadSprite;

    [Header("Post-Boost")]
    [Tooltip("Время после приземления (после буста), когда враги не спавнятся. Можно поставить 0.5–1.5 сек.")]
    [SerializeField] float noEnemySpawnCooldownAfterBoost = 1.2f;

    [Header("Shield")]
    [Tooltip("Визуальный эффект щита (например, зелёный круг вокруг игрока). Включается только когда щит активен.")]
    [SerializeField] GameObject shieldVisual;
    [Tooltip("Скорость вращения щита в градусах в секунду (0 = не крутится).")]
    [SerializeField] float shieldVisualRotationSpeed = 120f;
    [Tooltip("За сколько секунд до конца щит начинает мигать (0 = не мигать).")]
    [SerializeField] float shieldBlinkThreshold = 3f;
    [Tooltip("Период мигания в секундах (полный цикл видно/не видно).")]
    [SerializeField] float shieldBlinkInterval = 0.25f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private SpriteRenderer sr;

    private float animationTimer;
    private int frameIndex;
    private int facing = 1;

    private bool hatActive;
    private float hatTimeLeft;
    private float hatUpSpeed;
    private float hatGravityScaleBackup;

    private bool backPackActive;
    private float backPackTimeLeft;
    private float backPackUpSpeed;

    private int lastAnimationState; // 0=Idle, 1=Jump, 2=HatBoost, 3=BackPack
    private bool isGrounded;
    private float landingIdleTimer; // после приземления показываем idle короткое время
    private float fireCooldownLeft;
    private float attackSpriteTimer;
    private bool isDead;
    private float noEnemySpawnTimer;
    private bool shieldActive;
    private float shieldTimeLeft;
    private float shieldBlinkTimer;
    private SpriteRenderer shieldVisualRenderer;
    private float deadTimer;
    private float smoothTiltAccel;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        shieldActive = false;
        shieldTimeLeft = 0f;
        shieldBlinkTimer = 0f;
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
            shieldVisualRenderer = shieldVisual.GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    void Start()
    {
        rb.gravityScale = config.gravityScale;
        hatGravityScaleBackup = config.gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (Application.isMobilePlatform && Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
    }

    float GetFallDeathY()
    {
        if (mainCamera == null) return Mathf.NegativeInfinity;
        return mainCamera.transform.position.y - mainCamera.orthographicSize - 2f;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        float bottom = GetFallDeathY();
        if (rb.position.y < bottom)
            Die(ignoreShield: true);
    }

    void Update()
    {
        if (isDead)
        {
            deadTimer += Time.deltaTime;
            float bottom = GetFallDeathY();
            bool belowCamera = transform.position.y < bottom;
            if (belowCamera || deadTimer >= 2f)
            {
                GameUI gameUI = FindFirstObjectByType<GameUI>();
                if (gameUI != null)
                    gameUI.ShowPauseMenu(showResumeButton: false);
            }
            return;
        }

        if (transform.position.y < GetFallDeathY())
        {
            Die(ignoreShield: true);
            return;
        }

        if (landingIdleTimer > 0f) landingIdleTimer -= Time.deltaTime;
        if (fireCooldownLeft > 0f) fireCooldownLeft -= Time.deltaTime;
        if (attackSpriteTimer > 0f) attackSpriteTimer -= Time.deltaTime;
        if (shieldActive)
        {
            shieldTimeLeft -= Time.deltaTime;
            if (shieldTimeLeft <= 0f)
                SetShieldActive(false);
            if (shieldVisual != null && shieldVisual.activeSelf)
            {
                if (shieldVisualRotationSpeed != 0f)
                    shieldVisual.transform.Rotate(0f, 0f, -shieldVisualRotationSpeed * Time.deltaTime);
                // Мигание в последние N секунд
                if (shieldBlinkThreshold > 0f && shieldTimeLeft <= shieldBlinkThreshold && shieldTimeLeft > 0f)
                {
                    shieldBlinkTimer += Time.deltaTime;
                    if (shieldBlinkTimer >= shieldBlinkInterval)
                    {
                        shieldBlinkTimer = 0f;
                        if (shieldVisualRenderer != null)
                            shieldVisualRenderer.enabled = !shieldVisualRenderer.enabled;
                    }
                }
                else
                {
                    shieldBlinkTimer = 0f;
                    if (shieldVisualRenderer != null)
                        shieldVisualRenderer.enabled = true;
                }
            }
        }
        if (IsFlying())
            noEnemySpawnTimer = Mathf.Max(noEnemySpawnTimer, noEnemySpawnCooldownAfterBoost);
        else if (noEnemySpawnTimer > 0f)
            noEnemySpawnTimer -= Time.deltaTime;
        HandleMovement();
        HandleShooting();
        HandleScreenWrap();
        HandleBackPackBoost();
        HandleHatBoost();
        HandleAnimation();
    }

    /// <summary>Игрок в полёте (шапка, ракета или сильный подъём после пружины) — неуязвим для врагов.</summary>
    /// <summary>Порог velocity.y выше обычного прыжка (jumpForce ~15), чтобы не считать каждый прыжок полётом.</summary>
    public bool IsFlying()
    {
        if (rb == null) return false;
        const float boostVelocityThreshold = 20f; // выше обычного прыжка (~15), только пружина/сильный буст
        return hatActive || backPackActive || rb.linearVelocity.y > boostVelocityThreshold;
    }

    /// <summary>После буста ещё идёт период, когда враги не спавнятся вообще.</summary>
    public bool IsNoEnemySpawnPeriod()
    {
        return noEnemySpawnTimer > 0f;
    }

    public bool IsShieldActive()
    {
        return shieldActive;
    }

    /// <summary>Вызвать при старте любого буста (шапка/ранец/пружина), чтобы на заданное время отключить спавн врагов.</summary>
    public void SetNoEnemySpawnPeriod(float duration)
    {
        if (duration > noEnemySpawnTimer)
            noEnemySpawnTimer = duration;
    }

    /// <param name="ignoreShield">Если true, смерть наступает даже при активном щите (например, от падения). Щит спасает только от врагов и опасностей.</param>
    public void Die(bool ignoreShield = false)
    {
        if (!ignoreShield && shieldActive) return;
        if (isDead) return;

        isDead = true;
        deadTimer = 0f;

        if (playerDeadSprite != null)
            sr.sprite = playerDeadSprite;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3f;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void ActivateShield(float duration)
    {
        if (duration <= 0f) return;
        shieldActive = true;
        shieldTimeLeft = duration;
        SetShieldActive(true);
    }

    void SetShieldActive(bool active)
    {
        shieldActive = active;
        if (!active)
        {
            shieldTimeLeft = 0f;
            shieldBlinkTimer = 0f;
        }
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(active);
            if (shieldVisualRenderer != null)
                shieldVisualRenderer.enabled = true;
        }
    }

    void HandleShooting()
    {
        bool fireInput;
        if (Application.isMobilePlatform)
            fireInput = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        else
            fireInput = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (!fireInput || fireCooldownLeft > 0f || bulletPool == null || firePoint == null || isDead || IsFlying())
            return;

        fireCooldownLeft = fireCooldown;
        attackSpriteTimer = 0.1f;

        Sprite attackSprite = facing >= 0 ? rightAttackSprite : leftAttackSprite;
        if (attackSprite != null)
            sr.sprite = attackSprite;

        Bullet bullet = bulletPool.GetBullet();
        bullet.transform.position = firePoint.position;
        bullet.Init(Vector2.up);
    }

    void HandleMovement()
    {
        if (isDead) return;

        float moveInput = 0f;

        if (Application.isMobilePlatform)
        {
            Vector3 accel = Accelerometer.current != null && Accelerometer.current.enabled
                ? Accelerometer.current.acceleration.ReadValue()
                : new Vector3(Input.acceleration.x, Input.acceleration.y, Input.acceleration.z);
            float ax = accel.x, ay = accel.y, az = accel.z;
            float raw = mobileTiltAxis switch { 0 => ax, 1 => ay, 2 => az, 3 => -ax, 4 => -ay, 5 => -az, _ => ay };
            smoothTiltAccel = Mathf.Lerp(smoothTiltAccel, raw, Time.deltaTime * 12f);
            if (Mathf.Abs(smoothTiltAccel) >= mobileTiltDeadZone)
                moveInput = Mathf.Clamp(smoothTiltAccel * mobileTiltSensitivity, -1f, 1f);
        }
        else
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveInput = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveInput = 1f;
            }
        }

        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void HandleAnimation()
    {
        if (attackSpriteTimer > 0f)
        {
            Sprite attackSprite = facing >= 0 ? rightAttackSprite : leftAttackSprite;
            if (attackSprite != null)
                sr.sprite = attackSprite;
            return;
        }

        bool showIdle = isGrounded || landingIdleTimer > 0f;
        int state;
        if (backPackActive)
            state = 3; // BackPack
        else if (hatActive)
            state = 2; // Hat boost
        else if (showIdle)
            state = 0; // Idle
        else
            state = 1; // Jump

        // Reset frame when switching state so we don't reuse wrong sprites
        if (state != lastAnimationState)
        {
            lastAnimationState = state;
            frameIndex = 0;
            animationTimer = 0f;
        }

        Sprite[] currentSet = GetCurrentSpriteSet();
        if (currentSet == null || currentSet.Length == 0)
            return;

        if (state == 0)
        {
            // Idle: frame 0 only
            sr.sprite = currentSet[0];
            return;
        }

        if (state == 1)
        {
            // Jump: отдельный кадр (индекс 1 = поза в прыжке; 0 = на платформе)
            int jumpFrame = currentSet.Length > 1 ? 1 : 0;
            sr.sprite = currentSet[jumpFrame];
            return;
        }

        // HatBoost or BackPack: анимация кадров
        animationTimer += Time.deltaTime;
        if (animationTimer >= 1f / animationSpeed)
        {
            animationTimer = 0f;
            frameIndex++;
            if (frameIndex >= currentSet.Length)
                frameIndex = 0;
        }
        sr.sprite = currentSet[frameIndex];
    }

    Sprite[] GetCurrentSpriteSet()
    {
        if (backPackActive)
            return (facing >= 0) ? backpackRight : backpackLeft;
        if (hatActive)
            return (facing >= 0) ? hatRight : hatLeft;
        return (facing >= 0) ? normalRight : normalLeft;
    }

    void HandleBackPackBoost()
    {
        if (!backPackActive) return;

        backPackTimeLeft -= Time.deltaTime;

        if (backPackTimeLeft <= 0f)
        {
            backPackActive = false;
            rb.gravityScale = config.gravityScale;
            return;
        }

        rb.gravityScale = 0.05f;

        if (rb.linearVelocity.y < backPackUpSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, backPackUpSpeed);
    }

    void HandleHatBoost()
    {
        if (!hatActive) return;

        hatTimeLeft -= Time.deltaTime;

        if (hatTimeLeft <= 0f)
        {
            StopHatBoost();
            return;
        }

        if (rb.linearVelocity.y < hatUpSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, hatUpSpeed);
    }

    void HandleScreenWrap()
    {
        float screenHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float playerHalfWidth = GetComponent<Collider2D>().bounds.extents.x;
        Vector3 pos = transform.position;

        // Перенос только когда персонаж ПОЛНОСТЬЮ вышел за край — тогда ставим его целиком с другой стороны
        if (pos.x - playerHalfWidth > screenHalfWidth)
            pos.x = -screenHalfWidth + playerHalfWidth;
        else if (pos.x + playerHalfWidth < -screenHalfWidth)
            pos.x = screenHalfWidth - playerHalfWidth;

        transform.position = pos;
    }

    public void StartHatBoost(float duration, float upSpeed)
    {
        if (!hatActive)
        {
            hatGravityScaleBackup = rb.gravityScale;
            rb.gravityScale = Mathf.Max(0.2f, hatGravityScaleBackup * 0.25f);
        }

        hatActive = true;
        hatTimeLeft = duration;
        hatUpSpeed = upSpeed;
        frameIndex = 0;
        animationTimer = 0f;
        SetNoEnemySpawnPeriod(duration + noEnemySpawnCooldownAfterBoost);
    }

    void StopHatBoost()
    {
        hatActive = false;
        rb.gravityScale = hatGravityScaleBackup;
        frameIndex = 0;
        animationTimer = 0f;
    }

    public void StartBackPackBoost(float duration, float upSpeed)
    {
        backPackActive = true;
        backPackTimeLeft = duration;
        backPackUpSpeed = upSpeed;

        hatActive = false; // Backpack overrides hat

        frameIndex = 0;
        animationTimer = 0f;
        SetNoEnemySpawnPeriod(duration + noEnemySpawnCooldownAfterBoost);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return; // мёртвый не реагирует на платформы — только падает вниз

        BaseEnemy enemy = other.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            if (shieldActive)
            {
                int damage = enemy.config != null ? enemy.config.maxHealth : 999;
                enemy.TakeDamage(damage);
                return;
            }
            if (IsFlying()) return; // неуязвим во время полёта

            // Свечной враг: снимает очки, умирает, но не убивает игрока
            CandleEnemy candle = enemy as CandleEnemy;
            if (candle != null)
            {
                candle.OnHitPlayer();
                return;
            }

            if (!enemy.KillsPlayer)
            {
                // Враг только отталкивает вниз (например, кит)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -12f);
                return;
            }
            Die();
            return;
        }

        if (hatActive) return;

        if (rb.linearVelocity.y > 0f) return;

        Platform platform = other.GetComponent<Platform>();
        if (platform == null) return;

        isGrounded = true;
        landingIdleTimer = 0.12f; // чтобы при приземлении сразу показывался idle-спрайт
        platform.OnPlayerBounce(rb, config.jumpForce);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Platform>() != null)
            isGrounded = false;
    }

}