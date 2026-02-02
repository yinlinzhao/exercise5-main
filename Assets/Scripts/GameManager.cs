using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Tilemap Overlay")]
    [SerializeField] private Tilemap winScreenTilemap; // The tilemap overlay for win screen
    [SerializeField] private GameObject winScreenTilemapObject; // The GameObject containing the tilemap (for enable/disable)

    [Header("Text Display (Optional)")]
    [SerializeField] private TextMeshProUGUI winTextUI; // UI text (if using Canvas)
    [SerializeField] private TextMeshPro winTextWorld; // World space text (if using world space)
    [SerializeField] private TextMeshProUGUI restartTextUI; // "Press R to Restart" text

    [Header("Settings")]
    [SerializeField] private string winMessage = "You Win";
    [SerializeField] private string deathMessage = "You Died";
    // Kept for backwards compatibility with existing inspector setups.
    // End-of-game freeze is now always applied after the end-screen delay.
    [SerializeField] private bool pauseGameOnWin = true;
    [SerializeField] private float restartPromptDelay = 1.5f; // Delay before showing restart prompt (to allow animations to complete)

    private int totalCoins = 0; // Total coins in scene (all coins, enabled or disabled)
    private bool gameWon = false;
    private bool gameLost = false;
    private bool canRestart = false; // Only true after restart text appears

    void Start()
    {
        // Count all coins in the scene at start (this counts all coins regardless of enabled state)
        CountAllCoins();

        // Hide end-screen overlay initially (shown only on win/death after delay).
        if (winScreenTilemapObject != null)
        {
            winScreenTilemapObject.SetActive(false);
        }
        else if (winScreenTilemap != null)
        {
            winScreenTilemap.gameObject.SetActive(false);
        }

        // Subscribe to coin collection events
        CoinCollector.CoinCollected += HandleCoinCollected;
        Debug.Log("GameManager: Subscribed to CoinCollector.CoinCollected event");
        
        // Subscribe to player death events
        PlayerHealth.PlayerDied += HandlePlayerDeath;
        Debug.Log("GameManager: Subscribed to PlayerHealth.PlayerDied event");

        // Subscribe to boss death events (boss dying counts as a win condition).
        BossHealth.BossDied += HandleBossDeath;
        Debug.Log("GameManager: Subscribed to BossHealth.BossDied event");
        
        // Hide restart text initially
        if (restartTextUI != null)
        {
            restartTextUI.gameObject.SetActive(false);
        }
        
        // Hide end-message text during gameplay (shown only on win/death).
        if (winTextUI != null) winTextUI.gameObject.SetActive(false);
        if (winTextWorld != null) winTextWorld.gameObject.SetActive(false);
        
        // Ensure text state is correct at start (keeps end-message hidden during gameplay).
        UpdateCoinText();
    }

    void Update()
    {
        // Restart (after win/death) with R only.
        if (canRestart && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("GameManager: R pressed while game ended - calling ResetGame()");
            ResetGame();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        CoinCollector.CoinCollected -= HandleCoinCollected;
        PlayerHealth.PlayerDied -= HandlePlayerDeath;
        BossHealth.BossDied -= HandleBossDeath;
    }

    private void CountAllCoins()
    {
        // Count all coins in the scene (including disabled ones)
        CoinCollector[] allCoins = FindObjectsOfType<CoinCollector>(true); // true = include inactive
        totalCoins = allCoins.Length;

        Debug.Log($"GameManager: Found {totalCoins} coins in the scene. Coins remaining: {GetRemainingCoins()}");
    }

    private void HandleCoinCollected(int value)
    {
        if (gameWon || gameLost) return; // Game already ended, ignore

        int remaining = GetRemainingCoins();
        Debug.Log($"GameManager: Coin collected! Coins remaining: {remaining}");

        // Update text to show remaining coins
        UpdateCoinText();

        // Check if all coins are collected (all disabled)
        if (remaining == 0)
        {
            WinGame();
        }
    }

    private void HandlePlayerDeath()
    {
        if (gameWon || gameLost) return; // Game already ended, ignore
        
        LoseGame();
    }

    private void HandleBossDeath()
    {
        if (gameWon || gameLost) return; // Game already ended, ignore

        WinGame();
    }

    private void WinGame()
    {
        gameWon = true;
        Debug.Log("GameManager: All coins collected! You win!");

        // Make player invulnerable so they can't die after winning
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SetInvulnerable(true);
        }

        // Show end UI and freeze after delay (lets any win VFX/animations finish first).
        StartCoroutine(ShowEndScreenAfterDelay());
    }

    private void LoseGame()
    {
        gameLost = true;
        Debug.Log("GameManager: Player died! Game over.");

        // Show end UI and freeze after delay (lets death animation/VFX play first).
        StartCoroutine(ShowEndScreenAfterDelay());
    }

    // Public method to reset (called when R is pressed after winning or dying)
    public void ResetGame()
    {
        Debug.Log("GameManager: ResetGame() called");

        // Ensure time is running again before reload.
        Time.timeScale = 1f;

        // Reload the scene so all coins/enemies (including destroyed archers) reset cleanly.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetPlayer()
    {
        Debug.Log("GameManager: ResetPlayer() called");
        
        // Find the player and reset to home position
        MoveScript player = FindObjectOfType<MoveScript>(true); // true = include inactive
        if (player != null)
        {
            // Re-enable the player GameObject if it was disabled
            if (!player.gameObject.activeSelf)
            {
                player.gameObject.SetActive(true);
            }
            
            // Restore player components if DeathEffect exists
            DeathEffect deathEffect = player.GetComponent<DeathEffect>();
            if (deathEffect != null)
            {
                deathEffect.ReenableComponents();
                Debug.Log("GameManager: Restored player components");
            }
            
            Debug.Log($"GameManager: Found player at {player.transform.position}, calling ResetToHome()");
            player.ResetToHome();
            Debug.Log($"GameManager: Player now at {player.transform.position}");
        }
        else
        {
            Debug.LogWarning("GameManager: Could not find player (MoveScript) to reset!");
        }
    }

    private void ResetPlayerHealth()
    {
        Debug.Log("GameManager: ResetPlayerHealth() called");
        
        // Find the player health component and reset it
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
            Debug.Log("GameManager: Player health reset to full");
        }
        else
        {
            Debug.LogWarning("GameManager: Could not find PlayerHealth component to reset!");
        }
    }

    private void SnapCameraToPlayer()
    {
        // Find the camera and snap it to the player position
        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            MoveScript player = FindObjectOfType<MoveScript>();
            if (player != null)
            {
                // Snap camera directly to player position (with offset)
                Vector3 targetPos = player.transform.position + new Vector3(0, 0, -10);
                cameraFollow.transform.position = targetPos;
                Debug.Log($"GameManager: Camera snapped to {targetPos}");
            }
        }
    }

    private void ReenableAllCoins()
    {
        // Find all coins (including disabled ones)
        CoinCollector[] allCoins = FindObjectsOfType<CoinCollector>(true); // true = include inactive
        
        foreach (CoinCollector coin in allCoins)
        {
            if (coin != null)
            {
                coin.ResetCoin();
            }
        }
        
        Debug.Log($"GameManager: Re-enabled {allCoins.Length} coins. Coins remaining: {GetRemainingCoins()}");
    }

    private void ReenableAllArchers()
    {
        // Find all archers (including ones with disabled components)
        ArcherBehavior[] allArchers = FindObjectsOfType<ArcherBehavior>(true); // true = include inactive
        
        foreach (ArcherBehavior archer in allArchers)
        {
            if (archer != null)
            {
                archer.ResetArcher();
            }
        }
        
        Debug.Log($"GameManager: Re-enabled {allArchers.Length} archers");
    }

    private void UpdateCoinText()
    {
        string textToShow;
        int remaining = GetRemainingCoins();
        
        if (gameLost)
        {
            textToShow = deathMessage;
        }
        else if (gameWon || remaining == 0)
        {
            textToShow = winMessage;
        }
        else
        {
            // During gameplay, keep the win/death message hidden.
            textToShow = string.Empty;
        }

        Debug.Log($"GameManager: Updating text to '{textToShow}'");

        // Update UI text if assigned
        if (winTextUI != null)
        {
            winTextUI.text = textToShow;
            winTextUI.gameObject.SetActive(gameWon || gameLost);
            Debug.Log("GameManager: Updated winTextUI");
        }
        else
        {
            Debug.LogWarning("GameManager: winTextUI is not assigned!");
        }

        // Update world space text if assigned
        if (winTextWorld != null)
        {
            winTextWorld.text = textToShow;
            winTextWorld.gameObject.SetActive(gameWon || gameLost);
            Debug.Log("GameManager: Updated winTextWorld");
        }
    }

    // Public getters for UI display
    public int GetTotalCoins() => totalCoins;
    
    // Get remaining coins by counting currently enabled coins
    public int GetRemainingCoins()
    {
        CoinCollector[] enabledCoins = FindObjectsOfType<CoinCollector>(); // Only finds active/enabled coins
        return enabledCoins.Length;
    }
    
    public int GetCollectedCoins() => totalCoins - GetRemainingCoins();
    public float GetCollectionProgress() => totalCoins > 0 ? (float)GetCollectedCoins() / totalCoins : 0f;
    
    /// <summary>
    /// Coroutine that shows end UI and freezes the game after a delay
    /// (uses unscaled time so the delay works even if timeScale changes elsewhere).
    /// </summary>
    private IEnumerator ShowEndScreenAfterDelay()
    {
        // Wait for the specified delay (using unscaled time so it works even when game is paused)
        yield return new WaitForSecondsRealtime(restartPromptDelay);

        // Show end-screen overlay (if assigned)
        if (winScreenTilemapObject != null)
        {
            winScreenTilemapObject.SetActive(true);
        }
        else if (winScreenTilemap != null)
        {
            winScreenTilemap.gameObject.SetActive(true);
        }
        
        // Update main text to show win/death message now (after the delay).
        UpdateCoinText();
        
        // Show restart text
        if (restartTextUI != null)
        {
            restartTextUI.text = "Press R to Restart";
            restartTextUI.gameObject.SetActive(true);
        }
        
        // Now allow the player to restart
        canRestart = true;
        
        // Freeze the game at the same time the end UI becomes visible.
        Time.timeScale = 0f;
    }
}
