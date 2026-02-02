using UnityEngine;
using System.Collections;

/// <summary>
/// Handles death effects (like smoke animations) when a GameObject is destroyed.
/// Attach this to any GameObject that should play a death effect.
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [Header("Death Effect Settings")]
    [Tooltip("The smoke animation prefab or particle system to instantiate on death")]
    [SerializeField] private GameObject smokeEffectPrefab;
    
    [Tooltip("Offset from the GameObject's position where the smoke should appear (default: above)")]
    [SerializeField] private Vector3 effectOffset = new Vector3(0, 0.5f, 0);
    
    [Tooltip("Delay before destroying the GameObject (to allow smoke animation to start)")]
    [SerializeField] private float destroyDelay = 0.1f;
    
    [Tooltip("If true, the smoke effect will be a child of this GameObject. If false, it will be independent.")]
    [SerializeField] private bool parentEffectToObject = false;
    
    private bool isDying = false;

    /// <summary>
    /// Plays only the smoke effect (no disabling/destroying).
    /// Useful for spawn/teleport VFX while keeping the object alive.
    /// </summary>
    public void PlaySpawnEffect()
    {
        SpawnSmokeEffect(transform.position);
    }

    /// <summary>
    /// Plays the death effect and destroys the GameObject after a delay.
    /// Does not destroy the player (objects with PlayerHealth component).
    /// Call this method when the object should die.
    /// </summary>
    public void PlayDeathEffect()
    {
        if (isDying) return; // Prevent multiple calls
        isDying = true;
        
        // Don't destroy the player (it is restored on game reset).
        bool isPlayer = GetComponent<PlayerHealth>() != null;
        
        SpawnSmokeEffect(transform.position);
        
        // Disable the GameObject's renderer and collider immediately for visual feedback
        DisableComponents();
        
        // Destroy everything except the player (archers/enemies should be removed from the hierarchy).
        if (!isPlayer)
        {
            // Destroy the GameObject after a short delay
            Destroy(gameObject, destroyDelay);
        }
    }

    private void SpawnSmokeEffect(Vector3 originPosition)
    {
        // Play the smoke effect
        if (smokeEffectPrefab == null) return;

        Vector3 spawnPosition = originPosition + effectOffset;
        GameObject smokeEffect = Instantiate(smokeEffectPrefab, spawnPosition, Quaternion.identity);

        // Optionally parent the effect to this object
        if (parentEffectToObject)
        {
            smokeEffect.transform.SetParent(transform);
        }

        // Handle particle systems - auto-destroy when finished
        ParticleSystem particles = smokeEffect.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            // If it's a particle system, destroy it after it finishes playing
            float duration = particles.main.duration + particles.main.startLifetime.constantMax;
            Destroy(smokeEffect, duration);
        }
        // Handle animator-based animations - auto-destroy when finished
        else
        {
            Animator animator = smokeEffect.GetComponent<Animator>();
            if (animator != null)
            {
                // Start a coroutine on the smoke effect itself so it continues even if this GameObject is destroyed
                // Add a component to the smoke effect to run the coroutine
                SmokeDestroyer destroyer = smokeEffect.AddComponent<SmokeDestroyer>();
                destroyer.Initialize(animator);
            }
            // Handle legacy Animation component
            else
            {
                Animation animation = smokeEffect.GetComponent<Animation>();
                if (animation != null && animation.clip != null)
                {
                    // Destroy after the animation clip finishes
                    Destroy(smokeEffect, animation.clip.length);
                }
            }
        }
    }
    
    /// <summary>
    /// Re-enables all components that were disabled by DisableComponents().
    /// Used to restore the player after death.
    /// </summary>
    public void ReenableComponents()
    {
        // Reset the dying state
        isDying = false;
        
        // Re-enable sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        // Re-enable all colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        // Re-enable animator
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
        }
        
        // Re-enable all MonoBehaviour scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && 
                !(script is Transform))
            {
                script.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// Disables visual and collision components so the object appears to disappear
    /// while the smoke effect plays.
    /// </summary>
    private void DisableComponents()
    {
        // Disable sprite renderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Disable all colliders
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable animator
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Disable any movement scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            // Don't disable this script or essential Unity components
            if (script != this && script != null && 
                !(script is Transform) && 
                !(script is DeathEffect))
            {
                script.enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Helper component that runs on the smoke effect GameObject to destroy it after animation completes.
    /// This ensures the coroutine continues even if the original GameObject is destroyed.
    /// </summary>
    private class SmokeDestroyer : MonoBehaviour
    {
        private Animator animator;
        
        public void Initialize(Animator anim)
        {
            animator = anim;
            StartCoroutine(DestroyAfterAnimation());
        }
        
        private IEnumerator DestroyAfterAnimation()
        {
            // Wait a frame to ensure the animator has started playing
            yield return null;
            
            // Get the current animation state info
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float animationLength = stateInfo.length;
            
            // Wait for the animation to complete
            yield return new WaitForSeconds(animationLength);
            
            // Destroy the smoke effect GameObject (which this component is attached to)
            Destroy(gameObject);
        }
    }
    
}
