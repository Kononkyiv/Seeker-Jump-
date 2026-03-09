using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    protected int currentHealth;
    protected SpriteRenderer sr;
    protected Rigidbody2D rb;
    protected Collider2D col;

    public EnemyConfig config;

    /// <summary>Если false, при столкновении с игроком только отталкивает вниз, не убивает.</summary>
    public virtual bool KillsPlayer => true;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0;
    }

    protected virtual void OnEnable()
    {
        if (config != null)
        {
            currentHealth = config.maxHealth;

            // Поставить базовый спрайт из конфига
            if (sr != null && config.idleSprites != null && config.idleSprites.Length > 0)
                sr.sprite = config.idleSprites[0];
        }
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        if (col != null) col.enabled = false;
        if (rb != null) rb.gravityScale = 3f;
    }
}