using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Sprite heartSprite; // Sprite to use for heart images
    [SerializeField] private Canvas canvas; // Canvas to spawn hearts on (if null, will find automatically)
    [SerializeField] private RectTransform referenceTransform; // Transform to use as reference point for heart positioning
    
    [Header("Heart Display Settings")]
    [SerializeField] private float heartSize = 50f; // Size of each heart image
    [SerializeField] private float heartSpacing = 10f; // Spacing between hearts

    private PlayerHealth playerHealth;
    private List<GameObject> heartImages = new List<GameObject>();
    private RectTransform canvasRect;

    void Start()
    {
        // Find the player's health component
        playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthUI: Could not find PlayerHealth component in scene!");
            return;
        }

        // Find canvas if not assigned
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("PlayerHealthUI: No Canvas found in scene! Hearts cannot be displayed.");
                return;
            }
        }

        canvasRect = canvas.GetComponent<RectTransform>();

        // Subscribe to lives changed event
        PlayerHealth.LivesChanged += UpdateHealthDisplay;
        
        // Create initial heart images
        CreateHeartImages();
        
        // Update display with initial health
        UpdateHealthDisplay(playerHealth.CurrentLives);
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        PlayerHealth.LivesChanged -= UpdateHealthDisplay;
        
        // Clean up heart images
        ClearHeartImages();
    }

    private void CreateHeartImages()
    {
        if (playerHealth == null || canvas == null) return;

        if (referenceTransform == null)
        {
            Debug.LogError("PlayerHealthUI: Reference transform is not assigned! Please assign a transform in the inspector.");
            return;
        }

        // Clear any existing hearts
        ClearHeartImages();

        int maxLives = playerHealth.MaxLives;

        // Create heart images for max lives
        for (int i = 0; i < maxLives; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(referenceTransform, false);

            // Add Image component
            Image heartImage = heartObj.AddComponent<Image>();
            if (heartSprite != null)
            {
                heartImage.sprite = heartSprite;
            }
            else
            {
                // If no sprite assigned, create a simple colored square as fallback
                heartImage.color = Color.red;
                Debug.LogWarning("PlayerHealthUI: No heart sprite assigned! Using colored square as fallback.");
            }

            // Set up RectTransform
            RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(heartSize, heartSize);
            rectTransform.anchorMin = new Vector2(0f, 0f); // Bottom left anchor
            rectTransform.anchorMax = new Vector2(0f, 0f); // Bottom left anchor
            rectTransform.pivot = new Vector2(0f, 0f); // Pivot at bottom left

            // Position first heart at origin of reference transform, subsequent hearts to the left
            float xPosition = -(i * (heartSize + heartSpacing));
            rectTransform.anchoredPosition = new Vector2(xPosition, 0f);

            heartImages.Add(heartObj);
        }
    }

    private void UpdateHealthDisplay(int currentLives)
    {
        // Show/hide hearts based on current lives
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (heartImages[i] != null)
            {
                heartImages[i].SetActive(i < currentLives);
            }
        }
    }

    private void ClearHeartImages()
    {
        foreach (GameObject heart in heartImages)
        {
            if (heart != null)
            {
                Destroy(heart);
            }
        }
        heartImages.Clear();
    }
}
