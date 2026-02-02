using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableShooting : StateMachineBehaviour
{
    private void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Disabling shooting");
        animator.SetBool("IsShooting", false);
    }
}
