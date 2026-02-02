using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttacks : MonoBehaviour
{
    [Header("Spell Blast")]
    [Tooltip("Reticle/marker prefab that spawns on the target to telegraph SpellBlast.")]
    [SerializeField] private GameObject spellBlastReticlePrefab;
    [Tooltip("If true, the reticle spawns above the target's head using its renderer/collider bounds.")]
    [SerializeField] private bool spellBlastReticleSpawnAboveHead = true;
    [Tooltip("Extra y-offset added on top of the target's bounds when spawning above head.")]
    [SerializeField] private float spellBlastReticleExtraYOffset = 0.15f;
    [Tooltip("Fallback y-offset if no renderer/collider is found on the target.")]
    [SerializeField] private float spellBlastReticleFallbackYOffset = 1.0f;
    [Tooltip("Optional z-offset for the reticle (useful if your camera/sprites use Z for layering).")]
    [SerializeField] private float spellBlastReticleZOffset = 0f;
    [Tooltip("If set, forces the reticle SpriteRenderer's sorting order (helps ensure it's visible).")]
    [SerializeField] private bool spellBlastForceReticleSorting = true;
    [SerializeField] private int spellBlastReticleSortingOrder = 200;
    
    [Header("Spell Blast - Reticle Pulse")]
    [Tooltip("If true, the SpellBlast reticle gently pulses (scale) while it is alive.")]
    [SerializeField] private bool spellBlastReticlePulseEnabled = true;
    [Tooltip("Pulse amount as a fraction of the reticle's original scale. Example: 0.05 = 5% bigger/smaller.")]
    [Min(0f)]
    [SerializeField] private float spellBlastReticlePulseAmount = 0.05f;
    [Tooltip("Pulse speed in cycles per second (Hz). Example: 1.0 = one full pulse per second.")]
    [Min(0f)]
    [SerializeField] private float spellBlastReticlePulseSpeed = 1.1f;
    [Min(0f)]
    [SerializeField] private float spellBlastTelegraphSeconds = 0.75f;
    [Min(0f)]
    [SerializeField] private float spellBlastRadius = 1.25f;
    [Header("Spell Blast - Explosions (VFX)")]
    [Tooltip("One or more explosion VFX prefabs to spawn around the AoE center when it detonates.")]
    [SerializeField] private GameObject[] spellBlastExplosionPrefabs;
    [Min(0)]
    [SerializeField] private int spellBlastExplosionCount = 3;
    [Min(0f)]
    [SerializeField] private float spellBlastExplosionSpawnRadius = 0.9f;
    [Tooltip("Optional: keep spawned explosions organized under a parent transform.")]
    [SerializeField] private Transform spellBlastExplosionParent;
    [Tooltip("Optional z-offset for explosions.")]
    [SerializeField] private float spellBlastExplosionZOffset = 0f;
    [Tooltip("If > 0, force-destroys spawned explosions after this time (seconds). If 0, tries to auto-destroy based on ParticleSystem/Animator/Animation, otherwise leaves it.")]
    [Min(0f)]
    [SerializeField] private float spellBlastExplosionLifetimeSeconds = 0f;
    [Tooltip("If set, forces explosion SpriteRenderer sorting order (helps ensure visibility).")]
    [SerializeField] private bool spellBlastForceExplosionSorting = true;
    [SerializeField] private int spellBlastExplosionSortingOrder = 210;
    [Tooltip("If true, rotates the explosion ring by a random angle each cast (less repetitive).")]
    [SerializeField] private bool spellBlastExplosionRandomizeRingRotation = true;
    [Tooltip("If randomization is disabled, this is the starting angle (degrees) for the ring.")]
    [SerializeField] private float spellBlastExplosionRingStartAngleDegrees = 0f;
    [Tooltip("Optional angle jitter (degrees) applied per explosion to make the ring less perfect.")]
    [Min(0f)]
    [SerializeField] private float spellBlastExplosionAngleJitterDegrees = 0f;
    [Tooltip("Optional radial jitter added/subtracted from the ring radius per explosion.")]
    [Min(0f)]
    [SerializeField] private float spellBlastExplosionRadialJitter = 0f;
    [Tooltip("Damage dealt to PlayerHealth (in lives).")]
    [Min(0)]
    [SerializeField] private int spellBlastPlayerDamage = 1;
    [Tooltip("Optional: damage dealt to BossHealth (float). Only used if the target has BossHealth.")]
    [Min(0f)]
    [SerializeField] private float spellBlastBossDamage = 10f;

    [Header("Summon Reinforcements")]
    [SerializeField] private GameObject archerPrefab;
    [Min(0f)]
    [SerializeField] private float reinforcementSpawnRadius = 3f;
    [Tooltip("Optional: keep spawned archers organized under a parent transform.")]
    [SerializeField] private Transform reinforcementParent;

    // Call from Visual Scripting: SummonReinforcements(n)
    // Summons n archers at random points on a circle around the boss.
    public void SummonReinforcements(int n)
    {
        Debug.Log($"{nameof(BossAttacks)}: Summoning {n} reinforcements.");

        if (archerPrefab == null)
        {
            Debug.LogWarning($"{nameof(BossAttacks)}: Cannot summon reinforcements because archerPrefab is not assigned.", this);
            return;
        }

        int count = Mathf.Max(0, n);
        float radius = Mathf.Max(0f, reinforcementSpawnRadius);

        Vector3 bossPos = transform.position;
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            Vector3 spawnPos = bossPos + offset;
            spawnPos.z = bossPos.z; // keep in the same 2D plane

            GameObject spawned;
            if (reinforcementParent != null)
                spawned = Instantiate(archerPrefab, spawnPos, Quaternion.identity, reinforcementParent);
            else
                spawned = Instantiate(archerPrefab, spawnPos, Quaternion.identity);

            // Play the same smoke animation used for deaths, but without disabling/destroying the archer.
            DeathEffect deathEffect = spawned.GetComponent<DeathEffect>();
            if (deathEffect != null)
            {
                deathEffect.PlaySpawnEffect();
            }
        }
    }

    // Call from Visual Scripting: SpellBlast(target)
    // Spawns a reticle at the target's CURRENT location (does not follow), then applies an AoE hit at that fixed location.
    // IMPORTANT: Damage is applied ONLY to the passed-in target (nothing else can be hurt).
    public void SpellBlast(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(BossAttacks)}.{nameof(SpellBlast)} called with null target.", this);
            return;
        }

        StartCoroutine(SpellBlastRoutine(target));
    }

    private IEnumerator SpellBlastRoutine(GameObject target)
    {
        // Snapshot the target's position at cast time.
        // The reticle and AoE stay at this original location (they do NOT follow the target).
        Transform targetTransform = target != null ? target.transform : null;
        if (targetTransform == null) yield break;

        // Anchor is derived from target bounds when possible so that:
        // - the AoE/explosions are centered under the reticle
        // - offsets from nested sprites/colliders don't cause visual misalignment
        Bounds targetBounds;
        bool hasBounds = TryGetTargetBounds(targetTransform, out targetBounds);

        Vector3 blastPos = hasBounds ? targetBounds.center : targetTransform.position; // fixed AoE center
        blastPos.z = targetTransform.position.z;

        // For the player, spawn the reticle directly on them (not above their head).
        // This also keeps the reticle aligned with the AoE center.
        bool isPlayerTarget = false;
        {
            PlayerHealth ph = target.GetComponent<PlayerHealth>();
            if (ph == null) ph = target.GetComponentInParent<PlayerHealth>();
            isPlayerTarget = (ph != null);
        }
        bool spawnReticleAboveHeadForThisTarget = spellBlastReticleSpawnAboveHead && !isPlayerTarget;
        Vector3 reticlePos = GetSpellBlastReticleWorldPosition(targetTransform, spawnReticleAboveHeadForThisTarget); // computed at cast time

        GameObject reticleInstance = null;
        Vector3 reticleBaseLocalScale = Vector3.one;
        if (spellBlastReticlePrefab != null)
        {
            reticleInstance = Instantiate(spellBlastReticlePrefab, reticlePos, Quaternion.identity);
            reticleInstance.transform.position = reticlePos; // ensure world-space alignment
            reticleBaseLocalScale = reticleInstance.transform.localScale;

            if (spellBlastForceReticleSorting)
            {
                SpriteRenderer sr = reticleInstance.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = spellBlastReticleSortingOrder;
            }
        }
        else
        {
            Debug.LogWarning($"{nameof(BossAttacks)}.{nameof(SpellBlast)}: spellBlastReticlePrefab is not assigned, so no reticle will be shown.", this);
        }

        float delay = Mathf.Max(0f, spellBlastTelegraphSeconds);
        if (delay > 0f)
        {
            // Instead of a single WaitForSeconds, tick so we can gently pulse the reticle.
            float elapsed = 0f;
            float pulseAmount = Mathf.Max(0f, spellBlastReticlePulseAmount);
            float pulseSpeed = Mathf.Max(0f, spellBlastReticlePulseSpeed);
            bool doPulse = spellBlastReticlePulseEnabled && reticleInstance != null && pulseAmount > 0f && pulseSpeed > 0f;

            while (elapsed < delay)
            {
                if (doPulse && reticleInstance != null)
                {
                    // "Breathing" scale: base * (1 ± amount), at a slow speed.
                    float s = 1f + (pulseAmount * Mathf.Sin(elapsed * (Mathf.PI * 2f) * pulseSpeed));
                    Vector3 scaled = new Vector3(reticleBaseLocalScale.x * s, reticleBaseLocalScale.y * s, reticleBaseLocalScale.z);
                    reticleInstance.transform.localScale = scaled;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (target == null) // target may have been destroyed during the telegraph
        {
            if (reticleInstance != null) Destroy(reticleInstance);
            yield break;
        }

        float radius = Mathf.Max(0f, spellBlastRadius);

        // VFX: spawn explosions around the fixed AoE center.
        SpawnSpellBlastExplosions(blastPos);

        // Even though this is an "AoE", we intentionally only damage the provided target.
        // We still respect radius in case you later un-parent the reticle or want a miss possibility.
        Vector3 targetPosNow = target.transform.position;
        Bounds targetBoundsNow;
        if (TryGetTargetBounds(target.transform, out targetBoundsNow))
            targetPosNow = targetBoundsNow.center;

        Vector2 delta = new Vector2(targetPosNow.x - blastPos.x, targetPosNow.y - blastPos.y);
        if (delta.magnitude <= radius + 0.0001f)
        {
            ApplySpellBlastDamageToTargetOnly(target, blastPos, radius);
        }

        if (reticleInstance != null) Destroy(reticleInstance);
    }

    private void SpawnSpellBlastExplosions(Vector3 blastPosition)
    {
        if (spellBlastExplosionPrefabs == null || spellBlastExplosionPrefabs.Length == 0) return;

        int count = Mathf.Max(0, spellBlastExplosionCount);
        float r = Mathf.Max(0f, spellBlastExplosionSpawnRadius);
        if (count <= 0) return;

        float baseAngleDeg = spellBlastExplosionRandomizeRingRotation
            ? Random.Range(0f, 360f)
            : spellBlastExplosionRingStartAngleDegrees;

        float angleJitter = Mathf.Max(0f, spellBlastExplosionAngleJitterDegrees);
        float radialJitter = Mathf.Max(0f, spellBlastExplosionRadialJitter);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = spellBlastExplosionPrefabs[Random.Range(0, spellBlastExplosionPrefabs.Length)];
            if (prefab == null) continue;

            // Evenly spaced ring (prevents clustering), with optional jitter for a less rigid look.
            float angleDeg = baseAngleDeg + (360f * ((float)i / count));
            if (angleJitter > 0f)
                angleDeg += Random.Range(-angleJitter, angleJitter);

            float radius = r;
            if (radialJitter > 0f)
                radius = Mathf.Max(0f, radius + Random.Range(-radialJitter, radialJitter));

            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 offset2D = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
            Vector3 spawnPos = blastPosition + new Vector3(offset2D.x, offset2D.y, spellBlastExplosionZOffset);

            GameObject fx;
            if (spellBlastExplosionParent != null)
                fx = Instantiate(prefab, spawnPos, Quaternion.identity, spellBlastExplosionParent);
            else
                fx = Instantiate(prefab, spawnPos, Quaternion.identity);

            if (spellBlastForceExplosionSorting)
            {
                SpriteRenderer sr = fx.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = spellBlastExplosionSortingOrder;
            }

            TryScheduleDestroyVfx(fx);
        }
    }

    private bool TryGetTargetBounds(Transform targetTransform, out Bounds bounds)
    {
        bounds = default;
        if (targetTransform == null) return false;

        Renderer r = targetTransform.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            bounds = r.bounds;
            return true;
        }

        Collider2D c2 = targetTransform.GetComponentInChildren<Collider2D>();
        if (c2 != null)
        {
            bounds = c2.bounds;
            return true;
        }

        return false;
    }

    private void TryScheduleDestroyVfx(GameObject fx)
    {
        if (fx == null) return;

        if (spellBlastExplosionLifetimeSeconds > 0f)
        {
            Destroy(fx, spellBlastExplosionLifetimeSeconds);
            return;
        }

        // If lifetime isn't forced, try to destroy automatically based on common VFX components.
        ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(fx, duration);
            return;
        }

        Animator animator = fx.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            // Best-effort: read current state's length (may be 0 if not yet playing).
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float len = Mathf.Max(0.01f, stateInfo.length);
            Destroy(fx, len);
            return;
        }

        Animation anim = fx.GetComponentInChildren<Animation>();
        if (anim != null && anim.clip != null)
        {
            Destroy(fx, Mathf.Max(0.01f, anim.clip.length));
            return;
        }
    }

    private Vector3 GetSpellBlastReticleWorldPosition(Transform targetTransform, bool spawnAboveHead)
    {
        Vector3 pos = targetTransform.position;

        Bounds bounds;
        bool hasBounds = TryGetTargetBounds(targetTransform, out bounds);
        if (hasBounds)
        {
            // Keep reticle horizontally centered on the visual/physics bounds.
            pos = bounds.center;
            pos.z = targetTransform.position.z;
        }

        if (!spawnAboveHead)
        {
            pos.z += spellBlastReticleZOffset;
            return pos;
        }

        if (hasBounds)
        {
            // Place above the head using the top of the bounds.
            pos = new Vector3(bounds.center.x, bounds.max.y + spellBlastReticleExtraYOffset, pos.z);
            pos.z += spellBlastReticleZOffset;
            return pos;
        }

        // Fallback if we can't get bounds.
        pos += new Vector3(0f, spellBlastReticleFallbackYOffset, spellBlastReticleZOffset);
        return pos;
    }

    private void ApplySpellBlastDamageToTargetOnly(GameObject target, Vector3 blastPosition, float blastRadius)
    {
        if (target == null) return;

        // Primary intent: hurt the player (and only the player) when targeted.
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(Mathf.Max(0, spellBlastPlayerDamage));
            return;
        }

        // Fallback: support BossHealth targets (useful for testing / special fights).
        BossHealth bossHealth = target.GetComponent<BossHealth>();
        if (bossHealth == null) bossHealth = target.GetComponentInParent<BossHealth>();
        if (bossHealth != null)
        {
            bossHealth.TakeDamage(Mathf.Max(0f, spellBlastBossDamage));
            return;
        }

        // Fallback: support archers (kills them) while still ONLY affecting the passed-in target.
        ArcherBehavior archer = target.GetComponent<ArcherBehavior>();
        if (archer == null) archer = target.GetComponentInParent<ArcherBehavior>();
        if (archer != null)
        {
            archer.TryTakeDamage(blastPosition, Mathf.Max(0.01f, blastRadius));
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Helpful while tuning the radius in the inspector.
        Gizmos.color = new Color(0.7f, 0.2f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, spellBlastRadius));
    }
}
