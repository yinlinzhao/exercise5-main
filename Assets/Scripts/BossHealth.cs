using System.Collections;
using System;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = -1f;

    [Header("Hit Tint (Simple)")]
    [SerializeField] private SpriteRenderer hitTintRenderer;
    [SerializeField] private float hitTintDurationSeconds = 0.08f;
    [Range(0f, 1f)]
    [SerializeField] private float hitWhiteTintStrength = 0.35f;
    [SerializeField] private Color hitTintColor = new Color(1f, 0.25f, 0.25f, 1f);
    private Coroutine hitTintRoutine;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    public static Action<float, float> HealthChanged;
    public static Action BossDied;

    private void Awake()
    {
        if (hitTintRenderer == null)
            hitTintRenderer = GetComponentInChildren<SpriteRenderer>();

        InitializeHealth();
        NotifyHealthChanged();
    }

    // Same pattern as ArcherBehavior.TryTakeDamage(), but subtracts health instead of destroying immediately.
    // PlayerAttack already passes (attackerPosition, attackRange) so damage stays optional.
    public bool TryTakeDamage(Vector3 attackerPosition, float attackRange = 1.0f, float damage = 1.0f)
    {
        if (IsDead) return false;

        float distance = Vector3.Distance(transform.position, attackerPosition);
        if (distance > attackRange) return false;

        TakeDamage(damage);
        return true;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float applied = Mathf.Max(0f, damage);
        if (applied <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth - applied, 0f, maxHealth);
        NotifyHealthChanged();

        // Visual feedback on hit (simple tint).
        // If you kept the optional BossHitFlash component around, we still support it.
        BossHitFlash hitFlash = GetComponent<BossHitFlash>();
        if (hitFlash == null) hitFlash = GetComponentInChildren<BossHitFlash>();
        if (hitFlash != null)
        {
            hitFlash.Flash();
        }
        else
        {
            TriggerHitTint();
        }

        if (IsDead)
        {
            Die();
        }
    }

    private void TriggerHitTint()
    {
        if (hitTintRenderer == null) return;

        if (hitTintRoutine != null)
            StopCoroutine(hitTintRoutine);

        hitTintRoutine = StartCoroutine(HitTintRoutine());
    }

    private IEnumerator HitTintRoutine()
    {
        if (hitTintRenderer == null)
        {
            hitTintRoutine = null;
            yield break;
        }

        Color baseColor = hitTintRenderer.color;
        float duration = Mathf.Max(0.01f, hitTintDurationSeconds);
        float strength = Mathf.Clamp01(hitWhiteTintStrength);

        hitTintRenderer.color = Color.Lerp(baseColor, hitTintColor, strength);
        yield return new WaitForSeconds(duration);

        if (hitTintRenderer != null)
            hitTintRenderer.color = baseColor;

        hitTintRoutine = null;
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        float applied = Mathf.Max(0f, amount);
        if (applied <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + applied, 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void InitializeHealth()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        if (currentHealth < 0f)
        {
            currentHealth = maxHealth;
        }

        // Keep current health in a sane range.
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        BossDied?.Invoke();

        // Prefer a DeathEffect on the boss root (best: it disables + destroys this boss object).
        DeathEffect deathEffect = GetComponent<DeathEffect>();
        if (deathEffect != null)
        {
            deathEffect.PlayDeathEffect();
            return;
        }

        // Common setup: the DeathEffect is placed on a child sprite object.
        // In that case, use it to spawn the SAME puff of smoke, then cleanly remove the boss root.
        DeathEffect childDeathEffect = GetComponentInChildren<DeathEffect>();
        if (childDeathEffect != null)
        {
            childDeathEffect.PlaySpawnEffect();
            DisableBossComponentsForDeath();
            Destroy(gameObject, 0.1f);
            return;
        }

        // Fallback: no VFX available.
        Destroy(gameObject);
    }

    private void DisableBossComponentsForDeath()
    {
        // Disable all visible renderers (covers SpriteRenderer + any other Renderer types).
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = false;
        }

        // Disable collisions so the boss stops interacting immediately.
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D c in colliders)
        {
            if (c != null) c.enabled = false;
        }

        // Stop animations.
        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (Animator a in animators)
        {
            if (a != null) a.enabled = false;
        }

        // Stop physics simulation (if used).
        Rigidbody2D[] bodies = GetComponentsInChildren<Rigidbody2D>();
        foreach (Rigidbody2D rb in bodies)
        {
            if (rb != null) rb.simulated = false;
        }

        // Disable other scripts (attacks/movement/AI), but keep BossHealth so Destroy delay still happens.
        MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script == null) continue;
            if (script == this) continue;
            if (script is DeathEffect) continue; // allow child VFX scripts to function if needed
            script.enabled = false;
        }
    }
}

