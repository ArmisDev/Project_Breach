using TMPro;
using UnityEngine;
using System.Collections;

public class HUDUI : MonoBehaviour
{
    [Header("Ammo Display")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private RectTransform ammoFillImage; // Reference to the image to move
    
    [Header("Reload Animation")]
    [SerializeField] private RectTransform reloadIndicator; // Separate image for reload indicator
    [SerializeField] private float reloadIndicatorOffset = -90f; // How far down the reload indicator starts
    [SerializeField] private Color reloadColor = Color.yellow; // Color during reload
    
    private Coroutine reloadAnimationCoroutine;
    private Color originalAmmoColor;
    
    private void Start()
    {
        // Hide reload indicator initially
        if (reloadIndicator != null)
        {
            reloadIndicator.gameObject.SetActive(false);
        }
    }
    
    public void UpdateAmmoUI(int currentAmmo, int totalAmmo, int magSize = 0)
    {
        // Safety check for division by zero
        if (magSize <= 0) magSize = 1;
        
        // Update ammo text
        ammoText.text = currentAmmo.ToString() + "/" + totalAmmo.ToString();
        
        // Update ammo fill position based on current ammo
        if (ammoFillImage != null)
        {
            // Calculate ratio (1.0 when full, 0.0 when empty)
            float ratio = (float)currentAmmo / magSize;
            
            // Calculate Y position (0 when full, -90 when empty)
            float yPosition = Mathf.Lerp(0, -76, 1 - ratio);
            
            // Update position
            Vector3 pos = ammoFillImage.localPosition;
            pos.y = yPosition;
            ammoFillImage.localPosition = pos;
        }
    }
    
    // Call this from weapon system when reload starts
    public void StartReloadAnimation(float reloadDuration)
    {
        // Stop any existing reload animation
        if (reloadAnimationCoroutine != null)
        {
            StopCoroutine(reloadAnimationCoroutine);
        }
        
        // Start new reload animation
        reloadAnimationCoroutine = StartCoroutine(AnimateReload(reloadDuration));
    }
    
    // Call this from weapon system if reload is interrupted
    public void StopReloadAnimation()
    {
        if (reloadAnimationCoroutine != null)
        {
            StopCoroutine(reloadAnimationCoroutine);
            reloadAnimationCoroutine = null;
        }
        
        // Reset UI state
        ResetReloadUI();
    }
    
    // Reset UI elements after reload
    private void ResetReloadUI()
    {
        // Hide reload indicator
        if (reloadIndicator != null)
        {
            reloadIndicator.gameObject.SetActive(false);
        }
    }
    
    // Coroutine to animate reload progress
    private IEnumerator AnimateReload(float duration)
    {
        // Show reload indicator
        if (reloadIndicator != null)
        {
            // Set initial position (at bottom)
            Vector3 startPos = reloadIndicator.localPosition;
            startPos.y = reloadIndicatorOffset;
            reloadIndicator.localPosition = startPos;
            
            // Show the indicator
            reloadIndicator.gameObject.SetActive(true);
            
            // Animate over time
            float timer = 0f;
            while (timer < duration)
            {
                // Calculate progress
                float progress = timer / duration;
                
                // Update position - move from bottom to top
                Vector3 newPos = reloadIndicator.localPosition;
                newPos.y = Mathf.Lerp(reloadIndicatorOffset, 0f, progress);
                reloadIndicator.localPosition = newPos;
                
                // Update timer
                timer += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final position is correct
            Vector3 finalPos = reloadIndicator.localPosition;
            finalPos.y = 0f;
            reloadIndicator.localPosition = finalPos;
            
            // Small delay at end of animation for visual feedback
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // No indicator, just wait for duration
            yield return new WaitForSeconds(duration);
        }
        
        // Reset UI state
        ResetReloadUI();
        
        reloadAnimationCoroutine = null;
    }
}