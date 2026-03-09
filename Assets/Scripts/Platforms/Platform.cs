using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Platform : MonoBehaviour
{
    public PlatformType type;

    [Header("Sprites")]
    public Sprite normalSprite;
    public Sprite movingSprite;
    public Sprite vanishSprite;
    public Sprite breakableSprite;
    public Sprite brokenSprite;

    [Header("Moving Settings")]
    public float moveSpeed = 2f;
    public float moveRange = 2f;

    [Header("Break Settings")]
    public float fallGravity = 3f;

    private SpriteRenderer sr;
    private BoxCollider2D col;
    private Rigidbody2D rb;

    private Vector3 startPosition;
    private bool isFalling;

    [Header("Item References (assign in prefab)")]
    [SerializeField] private GameObject springObj;
    [SerializeField] private GameObject hatObj;
    [SerializeField] private GameObject backpackObj;
    [SerializeField] private GameObject shieldObj;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();

        if (springObj == null)
        {
            var si = GetComponentInChildren<SpringItem>(true);
            if (si != null) springObj = si.gameObject;
        }
        if (hatObj == null)
        {
            var hi = GetComponentInChildren<HatItem>(true);
            if (hi != null) hatObj = hi.gameObject;
        }
        if (backpackObj == null)
        {
            var bi = GetComponentInChildren<BackpackItem>(true);
            if (bi != null) backpackObj = bi.gameObject;
        }
        if (shieldObj == null)
        {
            var sh = GetComponentInChildren<ShieldItem>(true);
            if (sh != null) shieldObj = sh.gameObject;
        }
    }

    void OnEnable()
    {
        ResetState();
        ApplySprite();
        // startPosition for Moving is set in SetType after spawner sets position

        if (springObj != null) springObj.SetActive(false);
        if (hatObj != null) hatObj.SetActive(false);
        if (backpackObj != null) backpackObj.SetActive(false);
        if (shieldObj != null) shieldObj.SetActive(false);
    }

    public void SetType(PlatformType newType)
    {
        type = newType;
        if (type == PlatformType.Moving)
            startPosition = transform.position;
        ApplySprite();
    }

    public void SetupItems(bool spawnSpring, bool spawnHat, bool spawnBackpack, bool spawnShield)
    {
        if (springObj != null) springObj.SetActive(false);
        if (hatObj != null) hatObj.SetActive(false);
        if (backpackObj != null) backpackObj.SetActive(false);
        if (shieldObj != null) shieldObj.SetActive(false);

        if (type == PlatformType.Breakable)
            return;

        if (backpackObj != null && spawnBackpack)
        {
            backpackObj.SetActive(true);
            EnsureItemSortOrder(backpackObj.transform);
            return;
        }
        if (hatObj != null && spawnHat)
        {
            hatObj.SetActive(true);
            EnsureItemSortOrder(hatObj.transform);
            return;
        }
        if (shieldObj != null && spawnShield)
        {
            shieldObj.SetActive(true);
            EnsureItemSortOrder(shieldObj.transform);
            return;
        }
        if (springObj != null && spawnSpring)
        {
            springObj.SetActive(true);
            EnsureItemSortOrder(springObj.transform);
        }
    }

    void EnsureItemSortOrder(Transform item)
    {
        var itemSR = item.GetComponent<SpriteRenderer>();
        if (itemSR != null)
            itemSR.sortingOrder = sr.sortingOrder + 1;
    }

    /// <summary>Width for spawner span: BoxCollider2D size * scale. Use collider size (1,1) on prefab; scale controls actual size.</summary>
    public float GetPlatformWidth()
    {
        if (col == null)
            col = GetComponent<BoxCollider2D>();
        if (col == null)
            return 1f;
        return col.size.x * transform.localScale.x;
    }

    void ResetState()
    {
        isFalling = false;
        col.isTrigger = true;

        if (springObj != null) springObj.SetActive(false);
        if (hatObj != null) hatObj.SetActive(false);
        if (backpackObj != null) backpackObj.SetActive(false);

        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }
    }

    void ApplySprite()
    {
        switch (type)
        {
            case PlatformType.Normal: sr.sprite = normalSprite; break;
            case PlatformType.Moving: sr.sprite = movingSprite; break;
            case PlatformType.Vanish: sr.sprite = vanishSprite; break;
            case PlatformType.Breakable: sr.sprite = breakableSprite; break;
        }
    }

    void Update()
    {
        if (type == PlatformType.Moving && !isFalling)
        {
            float speed = moveSpeed * GameDifficultyController.MovingPlatformSpeedMultiplier;
            float offset = Mathf.Sin(Time.time * speed) * moveRange;
            transform.position = new Vector3(
                startPosition.x + offset,
                transform.position.y,
                transform.position.z
            );
        }
    }

    public void OnPlayerBounce(Rigidbody2D playerRb, float jumpForce)
    {
        if (isFalling) return;

        switch (type)
        {
            case PlatformType.Normal:
            case PlatformType.Moving:
                // Never override a stronger upward velocity (e.g. from spring or hat)
                float newY = Mathf.Max(playerRb.linearVelocity.y, jumpForce);
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, newY);
                break;

            case PlatformType.Vanish:
                newY = Mathf.Max(playerRb.linearVelocity.y, jumpForce);
                playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, newY);
                gameObject.SetActive(false);
                break;

            case PlatformType.Breakable:
                BreakPlatform();
                break;
        }
    }

    void BreakPlatform()
    {
        if (isFalling) return;

        isFalling = true;
        sr.sprite = brokenSprite;
        col.isTrigger = false;

        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = fallGravity;
        rb.freezeRotation = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (transform.localScale.x < 0.2f || transform.localScale.x > 3f)
            Debug.LogWarning($"{name} platform scale is outside safe range (0.2 - 3).");
    }
#endif
}