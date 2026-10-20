using UnityEngine;

/// <summary>
/// Attach to a chicken. Give the chicken's collider (trigger) the "Livestock" tag
/// so PlayerInteraction can detect it the same way it detects "Item" objects.
/// </summary>
public class Chicken : MonoBehaviour
{
    [Header("Identity")]
    public string chickenName = "Chicken";

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Stats")]
    [Range(0f, 100f)] public float hunger = 100f;
    [Range(0f, 100f)] public float happiness = 100f;

    [Header("Stat Decay (points per second)")]
    [SerializeField] private float hungerDecayRate = 0.5f;
    [SerializeField] private float happinessDecayRate = 0.2f;
    [SerializeField] private float hungryHappinessPenalty = 0.5f; // extra happiness loss while hunger is low
    [SerializeField] private float hungryThreshold = 25f;         // below this counts as "hungry"
    [SerializeField] private float starvationDamageRate = 1f;     // health lost per second at 0 hunger

    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 5f;   // how far from spawn it can roam
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float minPauseTime = 1f;   // seconds spent idle between moves
    [SerializeField] private float maxPauseTime = 3f;
    [SerializeField] private float destinationThreshold = 0.1f; // how close counts as "arrived"

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isWalkingParam = "IsWalking"; // must match the bool parameter name in your Animator Controller

    private Vector3 homePosition;   // center of the wander area (its starting spot)
    private Vector3 targetPosition; // where it's currently walking to
    private float pauseTimer;
    private bool isPaused = true;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(); // model's Animator is often on a child object, not the root
    }

    private void Start()
    {
        homePosition = transform.position;
        PickNewWanderTarget();
    }

    private void Update()
    {
        DecayStats(); // runs every frame, even while the chicken is paused

        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                PickNewWanderTarget();
                isPaused = false;
                SetWalking(true);
            }
            return;
        }

        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f; // keep movement on the horizontal plane; adjust if your game isn't flat ground

        if (toTarget.magnitude <= destinationThreshold)
        {
            isPaused = true;
            pauseTimer = Random.Range(minPauseTime, maxPauseTime);
            SetWalking(false);
            return;
        }

        Vector3 direction = toTarget.normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Face the direction it's walking, since chickens (and most sprites/models) look odd sliding sideways.
        if (direction != Vector3.zero)
            transform.forward = direction;
    }

    private void DecayStats()
    {
        float dt = Time.deltaTime;

        hunger = Mathf.Clamp(hunger - hungerDecayRate * dt, 0f, 100f);

        float happinessLoss = happinessDecayRate;
        if (hunger <= hungryThreshold)
            happinessLoss += hungryHappinessPenalty;
        happiness = Mathf.Clamp(happiness - happinessLoss * dt, 0f, 100f);

        // Starving chickens lose health
        if (hunger <= 0f)
            TakeDamage(starvationDamageRate * dt);
    }

    private void SetWalking(bool walking)
    {
        if (animator != null)
            animator.SetBool(isWalkingParam, walking);
    }

    private void PickNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = homePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
    }

    public void Feed(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
    }

    public void Pet(float amount)
    {
        happiness = Mathf.Clamp(happiness + amount, 0f, 100f);
    }

    /// <summary>Called by PlayerInteraction when the player interacts with this chicken.</summary>
    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.DisplayLivestockInfo(this);
        }
        else
        {
            Debug.LogWarning("No UIManager found in the scene.");
        }
    }

    // Visualize the wander radius in the Scene view for easy tuning.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = Application.isPlaying ? homePosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);
    }
}