using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FPS.Player;
using System;

public class PauseManager : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button exitButton;
    
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;
    
    [Header("Player References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraLook cameraLook;
    
    // Static properties for global state access
    public static bool IsGamePaused { get; private set; } = false;
    
    // Global events for input handling
    public static event Action<bool> OnGamePauseStateChanged;
    
    // Input-specific event that input handlers can subscribe to
    public static event Action OnInputDisabled;
    public static event Action OnInputEnabled;
    
    private bool isPaused = false;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        // Get references if not already set
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
            
        if (cameraLook == null)
            cameraLook = FindFirstObjectByType<CameraLook>();
            
        playerInput = playerController?.GetComponent<PlayerInput>();
        
        // Setup button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
    }
    
    private void OnEnable()
    {
        // Register for pause action
        if (pauseAction != null)
            pauseAction.action.performed += OnPauseActionPerformed;
    }
    
    private void OnDisable()
    {
        // Unregister from pause action
        if (pauseAction != null)
            pauseAction.action.performed -= OnPauseActionPerformed;
    }
    
    private void Start()
    {
        // Ensure pause menu is hidden at start
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
            
        // Initialize static state
        IsGamePaused = false;
    }
    
    private void OnPauseActionPerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }
    
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        // Set pause flags (local and static)
        isPaused = true;
        IsGamePaused = true;

        // Broadcast first that input will be disabled
        // This gives input handlers a chance to clean up before the pause
        OnInputDisabled?.Invoke();

        // Show pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        else
        {
            Debug.LogError("pauseMenuUI reference is missing! Assign it in the inspector.");
        }

        // Freeze time
        Time.timeScale = 0f;

        // Disable player input
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
        }
        else
        {
            Debug.LogWarning("playerInput is null, could not deactivate input");
        }

        // Show cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Broadcast game pause state changed event
        OnGamePauseStateChanged?.Invoke(true);
    }
    
    public void ResumeGame()
    {
        // Clear pause flags
        isPaused = false;
        IsGamePaused = false;
        
        // Hide pause menu
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        
        // Restore time
        Time.timeScale = 1f;
        
        // Re-enable player input
        if (playerInput != null)
            playerInput.ActivateInput();
        
        // Restore cursor lock state (for FPS camera)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Broadcast that input is now enabled
        OnInputEnabled?.Invoke();
        
        // Broadcast game pause state changed event
        OnGamePauseStateChanged?.Invoke(false);
    }
    
    public void ExitGame()
    {
        // First restore time scale to normal to prevent issues
        Time.timeScale = 1f;
        
        // Handle quitting differently in editor vs build
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // Public method to check pause state
    public bool IsPaused()
    {
        return isPaused;
    }
}