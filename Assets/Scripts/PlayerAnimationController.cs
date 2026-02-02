using System.Collections;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private MoveScript moveScript;
    private PlayerAttack playerAttack;
    private SpriteRenderer spriteRenderer;

    void Start() {
        animator = GetComponent<Animator>();
        moveScript = GetComponent<MoveScript>();
        playerAttack = GetComponent<PlayerAttack>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Check if player is moving (and not attacking or guarding)
        bool IsRunning = moveScript.IsRunning && !moveScript.IsAttacking && !moveScript.IsGuarding;
        
        // Set the IsRunning parameter
        animator.SetBool("IsRunning", IsRunning);
        
        // Set the Attack parameter
        animator.SetBool("IsAttacking", moveScript.IsAttacking);
        
        // Set the Guard parameter
        animator.SetBool("IsGuarding", moveScript.IsGuarding);
    }
}
