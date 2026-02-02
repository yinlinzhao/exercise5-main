using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Arrow Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 5f; // How long before arrow despawns
    [SerializeField] private int damage = 1;
    [SerializeField] private float collisionRadius = 0.5f; // Distance check radius for collision
    
    private Vector3 direction;
    private bool isInitialized = false;
    private GameObject player;
    private PlayerHealth playerHealth;
    private MoveScript playerMoveScript;
    
    void Start()
    {
        // Destroy arrow after lifetime
        Destroy(gameObject, lifetime);
        
        // Cache player reference
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerMoveScript = player.GetComponent<MoveScript>();
        }
    }
    
    void Update()
    {
        if (isInitialized)
        {
            // Move arrow using transform (no Rigidbody needed)
            transform.position += direction * speed * Time.deltaTime;
            
            // Check for collision with player using collider bounds or distance
            CheckForPlayerCollision();
        }
    }
    
    private void CheckForPlayerCollision()
    {
        if (player == null || playerHealth == null) return;
        
        bool isColliding = false;
        
        // Method 1: Check if arrow is within player's collider bounds (if player has a collider)
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null && playerCollider.bounds.Contains(transform.position))
        {
            isColliding = true;
        }
        // Method 2: Fallback to distance check if no collider or bounds check fails
        else
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= collisionRadius)
            {
                isColliding = true;
            }
        }
        
        // If collision detected, handle it
        if (isColliding)
        {
            // Check if player is guarding - if so, arrow is blocked but still destroyed
            bool isGuarding = playerMoveScript != null && playerMoveScript.IsGuarding;
            
            if (isGuarding)
            {
                // Arrow is blocked by guard - destroy arrow but don't deal damage
                Destroy(gameObject);
            }
            else
            {
                // Normal hit - deal damage and destroy arrow
                playerHealth.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
    
    public void SetDirection(Vector3 dir, float arrowSpeed)
    {
        direction = dir.normalized;
        speed = arrowSpeed;
        isInitialized = true;
        
        // Rotate arrow to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
