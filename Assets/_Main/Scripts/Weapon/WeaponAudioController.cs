using UnityEngine;

// Component for handling weapon audio
public class WeaponAudioController : MonoBehaviour, IWeaponComponent
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource primaryAudioSource;
    [SerializeField] private AudioSource secondaryAudioSource; // For layered sounds
    
    [Header("Custom Audio (overrides weapon data)")]
    [SerializeField] private bool useCustomAudio = false;
    [SerializeField] private AudioClip customFireSound;
    [SerializeField] private AudioClip customReloadStartSound;
    [SerializeField] private AudioClip customReloadEndSound;
    [SerializeField] private AudioClip customEmptySound;
    [SerializeField] private AudioClip customEquipSound;
    
    [Header("Additional Audio")]
    [SerializeField] private AudioClip clipInSound;
    [SerializeField] private AudioClip clipOutSound;
    [SerializeField] private AudioClip slideBackSound;
    [SerializeField] private AudioClip slidePullSound;
    [SerializeField] private AudioClip slideReleaseSound;
    
    // Internal state
    private WeaponBase weapon;
    private WeaponDataSO weaponData;
    private WeaponEventsSO events;
    
    public void Initialize(WeaponBase weaponBase)
    {
        weapon = weaponBase;
        weaponData = weapon.WeaponData;
        events = weapon.WeaponEvents;
        
        // Create audio sources if not assigned
        if (primaryAudioSource == null)
        {
            primaryAudioSource = gameObject.AddComponent<AudioSource>();
            primaryAudioSource.spatialBlend = 1.0f; // 3D sound
            primaryAudioSource.volume = 0.8f;
        }
        
        if (secondaryAudioSource == null)
        {
            secondaryAudioSource = gameObject.AddComponent<AudioSource>();
            secondaryAudioSource.spatialBlend = 1.0f; // 3D sound
            secondaryAudioSource.volume = 0.6f;
        }
        
        // Subscribe to events
        events.OnWeaponFired += HandleWeaponFired;
        events.OnWeaponFailedToFire += HandleWeaponFailedToFire;
        events.OnReloadStarted += HandleReloadStarted;
        events.OnReloadCompleted += HandleReloadCompleted;
        events.OnWeaponEquipped += HandleWeaponEquipped;
        events.OnAnimationEvent += HandleAnimationEvent;
    }
    
    public void Tick()
    {
        // No per-frame updates needed
    }
    
    public void OnWeaponEquipped()
    {
        // PlayEquipSound();
    }
    
    public void OnWeaponUnequipped()
    {
        // Nothing specific to do
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (events != null)
        {
            events.OnWeaponFired -= HandleWeaponFired;
            events.OnWeaponFailedToFire -= HandleWeaponFailedToFire;
            events.OnReloadStarted -= HandleReloadStarted;
            events.OnReloadCompleted -= HandleReloadCompleted;
            events.OnWeaponEquipped -= HandleWeaponEquipped;
            events.OnAnimationEvent -= HandleAnimationEvent;
        }
    }
    
    // Play fire sound
    public void PlayFireSound()
    {
        AudioClip fireClip = GetFireSound();
        if (fireClip != null && primaryAudioSource != null)
        {
            primaryAudioSource.pitch = Random.Range(0.95f, 1.05f); // Add slight pitch variation
            primaryAudioSource.PlayOneShot(fireClip);
        }
    }
    
    // Play empty magazine sound
    public void PlayEmptySound()
    {
        AudioClip emptyClip = GetEmptySound();
        if (emptyClip != null && primaryAudioSource != null)
        {
            primaryAudioSource.PlayOneShot(emptyClip);
        }
    }
    
    // Play equip sound
    public void PlayEquipSound()
    {
        AudioClip equipClip = useCustomAudio && customEquipSound != null ? 
            customEquipSound : weaponData != null ? weaponData.equipSound : null;
        
        if (equipClip != null && primaryAudioSource != null)
        {
            primaryAudioSource.PlayOneShot(equipClip);
        }
    }
    
    // Play reload start sound
    public void PlayReloadStartSound()
    {
        AudioClip reloadStartClip = GetReloadStartSound();
        if (reloadStartClip != null && primaryAudioSource != null)
        {
            primaryAudioSource.PlayOneShot(reloadStartClip);
        }
    }
    
    // Play reload end sound
    public void PlayReloadEndSound()
    {
        AudioClip reloadEndClip = GetReloadEndSound();
        if (reloadEndClip != null && primaryAudioSource != null)
        {
            primaryAudioSource.PlayOneShot(reloadEndClip);
        }
    }
    
    // Methods for playing specific mechanical sounds
    public void PlayClipOut()
    {
        if (clipOutSound != null && secondaryAudioSource != null)
        {
            secondaryAudioSource.PlayOneShot(clipOutSound);
        }
    }
    
    public void PlayClipIn()
    {
        if (clipInSound != null && secondaryAudioSource != null)
        {
            secondaryAudioSource.PlayOneShot(clipInSound);
        }
    }
    
    public void PlaySlideBack()
    {
        if (slideBackSound != null && secondaryAudioSource != null)
        {
            secondaryAudioSource.PlayOneShot(slideBackSound);
        }
    }
    
    public void PlaySlidePull()
    {
        if (slidePullSound != null && secondaryAudioSource != null)
        {
            secondaryAudioSource.PlayOneShot(slidePullSound);
        }
    }
    
    public void PlaySlideRelease()
    {
        if (slideReleaseSound != null && secondaryAudioSource != null)
        {
            secondaryAudioSource.PlayOneShot(slideReleaseSound);
        }
    }
    
    #region Utility Methods
    
    private AudioClip GetFireSound()
    {
        return useCustomAudio && customFireSound != null ? 
            customFireSound : weaponData != null ? weaponData.fireSound : null;
    }
    
    private AudioClip GetEmptySound()
    {
        return useCustomAudio && customEmptySound != null ? 
            customEmptySound : weaponData != null ? weaponData.emptySound : null;
    }
    
    private AudioClip GetReloadStartSound()
    {
        return useCustomAudio && customReloadStartSound != null ? 
            customReloadStartSound : weaponData != null ? weaponData.reloadStartSound : null;
    }
    
    private AudioClip GetReloadEndSound()
    {
        return useCustomAudio && customReloadEndSound != null ? 
            customReloadEndSound : weaponData != null ? weaponData.reloadEndSound : null;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleWeaponFired(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayFireSound();
    }
    
    private void HandleWeaponFailedToFire(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayEmptySound();
    }
    
    private void HandleReloadStarted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayReloadStartSound();
    }
    
    private void HandleReloadCompleted(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayReloadEndSound();
    }
    
    private void HandleWeaponEquipped(WeaponBase targetWeapon)
    {
        if (targetWeapon != weapon) return;
        PlayEquipSound();
    }
    
    private void HandleAnimationEvent(WeaponBase targetWeapon, string eventName)
    {
        if (targetWeapon != weapon) return;
        
        // Handle animation events for audio
        switch (eventName)
        {
            case "ClipOut":
                PlayClipOut();
                break;
            case "ClipIn":
                PlayClipIn();
                break;
            case "SlideBack":
                PlaySlideBack();
                break;
            case "SlidePull":
                PlaySlidePull();
                break;
            case "SlideRelease":
                PlaySlideRelease();
                break;
        }
    }
    
    #endregion
}