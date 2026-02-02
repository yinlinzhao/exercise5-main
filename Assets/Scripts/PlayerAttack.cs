using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float attackHitDelay = 0.3f; // Delay before damage is dealt (when sword visually hits)
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

    // Public property for state observation
    public bool IsAttacking { get; private set; } = false;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        // Prefer the PlayerHealth on the same GameObject; fallback to a scene lookup.
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
    }

    void Update()
    {
        // Handle attack input - can trigger even while moving
        if (Input.GetKeyDown(attackKey) && !IsAttacking)
        {
            TriggerAttack();
        }
    }

    private void TriggerAttack()
    {
        IsAttacking = true;
        
        // Delay the attack hit to match the sword swing animation
        // This makes it look like the archer is destroyed when the sword visually hits
        StartCoroutine(DelayedAttackHit());
        
        // Reset attack state after attack duration
        StartCoroutine(ResetAttackState());
    }

    private IEnumerator DelayedAttackHit()
    {
        // Wait for the sword swing to reach the hit point
        yield return new WaitForSeconds(attackHitDelay);
        
        // Check for enemies in attack range and notify them
        CheckForEnemies(transform.position);
    }

    private void CheckForEnemies(Vector3 position)
    {
        // Find all ArcherBehavior scripts in the scene and check if player is within attack range
        ArcherBehavior[] archers = FindObjectsOfType<ArcherBehavior>();
        foreach (ArcherBehavior archer in archers)
        {
            if (archer != null)
            {
                bool killed = archer.TryTakeDamage(position, attackRange);
                if (killed && playerHealth != null)
                {
                    playerHealth.AddLives(1);
                }
            }
        }

        // Also damage the boss (if present) using the same "TryTakeDamage" pattern.
        BossHealth[] bosses = FindObjectsOfType<BossHealth>();
        foreach (BossHealth boss in bosses)
        {
            if (boss != null)
            {
                boss.TryTakeDamage(position, attackRange);
            }
        }
    }

    private IEnumerator ResetAttackState()
    {
        // Wait for attack animation to complete
        yield return new WaitForSeconds(attackDuration);
        
        IsAttacking = false;
    }

    // Public method to reset attack state (called by MoveScript on game reset)
    public void ResetAttack()
    {
        IsAttacking = false;
        StopAllCoroutines();
    }
}
