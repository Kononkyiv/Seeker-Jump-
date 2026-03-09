using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShieldItem : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Длительность щита в секундах.")]
    public float duration = 10f;

    private bool used;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
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

        used = true;
        player.ActivateShield(duration);
        gameObject.SetActive(false);
    }
}

