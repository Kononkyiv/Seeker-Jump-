using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BlackHole : MonoBehaviour
{
    public BlackHoleConfig config;

    private SpriteRenderer sr;
    private CircleCollider2D col;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private PlayerController playerController;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnEnable()
    {
        CachePlayer();
    }

    void CachePlayer()
    {
        if (playerTransform == null || playerRb == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                playerController = pc;
                playerTransform = pc.transform;
                playerRb = pc.GetComponent<Rigidbody2D>();
            }
        }
    }

    void Update()
    {
        if (config != null && config.rotationSpeed != 0f && sr != null)
        {
            transform.Rotate(0f, 0f, -config.rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (config == null || playerRb == null || playerController == null)
        {
            CachePlayer();
            return;
        }
        if (playerController == null || playerTransform == null)
            return;
        if (playerController.IsDead() || playerController.IsFlying() || playerController.IsShieldActive())
            return;

        Vector2 center = transform.position;
        Vector2 playerPos = playerTransform.position;
        float dist = Vector2.Distance(center, playerPos);

        if (dist <= config.killRadius)
        {
            playerController.Die();
            return;
        }

        if (dist <= config.pullRadius && dist > 0.01f)
        {
            Vector2 dir = (center - playerPos).normalized;
            float pullForce = config.pullForce * GameDifficultyController.BlackHolePullForceMultiplier;
            float t = 1f - dist / config.pullRadius;
            float curve = config.pullCurveExponent > 0f ? Mathf.Pow(Mathf.Clamp01(t), config.pullCurveExponent) : t;
            float strength = pullForce * curve;
            playerRb.AddForce(dir * strength);
        }
    }
}
