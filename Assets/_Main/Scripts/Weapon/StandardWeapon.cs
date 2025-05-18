using UnityEngine;
using UnityEngine.InputSystem;
using FPS.Player;

// A concrete implementation of the abstract WeaponBase class
public class StandardWeapon : WeaponBase
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerController playerController;
    
    [Header("Visual FX")]
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private LineRenderer bulletTracer;
    
    // Component references (cached for quick access)
    private WeaponInputHandler inputHandler;
    private WeaponStateManager stateManager;
    private WeaponAmmoSystem ammoSystem;
    private WeaponRecoilSystem recoilSystem;
    private WeaponAnimationController animController;
    private WeaponAudioController audioController;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Cache component references
        inputHandler = GetComponent<WeaponInputHandler>();
        stateManager = GetComponent<WeaponStateManager>();
        ammoSystem = GetComponent<WeaponAmmoSystem>();
        recoilSystem = GetComponent<WeaponRecoilSystem>();
        animController = GetComponent<WeaponAnimationController>();
        audioController = GetComponent<WeaponAudioController>();
        
        // Initialize components if not found
        if (inputHandler == null) inputHandler = gameObject.AddComponent<WeaponInputHandler>();
        if (stateManager == null) stateManager = gameObject.AddComponent<WeaponStateManager>();
        if (ammoSystem == null) ammoSystem = gameObject.AddComponent<WeaponAmmoSystem>();
        if (recoilSystem == null) recoilSystem = gameObject.AddComponent<WeaponRecoilSystem>();
        if (animController == null) animController = gameObject.AddComponent<WeaponAnimationController>();
        if (audioController == null) audioController = gameObject.AddComponent<WeaponAudioController>();
        
        // Setup references
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(false);
        }
        
        if (bulletTracer != null)
        {
            bulletTracer.enabled = false;
        }
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Additional event subscriptions specific to this weapon type
        if (weaponEvents != null)
        {
            weaponEvents.OnWeaponFired += HandleWeaponFired;
        }
    }
    
    protected override void OnDisable()
    {
        base.OnDisable();
        
        // Cleanup additional event subscriptions
        if (weaponEvents != null)
        {
            weaponEvents.OnWeaponFired -= HandleWeaponFired;
        }
    }
    
    protected override void OnStateChanged(WeaponState oldState, WeaponState newState)
    {
        base.OnStateChanged(oldState, newState);
        
        // Specific state handling for this weapon type
        switch (newState)
        {
            case WeaponState.Firing:
                // Visual effects handled by event handler
                break;
                
            case WeaponState.Reloading:
                // Hide muzzle flash during reload
                if (muzzleFlash != null)
                {
                    muzzleFlash.SetActive(false);
                }
                break;
        }
    }
    
    // Override fire input handling for weapon-specific behavior
    protected override void HandleFireInput(WeaponBase weapon, bool isPressed)
    {
        base.HandleFireInput(weapon, isPressed);
        
        // Any additional weapon-specific logic here
    }
    
    protected override void HandleReloadInput(WeaponBase weapon)
    {
        base.HandleReloadInput(weapon);
        
        // Any additional weapon-specific reload logic here
    }
    
    // Show the bullet tracer effect
    private void ShowBulletTracer(Vector3 hitPoint)
    {
        if (bulletTracer == null) return;
        
        // Get firing position and direction
        Transform firePoint = transform.Find("FirePoint");
        if (firePoint == null) return;
        
        // Enable and position the tracer
        bulletTracer.enabled = true;
        bulletTracer.SetPosition(0, firePoint.position);
        bulletTracer.SetPosition(1, hitPoint);
        
        // Hide tracer after a short delay
        Invoke(nameof(HideBulletTracer), 0.05f);
    }
    
    private void HideBulletTracer()
    {
        if (bulletTracer != null)
        {
            bulletTracer.enabled = false;
        }
    }
    
    // Spawn impact effect at hit point
    private void SpawnImpactEffect(RaycastHit hit)
    {
        if (impactEffectPrefab == null) return;
        
        // Create impact effect at hit point
        GameObject impact = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        
        // Set parent to hit object if possible
        if (hit.transform != null)
        {
            impact.transform.SetParent(hit.transform);
        }
        
        // Auto-destroy after a while
        Destroy(impact, 2f);
    }
    
    #region Event Handlers
    
    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != this) return;
        
        // Add any weapon-specific firing effects here
        // Show muzzle flash
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            Invoke(nameof(HideMuzzleFlash), 0.05f);
        }
        
        // Perform additional visual effects like bullet tracers
        // For demo, just assume we're firing forward from the camera
        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                ShowBulletTracer(hit.point);
                SpawnImpactEffect(hit);
            }
            else
            {
                // No hit, show tracer going into the distance
                ShowBulletTracer(ray.origin + ray.direction * 100f);
            }
        }
    }
    
    private void HideMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(false);
        }
    }
    
    #endregion
}