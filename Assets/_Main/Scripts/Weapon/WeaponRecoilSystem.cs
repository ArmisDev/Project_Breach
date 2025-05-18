using UnityEngine;

// Component for handling weapon recoil effects
public class WeaponRecoilSystem : MonoBehaviour, IWeaponComponent
{
    [Header("References")]
    [SerializeField] private Transform recoilTransform; // Camera or weapon transform to apply recoil
    [SerializeField] private Transform weaponCameraTransform;
    
    [Header("Custom Tuning (overrides weapon data)")]
    [SerializeField] private bool useCustomSettings = false;
    [SerializeField] private float recoilVertical = 2f;
    [SerializeField] private float recoilHorizontal = 0.1f;
    [SerializeField] private float recoilRecoverySpeed = 5f;
    [SerializeField] private float maxRecoilAngle = 20f;
    
    // Internal state
    private WeaponBase weapon;
    private WeaponDataSO weaponData;
    private WeaponEventsSO events;
    
    private Vector3 currentRecoil;
    private Quaternion originalRotation;
    private Vector3 firstShotPosition; // Reference position for recoil effect
    private bool hasRecoil = false;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        weaponData = weapon.WeaponData;
        events = weapon.WeaponEvents;
        
        // Store original rotation
        if (recoilTransform == null)
        {
            recoilTransform = weaponCameraTransform;
        }
        originalRotation = recoilTransform.localRotation;
        
        // Subscribe to events
        events.OnWeaponFired += HandleWeaponFired;
        events.OnWeaponEquipped += HandleWeaponEquipped;
    }
    
    public void Tick()
    {
        // Handle recoil recovery
        HandleRecoilRecovery();
    }
    
    public void OnWeaponEquipped()
    {
        // Reset recoil on equip
        currentRecoil = Vector3.zero;
        hasRecoil = false;
        ApplyRecoil();
    }
    
    public void OnWeaponUnequipped()
    {
        // Reset transform on unequip
        if (recoilTransform != null)
        {
            recoilTransform.localRotation = originalRotation;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (events != null)
        {
            events.OnWeaponFired -= HandleWeaponFired;
            events.OnWeaponEquipped -= HandleWeaponEquipped;
        }
    }
    
    // Calculate and apply recoil when weapon fires
    private void CalculateAndApplyRecoil()
    {
        // Store the first shot position if this is the first shot
        if (!hasRecoil)
        {
            firstShotPosition = recoilTransform.position;
            hasRecoil = true;
        }
        
        // Get recoil values (either from custom settings or weapon data)
        float vertical = useCustomSettings ? recoilVertical : weaponData.recoilVertical;
        float horizontal = useCustomSettings ? recoilHorizontal : weaponData.recoilHorizontal;
        float maxAngle = useCustomSettings ? maxRecoilAngle : weaponData.maxRecoilAngle;
        
        // Add some randomness to horizontal recoil
        float randomHorizontal = Random.Range(-horizontal, horizontal);
        
        // Add recoil to current recoil
        currentRecoil += new Vector3(-Mathf.Abs(vertical), randomHorizontal, 0);
        
        // Clamp vertical recoil
        currentRecoil.x = Mathf.Clamp(currentRecoil.x, -maxAngle, 0);
        
        // Apply the recoil
        ApplyRecoil();
    }
    
    // Apply current recoil to transform
    private void ApplyRecoil()
    {
        if (recoilTransform != null)
        {
            recoilTransform.localRotation = originalRotation * Quaternion.Euler(currentRecoil);
        }
    }
    
    // Handle gradual recoil recovery
    private void HandleRecoilRecovery()
    {
        if (currentRecoil.magnitude > 0.01f)
        {
            // Get recovery speed (either from custom settings or weapon data)
            float recoverySpeed = useCustomSettings ? recoilRecoverySpeed : weaponData.recoilRecoverySpeed;
            
            // Gradually reduce recoil
            currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, recoverySpeed * Time.deltaTime);
            ApplyRecoil();
        }
        else if (hasRecoil)
        {
            // Reset when recoil is fully recovered
            hasRecoil = false;
            currentRecoil = Vector3.zero;
            ApplyRecoil();
        }
    }
    
    #region Event Handlers
    
    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        CalculateAndApplyRecoil();
    }
    
    private void HandleWeaponEquipped(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        originalRotation = recoilTransform.localRotation;
    }
    
    #endregion
}