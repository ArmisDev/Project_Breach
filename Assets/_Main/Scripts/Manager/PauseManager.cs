using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FPS.Player;

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
    
    private bool isPaused = false;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        // Get references if not already set
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
            
        if (cameraLook == null)
            cameraLook = FindObjectOfType<CameraLook>();
            
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
        Debug.Log("PauseGame() called!");

        // Set pause flag
        isPaused = true;

        // Check if pauseMenuUI is assigned
        Debug.Log("pauseMenuUI is " + (pauseMenuUI == null ? "NULL" : "assigned"));

        // Show pause menu
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            Debug.Log("Pause menu activated");
        }
        else
        {
            Debug.LogError("pauseMenuUI reference is missing! Assign it in the inspector.");
        }

        // Freeze time
        Time.timeScale = 0f;
        Debug.Log("Time.timeScale set to 0");

        // Disable player input
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
            Debug.Log("Player input deactivated");
        }
        else
        {
            Debug.LogWarning("playerInput is null, could not deactivate input");
        }

        // Show cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Cursor unlocked and visible");
    }
    
    public void ResumeGame()
    {
        // Clear pause flag
        isPaused = false;
        
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
    
    // Update check for escape key as a fallback method
    // private void Update()
    // {
    //     // This is a backup in case the Input System action isn't set up
    //     if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
    //         TogglePause();
    // }
}