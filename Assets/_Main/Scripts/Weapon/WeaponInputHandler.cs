using UnityEngine;
using UnityEngine.InputSystem;

// Component for handling weapon input
public class WeaponInputHandler : MonoBehaviour, IWeaponComponent
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference fireInputAction;
    [SerializeField] private InputActionReference reloadInputAction;
    
    private WeaponBase weapon;
    private WeaponEventsSO events;
    private bool isEnabled = false;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        events = weapon.WeaponEvents;
        
        // Initial setup
        fireInputAction.action.performed += OnFirePerformed;
        fireInputAction.action.canceled += OnFireCanceled;
        reloadInputAction.action.performed += OnReloadPerformed;
    }
    
    public void Tick()
    {
        // Handle continuous fire input for automatic weapons
        if (isEnabled && 
            weapon.WeaponData.weaponType == WeaponType.FullAuto && 
            fireInputAction.action.IsPressed())
        {
            events.RaiseFireInputChanged(weapon, true);
        }
    }
    
    public void OnWeaponEquipped()
    {
        isEnabled = true;
        
        // Enable input actions
        fireInputAction.action.Enable();
        reloadInputAction.action.Enable();
    }
    
    public void OnWeaponUnequipped()
    {
        isEnabled = false;
        
        // Release input actions
        if (fireInputAction.action.IsPressed())
        {
            events.RaiseFireInputChanged(weapon, false);
        }
        
        // Disable input actions
        fireInputAction.action.Disable();
        reloadInputAction.action.Disable();
    }
    
    private void OnDestroy()
    {
        // Cleanup
        fireInputAction.action.performed -= OnFirePerformed;
        fireInputAction.action.canceled -= OnFireCanceled;
        reloadInputAction.action.performed -= OnReloadPerformed;
    }
    
    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        // Different behavior based on weapon type
        switch (weapon.WeaponData.weaponType)
        {
            case WeaponType.SemiAuto:
            case WeaponType.Shotgun:
            case WeaponType.Projectile:
            case WeaponType.Burst:
                // Single fire event for non-automatic weapons
                events.RaiseFireInputChanged(weapon, true);
                break;
        }
    }
    
    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        // Signal that fire button was released
        events.RaiseFireInputChanged(weapon, false);
    }
    
    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (!isEnabled) return;
        
        // Signal reload button press
        events.RaiseReloadInputPressed(weapon);
    }
}