using System.Collections;
using UnityEngine;

// Component that manages weapon state transitions and core firing logic
public class WeaponStateManager : MonoBehaviour, IWeaponComponent
{
    [Header("Effects")]
    [SerializeField] private GameObject muzzleFlashObject;
    [SerializeField] private ParticleSystem muzzleFlashParticle;
    
    [Header("Firing Setup")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float maxRange = 100f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Internal state
    private WeaponBase weapon;
    private WeaponDataSO weaponData;
    private WeaponEventsSO events;
    private WeaponAmmoSystem ammoSystem;
    
    private bool canFire = true;
    private float lastFireTime = -100f; // Initialize to a value that will allow immediate first shot
    private int burstCount = 0;
    private bool isFiring = false;
    private Coroutine firingCoroutine;
    
    // New flag for single-shot prevention
    private bool hasProcessedFirstShot = false;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        weaponData = weapon.WeaponData;
        events = weapon.WeaponEvents;
        
        // Get ammo system reference
        ammoSystem = GetComponent<WeaponAmmoSystem>();
        if (ammoSystem == null)
        {
            Debug.LogError("WeaponStateManager requires a WeaponAmmoSystem component!");
        }
        
        // Setup muzzle flash
        if (muzzleFlashObject != null)
        {
            muzzleFlashObject.SetActive(false);
        }
        
        // Subscribe to events
        events.OnFireInputChanged += HandleFireInputChanged;
        events.OnReloadStarted += HandleReloadStarted;
        events.OnReloadCompleted += HandleReloadCompleted;
        events.OnMagazineEmpty += HandleMagazineEmpty;
        events.OnAnimationEvent += HandleAnimationEvent;
    }
    
    public void Tick()
    {
        // Handle fire rate timing for continuous fire modes
        if (isFiring && 
            (weapon.CurrentState == WeaponState.Ready || weapon.CurrentState == WeaponState.Firing) &&
            weaponData.weaponType == WeaponType.FullAuto &&
            Time.time >= lastFireTime + weaponData.fireRate)
        {
            // Process automatic firing
            if (enableDebugLogs)
                Debug.Log($"Auto-fire tick check - frame {Time.frameCount}, last shot {Time.time - lastFireTime:F3}s ago");
                
            TryFireWeapon();
        }
    }
    
    public void OnWeaponEquipped()
    {
        canFire = true;
        isFiring = false;
        hasProcessedFirstShot = false; 
        lastFireTime = -100f; // Reset to allow immediate first shot
    }
    
    public void OnWeaponUnequipped()
    {
        StopAllFiringCoroutines();
        
        // Reset all firing state when weapon is unequipped
        isFiring = false;
        hasProcessedFirstShot = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (events != null)
        {
            events.OnFireInputChanged -= HandleFireInputChanged;
            events.OnReloadStarted -= HandleReloadStarted;
            events.OnReloadCompleted -= HandleReloadCompleted;
            events.OnMagazineEmpty -= HandleMagazineEmpty;
            events.OnAnimationEvent -= HandleAnimationEvent;
        }
    }
    
    // Attempt to fire the weapon based on current state
    private bool TryFireWeapon()
    {
        // Verify we can fire
        if (!canFire || 
            (weapon.CurrentState != WeaponState.Ready && weapon.CurrentState != WeaponState.Firing))
        {
            if (enableDebugLogs)
                Debug.Log($"Cannot fire: canFire={canFire}, state={weapon.CurrentState}");
            return false;
        }
        
        // Check if enough time has passed since last shot (respect fire rate)
        if (Time.time < lastFireTime + weaponData.fireRate)
        {
            if (enableDebugLogs)
                Debug.Log($"Fire rate limit: {Time.time - lastFireTime:F3}s < {weaponData.fireRate:F3}s");
            return false;
        }
        
        // Check ammo
        if (ammoSystem != null && !ammoSystem.CanFire())
        {
            events.RaiseWeaponFailedToFire(weapon);
            weapon.ChangeState(WeaponState.Empty);
            
            if (enableDebugLogs)
                Debug.Log("Cannot fire: out of ammo");
            return false;
        }
        
        // Track fire time
        lastFireTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log($"Firing weapon - frame {Time.frameCount}");
        
        // Perform fire
        FireWeapon();
        
        // Change state
        weapon.ChangeState(WeaponState.Firing);
        
        return true;
    }
    
    // Actual firing logic
    private void FireWeapon()
    {
        // Show muzzle flash
        ShowMuzzleFlash();
        
        // Perform hit detection
        PerformHitDetection();
        
        // Notify weapon fired
        events.RaiseWeaponFired(weapon);
    }
    
    // Show muzzle flash effect
    private void ShowMuzzleFlash()
    {
        if (muzzleFlashObject != null)
        {
            StartCoroutine(FlashCoroutine());
        }
        
        if (muzzleFlashParticle != null)
        {
            muzzleFlashParticle.Play();
        }
    }
    
    // Flash effect coroutine
    private IEnumerator FlashCoroutine()
    {
        muzzleFlashObject.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        muzzleFlashObject.SetActive(false);
    }
    
    // Perform hit detection
    private void PerformHitDetection()
    {
        // Implement raycast or projectile spawning based on weapon type
        if (firePoint == null)
        {
            Debug.LogWarning("No fire point assigned for weapon hit detection");
            return;
        }
        
        // Basic raycast implementation (extend for more complex weapons)
        if (Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, maxRange, hitLayers))
        {
            // Apply damage to hit object if it has a health component
            var damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(weaponData.damage);
            }
            
            // Spawn impact effect, apply force, etc.
            Debug.DrawLine(firePoint.position, hit.point, Color.red, 1.0f);
        }
    }
    
    // Handle burst fire mode
    private IEnumerator BurstFireCoroutine()
    {
        int burstCount = 0;
        
        while (burstCount < weaponData.burstSize)
        {
            if (TryFireWeapon())
            {
                burstCount++;
                
                // Wait for fire rate before next burst shot
                yield return new WaitForSeconds(weaponData.fireRate);
            }
            else
            {
                // Couldn't fire (likely out of ammo), so exit burst
                break;
            }
        }
        
        // Reset burst state
        isFiring = false;
        
        // Return to ready state if we can still fire
        if (ammoSystem != null && ammoSystem.CanFire())
        {
            weapon.ChangeState(WeaponState.Ready);
        }
        
        firingCoroutine = null;
    }
    
    // Stop all firing coroutines
    private void StopAllFiringCoroutines()
    {
        if (firingCoroutine != null)
        {
            StopCoroutine(firingCoroutine);
            firingCoroutine = null;
        }
        
        isFiring = false;
    }
    
    #region Event Handlers
    
    private void HandleFireInputChanged(WeaponBase targetWeapon, bool isPressed)
    {
        if (targetWeapon != weapon) return;
        
        // Store the input state
        isFiring = isPressed;
        
        if (enableDebugLogs)
            Debug.Log($"Fire input changed: {isPressed}, Weapon Type: {weaponData.weaponType}, Frame: {Time.frameCount}"); 
        
        // Handle fire input based on weapon type
        switch (weaponData.weaponType)
        {
            case WeaponType.SemiAuto:
            case WeaponType.Shotgun:
            case WeaponType.Projectile:
                // Only fire on press, not hold
                if (isPressed)
                {
                    TryFireWeapon();
                }
                else if (weapon.CurrentState == WeaponState.Firing)
                {
                    // Return to ready state when button released
                    weapon.ChangeState(WeaponState.Ready);
                }
                break;
                
            case WeaponType.FullAuto:
                // For full auto, handle both press and release
                if (isPressed)
                {
                    // Reset the first shot flag when button is pressed
                    hasProcessedFirstShot = false;
                    
                    // Process first shot immediately
                    if (enableDebugLogs)
                        Debug.Log($"Processing initial auto shot - frame {Time.frameCount}");
                    
                    TryFireWeapon();
                }
                // When released, ALWAYS transition back to ready state
                else if (!isPressed)
                {
                    // Stop firing immediately when button is released
                    StopAllFiringCoroutines();
                    
                    if (enableDebugLogs)
                        Debug.Log($"Auto-fire released - frame {Time.frameCount}");
                    
                    // Ensure we return to ready state 
                    if (weapon.CurrentState == WeaponState.Firing)
                    {
                        weapon.ChangeState(WeaponState.Ready);
                    }
                }
                break;
                
            case WeaponType.Burst:
                // Start burst on press if not already bursting
                if (isPressed && firingCoroutine == null)
                {
                    firingCoroutine = StartCoroutine(BurstFireCoroutine());
                }
                break;
        }
    }
    
    private void HandleReloadStarted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        
        // Stop any firing when reload starts
        StopAllFiringCoroutines();
        canFire = false;
        
        // Set weapon state
        weapon.ChangeState(WeaponState.Reloading);
    }
    
    private void HandleReloadCompleted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        
        // Re-enable firing
        canFire = true;
        
        // Return to ready state
        weapon.ChangeState(WeaponState.Ready);
    }
    
    private void HandleMagazineEmpty(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        
        // Set state to empty
        weapon.ChangeState(WeaponState.Empty);
    }
    
    private void HandleAnimationEvent(WeaponBase targetWeapon, string eventName)
    {
        if (targetWeapon != weapon) return;
        
        // Handle specific animation events that affect state
        switch (eventName)
        {
            case "FireComplete":
                if (weapon.CurrentState == WeaponState.Firing && !isFiring)
                {
                    weapon.ChangeState(WeaponState.Ready);
                }
                break;
                
            case "ReloadComplete":
                // Note: Actual state change happens in HandleReloadCompleted
                // This is just for animation-driven reload timing
                break;
        }
    }
    
    #endregion
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(float amount);
}