using UnityEngine;
using FPS.Player;

public class FootstepSystem : MonoBehaviour
{
    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepSounds;
    
    [Header("Footstep Settings")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float stepVolume = 0.7f;
    [SerializeField] private float randomPitchRange = 0.1f;
    
    [Header("Surface Detection")]
    [SerializeField] private float footstepRaycastDistance = 1.2f;
    [SerializeField] private LayerMask groundLayer;
    
    // Internal variables
    private float footstepTimer = 0f;
    private int previousSoundIndex = -1;
    private PlayerController playerController;
    private bool isMoving = false;
    
    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        
        // Create AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // Make sound 3D
            audioSource.volume = stepVolume;
            audioSource.playOnAwake = false;
        }
    }
    
    private void Update()
    {
        // Check if player is moving on the ground
        Vector3 horizontalVelocity = new Vector3(
            playerController.playerVelocity.x, 
            0, 
            playerController.playerVelocity.z
        );
        
        isMoving = horizontalVelocity.magnitude > 0.1f && playerController.IsGrounded();
        
        if (isMoving)
        {
            // Determine step interval based on whether player is sprinting
            float currentStepInterval = playerController.sprint.IsInProgress() ? 
                runStepInterval : walkStepInterval;
            
            // Update footstep timer
            footstepTimer += Time.deltaTime;
            
            // Play footstep sound at appropriate interval
            if (footstepTimer >= currentStepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            // Reset timer when not moving
            footstepTimer = 0f;
        }
    }
    
    private void PlayFootstepSound()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            Debug.LogWarning("No footstep sounds assigned to FootstepSystem.");
            return;
        }
        
        // Get a random sound that's different from the previous one
        int newSoundIndex = GetRandomSoundIndex();
        
        // Apply slight random pitch variation for more natural sound
        audioSource.pitch = 1.0f + Random.Range(-randomPitchRange, randomPitchRange);
        
        // Play the footstep sound
        audioSource.PlayOneShot(footstepSounds[newSoundIndex], stepVolume);
        
        // Store this index as the previous sound for next time
        previousSoundIndex = newSoundIndex;
    }
    
    private int GetRandomSoundIndex()
    {
        if (footstepSounds.Length == 1)
            return 0;
        
        // Get a random index that's different from the previous one
        int newIndex;
        do
        {
            newIndex = Random.Range(0, footstepSounds.Length);
        } while (newIndex == previousSoundIndex);
        
        return newIndex;
    }
    
    // Optional: Surface detection for different footstep sounds
    private string DetectSurface()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 
            footstepRaycastDistance, groundLayer))
        {
            // You can check the hit.collider.tag or material to determine surface type
            return hit.collider.tag;
        }
        
        return "Default";
    }
}