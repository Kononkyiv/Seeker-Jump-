using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BackpackItem : MonoBehaviour
{
    [Header("Boost Settings")]
    public float duration = 3.5f;
    public float upSpeed = 16f;

    private bool used;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnEnable()
    {
        used = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        if (player.IsFlying()) return; // в полёте нельзя подбирать ещё один ранец

        used = true;
        player.StartBackPackBoost(duration, upSpeed);
        gameObject.SetActive(false);
    }
}
