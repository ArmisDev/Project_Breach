using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Weapons/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Basic Information")]
    public string weaponName = "Default Weapon";
    public string weaponDescription = "A standard weapon.";
    public GameObject weaponPrefab;
    public GameObject weaponViewModelPrefab;
    
    [Header("Weapon Properties")]
    public WeaponType weaponType = WeaponType.SemiAuto;
    public WeaponCategory weaponCategory = WeaponCategory.Primary;
    
    [Header("Fire Properties")]
    public float fireRate = 0.1f; // Time between shots in seconds
    public int burstSize = 3; // For burst weapons
    public float damage = 10f;
    public float range = 100f;
    
    [Header("Ammo Properties")]
    public int magazineSize = 30;
    public int startingAmmo = 90;
    public int maxAmmo = 210;
    
    [Header("Reload Properties")]
    public float reloadTime = 2.0f;
    public bool canReloadPartially = true; // Can reload when magazine is not empty
    
    [Header("Recoil Properties")]
    public float recoilVertical = 2.0f;
    public float recoilHorizontal = 0.1f;
    public float recoilRecoverySpeed = 5.0f;
    public float maxRecoilAngle = 20.0f;
    
    [Header("Visual and Audio")]
    public AudioClip fireSound;
    public AudioClip reloadStartSound;
    public AudioClip reloadEndSound;
    public AudioClip emptySound;
    public AudioClip equipSound;
    public GameObject muzzleFlashPrefab;
    
    [Header("Animation Properties")]
    public string fireAnimationTrigger = "Fire";
    public string reloadAnimationTrigger = "Reload";
    public string equipAnimationTrigger = "Equip";
}

public enum WeaponType
{
    SemiAuto,
    FullAuto,
    Burst,
    Shotgun,
    Projectile
}

public enum WeaponCategory
{
    Primary,
    Secondary,
    Special
}