using UnityEngine;
using UnityEngine.InputSystem;

// Component for handling weapon input
public class WeaponInputHandler : MonoBehaviour, IWeaponComponent
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference fireInputAction;
    [SerializeField] private InputActionReference reloadInputAction;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private WeaponBase weapon;
    private WeaponEventsSO events;
    private bool isEnabled = false;
    private bool isFireButtonPressed = false;
    
    // Add a press debounce timer to prevent accidental release detection
    private float fireButtonPressTime = 0f;
    private const float INPUT_DEBOUNCE_TIME = 0.05f; // 50ms debounce
    
    // Flag for tracking pause state
    private bool isInputPaused = false;
    
    // Track last fire event time to avoid spamming events
    private float lastFireEventTime = 0f;
    private float fireInputThrottleTime = 0.01f; // Minimum time between fire events
    
    private void OnEnable()
    {
        // Subscribe to global pause events if PauseManager exists
        if (typeof(PauseManager) != null)
        {
            PauseManager.OnInputDisabled += HandleInputDisabled;
            PauseManager.OnInputEnabled += HandleInputEnabled;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from global pause events
        if (typeof(PauseManager) != null)
        {
            PauseManager.OnInputDisabled -= HandleInputDisabled;
            PauseManager.OnInputEnabled -= HandleInputEnabled;
        }
    }
    
    private void HandleInputDisabled()
    {
        isInputPaused = true;
        
        // Make sure we release any pressed inputs when pausing
        if (isFireButtonPressed)
        {
            // Tell the weapon that fire input is released
            if (events != null && weapon != null)
            {
                events.RaiseFireInputChanged(weapon, false);
            }
            isFireButtonPressed = false;
        }
    }
    
    private void HandleInputEnabled()
    {
        isInputPaused = false;
    }
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        events = weapon.WeaponEvents;

        // Initial setup
        fireInputAction.action.performed += OnFirePerformed;
        fireInputAction.action.canceled += OnFireCanceled;
        reloadInputAction.action.performed += OnReloadPerformed;
        
        // Check initial pause state if PauseManager exists
        if (typeof(PauseManager) != null)
        {
            isInputPaused = PauseManager.IsGamePaused;
        }
    }
    
    public void Tick()
    {
        // Skip all input processing if paused
        if (IsGamePaused()) return;
        
        // Handle continuous fire input for automatic weapons
        if (isEnabled && weapon.WeaponData.weaponType == WeaponType.FullAuto)
        {
            // For full auto weapons only - check actual button state
            bool isButtonPhysicallyPressed = fireInputAction.action.IsPressed();
            
            // IMPORTANT: Don't check for release during debounce period
            bool debounceActive = Time.time < fireButtonPressTime + INPUT_DEBOUNCE_TIME;
            
            // Only for automatic weapons - if button is held down, continue firing
            if (isFireButtonPressed && isButtonPhysicallyPressed && 
                Time.time > lastFireEventTime + fireInputThrottleTime)
            {
                // Raise continuous fire event with throttling
                if (enableDebugLogs)
                    Debug.Log($"Auto-fire: button held - frame {Time.frameCount}");
                    
                events.RaiseFireInputChanged(weapon, true);
                lastFireEventTime = Time.time;
            }
            // If we think button is pressed but it's actually released (and past debounce)
            else if (isFireButtonPressed && !isButtonPhysicallyPressed && !debounceActive)
            {
                if (enableDebugLogs)
                    Debug.Log($"Fire released detected in Tick - frame {Time.frameCount}");
                
                // Only signal if weapon is still firing
                if (weapon.CurrentState == WeaponState.Firing)
                {
                    events.RaiseFireInputChanged(weapon, false);
                    isFireButtonPressed = false;
                }
            }
        }
    }
    
    public void OnWeaponEquipped()
    {
        isEnabled = true;
        isFireButtonPressed = false;
        lastFireEventTime = 0f;
        
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
        
        // Reset state
        isFireButtonPressed = false;
        
        // Disable input actions
        fireInputAction.action.Disable();
        reloadInputAction.action.Disable();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from global events
        if (typeof(PauseManager) != null)
        {
            PauseManager.OnInputDisabled -= HandleInputDisabled;
            PauseManager.OnInputEnabled -= HandleInputEnabled;
        }
        
        // Cleanup input action callbacks
        if (fireInputAction?.action != null)
        {
            fireInputAction.action.performed -= OnFirePerformed;
            fireInputAction.action.canceled -= OnFireCanceled;
        }
        
        if (reloadInputAction?.action != null)
        {
            reloadInputAction.action.performed -= OnReloadPerformed;
        }
    }
    
    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        // Skip input processing if paused
        if (!isEnabled || IsGamePaused()) return;
        
        // Set button press state and record press time
        isFireButtonPressed = true;
        fireButtonPressTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log($"Fire button pressed - frame {Time.frameCount}");
        
        // Different behavior based on weapon type
        switch (weapon.WeaponData.weaponType)
        {
            case WeaponType.SemiAuto:
            case WeaponType.Shotgun:
            case WeaponType.Projectile:
            case WeaponType.Burst:
                // Single fire event for non-automatic weapons
                events.RaiseFireInputChanged(weapon, true);
                lastFireEventTime = Time.time;
                break;
            
            case WeaponType.FullAuto:
                // For full-auto, raise the event once on initial press
                events.RaiseFireInputChanged(weapon, true);
                lastFireEventTime = Time.time;
                break;
        }
    }
    
    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        // Always process cancellation, even when paused (for safety)
        if (!isEnabled) return;
        
        if (enableDebugLogs)
            Debug.Log($"Fire button CANCELED event - frame {Time.frameCount}");
        
        // Signal that fire button was released
        events.RaiseFireInputChanged(weapon, false);
        
        // Update local state
        isFireButtonPressed = false;
    }
    
    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        // Skip input processing if paused
        if (!isEnabled || IsGamePaused()) return;
        
        // Signal reload button press
        events.RaiseReloadInputPressed(weapon);
    }
    
    // Helper methods to handle pause checks
    private bool IsGamePaused()
    {
        // If we don't have PauseManager, assume not paused
        if (typeof(PauseManager) == null) return false;
        
        return isInputPaused || PauseManager.IsGamePaused;
    }
}