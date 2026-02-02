using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxLives = 3;
    
    private int currentLives;
    private bool isInvulnerable = false;
    
    // Event for when lives change (useful for UI updates)
    public static Action<int> LivesChanged;
    
    // Event for when player dies (runs out of lives)
    public static Action PlayerDied;
    
    // Public property to check current lives
    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;
    public bool IsDead => currentLives <= 0;

    void Start()
    {
        // Initialize with max lives
        currentLives = maxLives;
        NotifyLivesChanged();
    }

    /// <summary>
    /// Makes the player take damage, reducing lives by the specified amount.
    /// </summary>
    /// <param name="damage">Amount of damage to take (default 1)</param>
    /// <returns>True if player died, false otherwise</returns>
    public bool TakeDamage(int damage = 1)
    {
        if (IsDead) return true; // Already dead
        if (isInvulnerable) return false; // Can't take damage while invulnerable
        
        currentLives = Mathf.Max(0, currentLives - damage);
        NotifyLivesChanged();
        
        Debug.Log($"Player took {damage} damage! Lives remaining: {currentLives}");
        
        if (IsDead)
        {
            OnPlayerDeath();
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Restores the player to full health/lives.
    /// </summary>
    public void RestoreFullHealth()
    {
        currentLives = maxLives;
        NotifyLivesChanged();
    }

    /// <summary>
    /// Adds lives to the player (for power-ups, etc.).
    /// </summary>
    public void AddLives(int amount)
    {
        currentLives = Mathf.Min(maxLives, currentLives + amount);
        NotifyLivesChanged();
    }

    private void NotifyLivesChanged()
    {
        if (LivesChanged != null)
        {
            LivesChanged.Invoke(currentLives);
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Player died! No lives remaining.");
        
        // Play death effect (smoke animation) if DeathEffect component exists
        DeathEffect deathEffect = GetComponent<DeathEffect>();
        if (deathEffect != null)
        {
            deathEffect.PlayDeathEffect();
        }
        
        if (PlayerDied != null)
        {
            PlayerDied.Invoke();
        }
        
        // You can add death logic here (disable player, show game over, etc.)
        // For now, we'll just log it
    }

    // Public method to reset health (called by game reset systems)
    public void ResetHealth()
    {
        isInvulnerable = false;
        RestoreFullHealth();
    }

    /// <summary>
    /// Sets the player's invulnerability state.
    /// </summary>
    public void SetInvulnerable(bool invulnerable)
    {
        isInvulnerable = invulnerable;
        Debug.Log($"Player invulnerability set to: {invulnerable}");
    }
}
