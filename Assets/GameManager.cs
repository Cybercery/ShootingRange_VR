using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 90f;
    [SerializeField] private bool autoStart = true;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game References")]
    [SerializeField] private TargetSpawner spawner;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource gameStartSound;
    [SerializeField] private AudioSource gameEndSound;
    [SerializeField] private AudioSource countdownBeepSound;

    private float currentTime;
    private bool gameActive = false;
    private bool gameStarted = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (startPanel != null)
            startPanel.SetActive(!autoStart);

        if (spawner == null)
        {
            Debug.LogError("TargetSpawner not assigned to GameManager!");
        }

        if (autoStart)
        {
            Debug.Log("Auto-starting game");
            StartCoroutine(StartGameSequence());
        }
    }

    void Update()
    {
        if (gameActive)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }

            UpdateTimerUI();

            // Warning beeps for last 10 seconds
            if (currentTime <= 10f && currentTime > 9f)
            {
                int currentSecond = Mathf.CeilToInt(currentTime);
                int lastSecond = Mathf.CeilToInt(currentTime + Time.deltaTime);

                if (currentSecond != lastSecond && countdownBeepSound != null)
                {
                    countdownBeepSound.Play();
                }
            }
        }
    }

    public void StartGame()
    {
        if (gameStarted) return;
        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        gameStarted = true;

        if (startPanel != null)
            startPanel.SetActive(false);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            for (int i = 3; i > 0; i--)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 120;

                if (countdownBeepSound != null)
                    countdownBeepSound.Play();

                yield return new WaitForSeconds(1f);
            }

            countdownText.text = "GO!";
            countdownText.fontSize = 150;

            if (gameStartSound != null)
                gameStartSound.Play();

            yield return new WaitForSeconds(0.5f);

            countdownText.gameObject.SetActive(false);
        }

        currentTime = gameDuration;
        gameActive = true;

        Debug.Log($"Game started! Duration: {gameDuration} seconds");

        if (spawner != null)
        {
            spawner.StartSpawning();
        }
        else
        {
            Debug.LogError("Cannot start game - TargetSpawner is null!");
        }
    }

    void EndGame()
    {
        if (!gameActive) return;

        gameActive = false;

        Debug.Log("Game ended!");

        if (spawner != null)
            spawner.StopSpawning();

        if (gameEndSound != null)
            gameEndSound.Play();

        StartCoroutine(ShowEndScreenDelayed());
    }

    IEnumerator ShowEndScreenDelayed()
    {
        yield return new WaitForSeconds(1.5f);

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ShowEndScreen();
        }
        else
        {
            Debug.LogError("ScoreManager not found!");
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            int milliseconds = Mathf.FloorToInt((currentTime * 100f) % 100f);

            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);

            if (currentTime <= 10f)
            {
                float lerpValue = 1f - (currentTime / 10f);
                timerText.color = Color.Lerp(Color.white, Color.red, lerpValue);
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    public void RestartGame()
    {
        Debug.Log("Restarting game");

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetScore();

        Target[] targets = FindObjectsOfType<Target>();
        foreach (Target t in targets)
            Destroy(t.gameObject);

        gameStarted = false;
        gameActive = false;

        StartCoroutine(StartGameSequence());
    }

    public bool IsGameActive() => gameActive;
    public float GetRemainingTime() => currentTime;
}