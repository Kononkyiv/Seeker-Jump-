using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HatItem : MonoBehaviour
{
    [Header("Boost Settings")]
    public float duration = 2f;
    public float upSpeed = 10f;

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
        if (player.IsFlying()) return; // в полёте нельзя подбирать ещё одну шапку

        used = true;
        player.StartHatBoost(duration, upSpeed);
        gameObject.SetActive(false);
    }
}