using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public GameObject bulletPrefab;
    public int poolSize = 20;

    private Queue<Bullet> pool = new Queue<Bullet>();

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(bulletPrefab);
            obj.SetActive(false);

            Bullet bullet = obj.GetComponent<Bullet>();
            bullet.SetPool(this);

            pool.Enqueue(bullet);
        }
    }

    public Bullet GetBullet()
    {
        if (pool.Count > 0)
        {
            Bullet bullet = pool.Dequeue();
            bullet.gameObject.SetActive(true);
            return bullet;
        }

        GameObject obj = Instantiate(bulletPrefab);
        Bullet newBullet = obj.GetComponent<Bullet>();
        newBullet.SetPool(this);

        return newBullet;
    }

    public void ReturnBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
        pool.Enqueue(bullet);
    }
}
