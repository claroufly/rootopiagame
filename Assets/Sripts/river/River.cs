using UnityEngine;

/// <summary>
/// Put this on a GameObject with a Collider set to "Is Trigger".
/// When the player enters, it asks the UIManager to show this river's info.
/// The player needs the "Player" tag and a Rigidbody (or CharacterController) so triggers fire.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RiverHealthZone : MonoBehaviour
{
    [Header("River Stats")]
    public string riverName = "River";
    [SerializeField] private float maxHealth = 100f;
    [Range(0f, 100f)] public float health = 100f;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("Status Thresholds")]
    [SerializeField] private float healthyAbove = 70f;
    [SerializeField] private float pollutedBelow = 40f;

    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? health / maxHealth : 0f;

    public string Status
    {
        get
        {
            if (health >= healthyAbove) return "Healthy";
            if (health >= pollutedBelow) return "Polluted";
            return "Critical";
        }
    }

    public Color StatusColor
    {
        get
        {
            if (health >= healthyAbove) return new Color(0.3f, 0.8f, 0.4f);   // green
            if (health >= pollutedBelow) return new Color(0.95f, 0.75f, 0.2f); // yellow
            return new Color(0.85f, 0.25f, 0.25f);                             // red
        }
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowRiverInfo(this);
        else
            Debug.LogWarning("No UIManager found in the scene.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (UIManager.Instance != null)
            UIManager.Instance.HideRiverInfo(this);
    }

    // Call these from other systems (cleanup actions, pollution sources, etc.)
    public void Damage(float amount)
    {
        health = Mathf.Clamp(health - amount, 0f, maxHealth);
    }

    public void Restore(float amount)
    {
        health = Mathf.Clamp(health + amount, 0f, maxHealth);
    }
}