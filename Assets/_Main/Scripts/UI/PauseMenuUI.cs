using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float scaleInDuration = 0.3f;
    
    private PauseManager pauseManager;
    
    private void Awake()
    {
        pauseManager = FindFirstObjectByType<PauseManager>();
        
        // Setup button events
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
    }
    
    private void OnEnable()
    {
        // Animate the menu when it becomes visible
        AnimateMenuOpen();
    }
    
    private void OnDisable()
    {
        // Reset menu state for next time
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
            
        if (menuPanel != null)
            menuPanel.localScale = Vector3.one * 0.8f;
    }
    
    private void AnimateMenuOpen()
    {
        // Simple animation effect using lerp
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
            
        if (menuPanel != null)
            menuPanel.localScale = Vector3.one * 0.8f;
            
        // Start the animation coroutine
        StartCoroutine(AnimateMenu());
    }
    
    private System.Collections.IEnumerator AnimateMenu()
    {
        float startTime = Time.unscaledTime;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime = Time.unscaledTime - startTime;
            float t = elapsedTime / fadeInDuration;
            
            // Animate opacity
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                
            // Animate scale
            if (menuPanel != null)
                menuPanel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                
            yield return null;
        }
        
        // Ensure final state
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
            
        if (menuPanel != null)
            menuPanel.localScale = Vector3.one;
    }
    
    private void OnResumeClicked()
    {
        if (pauseManager != null)
            pauseManager.ResumeGame();
    }
    
    private void OnExitClicked()
    {
        if (pauseManager != null)
            pauseManager.ExitGame();
    }
}