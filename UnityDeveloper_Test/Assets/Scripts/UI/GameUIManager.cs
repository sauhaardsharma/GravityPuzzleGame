using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all in-game UI — timer, cube count,
/// game over screen, win screen.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI cubeCountText;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverReasonText;
    [SerializeField] private Button restartButton;

    [Header("Win Panel")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winTimeText;
    [SerializeField] private Button winRestartButton;

    [Header("Timer Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningTime = 30f;
    [SerializeField] private float dangerTime = 10f;

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();

        // Hide panels at start
        gameOverPanel.SetActive(false);
        winPanel.SetActive(false);

        // Button listeners
        restartButton.onClick.AddListener(() => _gameManager.RestartGame());
        winRestartButton.onClick.AddListener(() => _gameManager.RestartGame());
    }

    #region Public API

    /// <summary>Update timer display with color coding.</summary>
    public void UpdateTimer(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Color code timer
        if (timeRemaining <= dangerTime)
            timerText.color = dangerColor;
        else if (timeRemaining <= warningTime)
            timerText.color = warningColor;
        else
            timerText.color = normalColor;
    }

    /// <summary>Update cube count display.</summary>
    public void UpdateCubeCount(int collected, int total)
    {
        cubeCountText.text = $"Cubes: {collected} / {total}";
    }

    /// <summary>Show game over screen with reason.</summary>
    public void ShowGameOver(string reason)
    {
        gameOverPanel.SetActive(true);
        gameOverReasonText.text = reason;

        // Unlock cursor for button interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Show win screen with remaining time.</summary>
    public void ShowWin(float timeRemaining)
    {
        winPanel.SetActive(true);

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        winTimeText.text = $"You Won!\nTime Left: {minutes:00}:{seconds:00}";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}