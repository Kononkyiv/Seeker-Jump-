using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifetime = 3f;

    private Vector2 direction;
    private BulletPool pool;

    public void SetPool(BulletPool p)
    {
        pool = p;
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        Invoke(nameof(ReturnToPool), lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Не лететь дальше камеры — вернуть в пул при выходе за границы
        Camera cam = Camera.main;
        if (cam != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 camPos = cam.transform.position;
            if (transform.position.y > camPos.y + halfH + 0.5f ||
                transform.position.y < camPos.y - halfH - 0.5f ||
                transform.position.x < camPos.x - halfW - 0.5f ||
                transform.position.x > camPos.x + halfW + 0.5f)
            {
                ReturnToPool();
            }
        }
    }

    void ReturnToPool()
    {
        CancelInvoke();
        pool.ReturnBullet(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        BaseEnemy enemy = other.GetComponentInParent<BaseEnemy>();

        if (enemy != null)
        {
            enemy.TakeDamage(1);
            ReturnToPool();
        }
    }
}
