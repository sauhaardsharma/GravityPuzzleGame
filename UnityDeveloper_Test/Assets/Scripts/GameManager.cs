using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game state — timer, cube collection,
/// win condition and game over conditions.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float timeLimit = 120f; // 2 minutes
    [SerializeField] private float fallDeathTime = 3f;   // airtime before fall death

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameUIManager gameUIManager;

    // ── State ─────────────────────────────────────────────────
    private int _totalCubes;
    private int _collectedCubes;
    private float _timeRemaining;
    private bool _gameActive;

    #region Unity Callbacks

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Count all cubes in scene
        _totalCubes = FindObjectsByType<CollectibleCube>(FindObjectsSortMode.None).Length;
        _collectedCubes = 0;
        _timeRemaining = timeLimit;
        _gameActive = true;

        gameUIManager.UpdateCubeCount(_collectedCubes, _totalCubes);
        gameUIManager.UpdateTimer(_timeRemaining);
    }

    private void Update()
    {
        if (!_gameActive) return;

        UpdateTimer();
        CheckFallDeath();
    }

    #endregion

    #region Game Logic

    private void UpdateTimer()
    {
        _timeRemaining -= Time.deltaTime;
        gameUIManager.UpdateTimer(_timeRemaining);

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            TriggerGameOver("Time's Up!");
        }
    }

    private void CheckFallDeath()
    {
        if (playerController.AirTime >= fallDeathTime
            && !playerController.IsSwitchingGravity)
        {
            TriggerGameOver("You Fell!");
        }
    }

    /// <summary>Called by CollectibleCube when player collects it.</summary>
    public void OnCubeCollected()
    {
        _collectedCubes++;
        gameUIManager.UpdateCubeCount(_collectedCubes, _totalCubes);

        if (_collectedCubes >= _totalCubes)
            TriggerWin();
    }

    private void TriggerWin()
    {
        _gameActive = false;
        gameUIManager.ShowWin(_timeRemaining);
    }

    private void TriggerGameOver(string reason)
    {
        _gameActive = false;
        gameUIManager.ShowGameOver(reason);
    }

    /// <summary>Restart current scene.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion
}