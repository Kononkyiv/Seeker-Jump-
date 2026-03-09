using UnityEngine;
using System.Collections;

public class RoboEnemy : BaseEnemy
{
    // idleSprites: 0=left, 1=left_dead, 2=red_left, 3=right, 4=right_dead, 5=red_right
    private Vector3 spawnPosition;
    private bool isDead;
    private bool hitReaction;
    private bool facingRight;

    void Start()
    {
        spawnPosition = transform.position;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isDead = false;
        hitReaction = false;
        if (Application.isPlaying)
            spawnPosition = transform.position;
    }

    void Update()
    {
        if (isDead || config == null)
            return;

        float speed = (config.moveSpeed > 0f ? config.moveSpeed : 1f) * GameDifficultyController.GetEnemySpeedMultiplier(config.type);
        float x;
        if (config.moveFullWidth)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            float range = config.moveRangeX > 0f ? config.moveRangeX : 4f;
            x = Mathf.Lerp(-range, range, t);
        }
        else
        {
            float offset = Mathf.Sin(Time.time * speed) * 2f;
            x = spawnPosition.x + offset;
        }

        transform.position = new Vector3(x, spawnPosition.y, spawnPosition.z);
        facingRight = Mathf.Cos(Time.time * speed) >= 0f;
        UpdateSprite();
    }

    void UpdateSprite()
    {
        if (config.idleSprites == null || config.idleSprites.Length < 6)
            return;

        int index = facingRight ? 3 : 0;
        if (hitReaction)
            index = facingRight ? 5 : 2;

        sr.sprite = config.idleSprites[index];
    }

    public override void TakeDamage(int amount)
    {
        hitReaction = true;
        base.TakeDamage(amount);
        StartCoroutine(EndHitReactionAfterDelay());
    }

    IEnumerator EndHitReactionAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        hitReaction = false;
    }

    protected override void Die()
    {
        if (isDead)
            return;
        isDead = true;

        base.Die();

        if (config != null && config.idleSprites != null && config.idleSprites.Length >= 5)
            sr.sprite = config.idleSprites[facingRight ? 4 : 1];

        if (rb != null)
            rb.freezeRotation = true;
    }
}
