using UnityEngine;

public abstract class PlatformItem : MonoBehaviour
{
    // Можно ли этому предмету срабатывать на данной платформе
    public virtual bool CanSpawnOn(Platform platform) => platform.type != PlatformType.Breakable;

    // Вызывается в момент отскока от платформы (идеальная точка)
    public abstract void OnBounce(Rigidbody2D playerRb, Platform platform, ref float jumpForce);

    // Если предмет одноразовый — пусть сам решает, что делать после срабатывания
    public virtual void Consume()
    {
        gameObject.SetActive(false);
    }
}