using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void TriggerDash()
    {
        animator.SetTrigger("Dashes");
    }

    // public void TriggerStumble()
    // {
    //     animator.SetTrigger("Stumbles");
    // }

    public void TriggerSlide()
    {
        animator.SetTrigger("Slides");
    }

}
