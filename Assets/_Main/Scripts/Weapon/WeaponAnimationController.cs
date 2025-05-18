using FPS.Player;
using UnityEngine;

// Component for handling weapon animations
public class WeaponAnimationController : MonoBehaviour, IWeaponComponent
{
    [Header("Animation References")]
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private PlayerController playerController; // Reference to player controller for movement parameters
    
    [Header("Animation Parameters")]
    [SerializeField] private string fireAnimTrigger = "Shoot";
    [SerializeField] private string reloadAnimTrigger = "Reload";
    [SerializeField] private string equipAnimTrigger = "Equip";
    [SerializeField] private string fireCompleteParam = "FireComplete";
    [SerializeField] private string reloadCompleteParam = "ReloadComplete";
    [SerializeField] private string playerSpeedParam = "PlayerSpeed";
    
    [Header("Animation Options")]
    [SerializeField] private bool resetTriggerBeforeFiring = true;
    
    // Internal state
    private WeaponBase weapon;
    private WeaponDataSO weaponData;
    private WeaponEventsSO events;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        weaponData = weapon.WeaponData;
        events = weapon.WeaponEvents;
        
        // Use parameters from weapon data if available
        if (!string.IsNullOrEmpty(weaponData.fireAnimationTrigger))
        {
            fireAnimTrigger = weaponData.fireAnimationTrigger;
        }
        
        if (!string.IsNullOrEmpty(weaponData.reloadAnimationTrigger))
        {
            reloadAnimTrigger = weaponData.reloadAnimationTrigger;
        }
        
        if (!string.IsNullOrEmpty(weaponData.equipAnimationTrigger))
        {
            equipAnimTrigger = weaponData.equipAnimationTrigger;
        }
        
        // Subscribe to events
        events.OnWeaponFired += HandleWeaponFired;
        events.OnReloadStarted += HandleReloadStarted;
        events.OnWeaponEquipped += HandleWeaponEquipped;
        events.OnWeaponStateChanged += HandleWeaponStateChanged;
    }
    
    public void Tick()
    {
        // Update animator parameters that change over time
        if (weaponAnimator != null && playerController != null)
        {
            weaponAnimator.SetFloat(playerSpeedParam, playerController.playerVelocity.magnitude);
        }
    }
    
    public void OnWeaponEquipped()
    {
        PlayEquipAnimation();
    }
    
    public void OnWeaponUnequipped()
    {
        // Nothing specific to do on unequip
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (events != null)
        {
            events.OnWeaponFired -= HandleWeaponFired;
            events.OnReloadStarted -= HandleReloadStarted;
            events.OnWeaponEquipped -= HandleWeaponEquipped;
            events.OnWeaponStateChanged -= HandleWeaponStateChanged;
        }
    }
    
    // Play fire animation
    public void PlayFireAnimation()
    {
        if (weaponAnimator == null) return;
        
        // First, make sure any previous "FireComplete" is reset
        weaponAnimator.SetBool(fireCompleteParam, false);
        
        // Reset the trigger first to ensure it can be triggered again
        if (resetTriggerBeforeFiring)
        {
            weaponAnimator.ResetTrigger(fireAnimTrigger);
        }
        
        // Set the trigger to play the fire animation
        weaponAnimator.SetTrigger(fireAnimTrigger);
    }
    
    // Play reload animation
    public void PlayReloadAnimation()
    {
        if (weaponAnimator == null) return;
        
        // Reset the reload complete flag
        weaponAnimator.SetBool(reloadCompleteParam, false);
        
        // Trigger reload animation
        weaponAnimator.SetTrigger(reloadAnimTrigger);
    }
    
    // Play equip animation
    public void PlayEquipAnimation()
    {
        if (weaponAnimator == null) return;
        
        // Trigger equip animation
        weaponAnimator.SetTrigger(equipAnimTrigger);
    }
    
    // Animation event callback - called from animation
    public void OnFireAnimationComplete()
    {
        if (weaponAnimator == null) return;
        
        // Set the completion flag
        weaponAnimator.SetBool(fireCompleteParam, true);
        
        // Notify via event
        events.RaiseAnimationEvent(weapon, "FireComplete");
    }
    
    // Animation event callback - called from animation
    public void OnReloadAnimationComplete()
    {
        if (weaponAnimator == null) return;
        
        // Set the completion flag
        weaponAnimator.SetBool(reloadCompleteParam, true);
        
        // Notify via event
        events.RaiseAnimationEvent(weapon, "ReloadComplete");
    }
    
    // Animation event callbacks for audio cues
    public void AnimEvent_ClipOut() => events.RaiseAnimationEvent(weapon, "ClipOut");
    public void AnimEvent_ClipIn() => events.RaiseAnimationEvent(weapon, "ClipIn");
    public void AnimEvent_SlideBack() => events.RaiseAnimationEvent(weapon, "SlideBack");
    public void AnimEvent_SlidePull() => events.RaiseAnimationEvent(weapon, "SlidePull");
    public void AnimEvent_SlideRelease() => events.RaiseAnimationEvent(weapon, "SlideRelease");
    
    #region Event Handlers
    
    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayFireAnimation();
    }
    
    private void HandleReloadStarted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayReloadAnimation();
    }
    
    private void HandleWeaponEquipped(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayEquipAnimation();
    }
    
    private void HandleWeaponStateChanged(WeaponBase targetWeapon, WeaponState newState)
    {
        if (targetWeapon != weapon || weaponAnimator == null) return;
        
        // Update animator state booleans
        weaponAnimator.SetBool("IsReloading", newState == WeaponState.Reloading);
        weaponAnimator.SetBool("IsEmpty", newState == WeaponState.Empty);
    }
    
    #endregion
}