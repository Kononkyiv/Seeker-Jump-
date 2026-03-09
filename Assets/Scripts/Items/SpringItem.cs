using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class SpringItem : MonoBehaviour
{
    public float superJumpForce = 35f;

    public Sprite idleSprite;
    public Sprite activeSprite;

    private SpriteRenderer sr;
    private bool used;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var c = GetComponent<Collider2D>();
        if (c != null)
        {
            c.isTrigger = true;
            c.enabled = true;
        }
    }

    void OnEnable()
    {
        used = false;

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = true;
            var box = col as BoxCollider2D;
            if (box != null && (box.size.x < 2f || box.size.y < 2f))
                box.size = new Vector2(5f, 5f);
        }

        if (sr && idleSprite)
            sr.sprite = idleSprite;
    }

    public bool IsActive()
    {
        return !used && gameObject.activeSelf;
    }

    public float Consume()
    {
        used = true;

        if (activeSprite)
            sr.sprite = activeSprite;

        return superJumpForce;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        // Только игрок может использовать пружину (пули и прочее не должны её тратить)
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // Срабатывает только при падении
        if (rb.linearVelocity.y > 0f) return;

        float force = Consume();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        player.SetNoEnemySpawnPeriod(2f);

        StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        gameObject.SetActive(false);
    }
}