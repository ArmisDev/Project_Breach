using System;
using FPS.Player;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    private WeaponController weaponController;
    [SerializeField] private Animator animator;

    // Name of the animation trigger parameter
    [SerializeField] private string fireTriggerName = "Shoot";

    // Optional - for the auto-return to Init state
    [SerializeField] private string fireCompleteName = "FireComplete";

    // Always reset the trigger before firing (solves most issues)
    [SerializeField] private bool resetAnimationBeforeFiring = true;

    void Start()
    {
        weaponController = GetComponent<WeaponController>();
        weaponController.OnWeaponFired += PlayFireAnimation;
        weaponController.OnWeaponReload += OnReload;
    }

    void Update()
    {
        animator.SetFloat("PlayerSpeed", playerController.playerVelocity.magnitude);
    }

    void PlayFireAnimation()
    {
        // First, make sure any previous "FireComplete" is reset
        animator.SetBool(fireCompleteName, false);

        // Reset the trigger first to ensure it can be triggered again
        if (resetAnimationBeforeFiring)
        {
            animator.ResetTrigger(fireTriggerName);
        }

        // Set the trigger to play the fire animation
        animator.SetTrigger(fireTriggerName);
    }

    // Call this via Animation Event at the end of your fire animation
    public void OnFireAnimationComplete()
    {
        animator.SetBool(fireCompleteName, true);
    }

    public void OnReload()
    {
        animator.SetBool("Reload", true);
    }

    public void OnReloadComplete()
    {
        animator.SetTrigger("Reload");
    }
}