using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // The static reference to the GameManager, ensuring it's a singleton
    public static GameManager instance;

    // Game state variables
    public int playerScore = 0; // Example of a game state you want to track
    public bool isGameOver = false; // Tracks if the game is over

    private bool isPaused = false;  // Track the current pause state
    public bool gameHasStarted = false;
    [SerializeField] private GameObject pauseMenuUI;  // Optional: UI to show when the game is paused
    [SerializeField] private GameObject startMenuUI;  // The UI GameObject for the start menu

    // Called before Start, used for initialization
    void Awake()
    {
        // Singleton pattern - ensure only one GameManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Make sure this GameObject persists across scenes
        }
        else
        {
            Destroy(gameObject); // If another instance exists, destroy this one
        }
    }

    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        // To get the scene's build index
        int sceneIndex = currentScene.buildIndex;

        if (sceneIndex == 0)
        {
            // Pause the game when the start menu is shown
            StartMenu();
            // Ensure the start screen is visible and weapon select screen is hidden
            startMenuUI.SetActive(true);
            gameHasStarted = false;
        }
        else
        {
            startMenuUI.SetActive(false);
            gameHasStarted = true;
        }
    }

    private void Update()
    {
        if (gameHasStarted)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseMenu();
                }
            }
        }
    }

    public void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels!");
        }
    }

    public void ResetScene()
    {
        // Get the currently active scene and reload it
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);  // Reload the current scene using its build index
    }

    private void PauseMenu()
    {
        Time.timeScale = 0f;  // Freeze the game
        isPaused = true;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);  // Show the pause menu UI if it exists
        }

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;  // Resume the game
        isPaused = false;

        if (pauseMenuUI != null && !startMenuUI.activeSelf)
        {
            pauseMenuUI.SetActive(false);  // Hide the pause menu UI if it exists
        }
        else if(startMenuUI.activeSelf)
        {
            startMenuUI.SetActive(false);
        }

        Debug.Log("Game Resumed");
    }
    private void StartMenu()
    {
        Time.timeScale = 0f;

        if (startMenuUI != null)
        {
            startMenuUI.SetActive(true);  // Show the start menu UI
            gameHasStarted = true;
        }
    }

}
