using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("HUD UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI weakPointText;

    [Header("End Game UI References")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Scoring Settings")]
    [SerializeField] private int perfectScore = 1000;
    [SerializeField] private float comboTimeLimit = 2f;
    [SerializeField] private int weakPointBonusMultiplier = 2;

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource comboSound;
    [SerializeField] private AudioSource weakPointSound;
    [SerializeField] private AudioSource gradeRevealSound;

    private int currentScore = 0;
    private int comboCount = 0;
    private float comboTimer = 0f;
    private int targetsHit = 0;
    private int weakPointsHit = 0;
    private int totalTargets = 0;
    private int maxCombo = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ScoreManager initialized");
        }
        else
        {
            Debug.LogWarning("Duplicate ScoreManager found, destroying");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        if (weakPointText != null)
            weakPointText.gameObject.SetActive(false);

        UpdateUI();
    }

    void Update()
    {
        if (comboCount > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                Debug.Log($"Combo expired at x{comboCount}");
                ResetCombo();
            }
        }
    }

    public void AddScore(int points, bool isWeakPoint = false)
    {
        comboCount++;
        comboTimer = comboTimeLimit;
        targetsHit++;

        if (comboCount > maxCombo)
            maxCombo = comboCount;

        int multiplier = comboCount;
        if (isWeakPoint)
        {
            multiplier *= weakPointBonusMultiplier;
            weakPointsHit++;

            if (weakPointSound != null)
                weakPointSound.Play();

            if (weakPointText != null)
            {
                weakPointText.gameObject.SetActive(true);
                weakPointText.text = "WEAK POINT!";
                CancelInvoke(nameof(HideWeakPointText));
                Invoke(nameof(HideWeakPointText), 1f);
            }
        }

        int totalPoints = points * multiplier;
        currentScore += totalPoints;

        Debug.Log($"Score: +{totalPoints} (base: {points} x combo: {multiplier}) | Total: {currentScore}");

        if (comboSound != null && comboCount > 1)
        {
            comboSound.pitch = 1f + (comboCount * 0.1f);
            comboSound.Play();
        }

        UpdateUI();
    }

    void HideWeakPointText()
    {
        if (weakPointText != null)
            weakPointText.gameObject.SetActive(false);
    }

    void ResetCombo()
    {
        comboCount = 0;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {currentScore}";

        if (comboText != null)
        {
            if (comboCount > 1)
            {
                comboText.text = $"COMBO x{comboCount}!";
                comboText.color = Color.Lerp(Color.white, Color.yellow, Mathf.Min(comboCount / 10f, 1f));
            }
            else
            {
                comboText.text = "";
            }
        }
    }

    public void RegisterTarget()
    {
        totalTargets++;
        Debug.Log($"Target registered (Total: {totalTargets})");
    }

    public void ShowEndScreen()
    {
        Debug.Log("=== SHOWING END SCREEN ===");

        if (endGamePanel == null)
        {
            Debug.LogError("End Game Panel not assigned!");
            return;
        }

        endGamePanel.SetActive(true);

        string grade = CalculateGrade();
        float accuracy = totalTargets > 0 ? (float)targetsHit / totalTargets * 100f : 0f;

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {currentScore}";

        if (gradeText != null)
        {
            gradeText.text = grade;
            gradeText.color = GetGradeColor(grade);
        }

        if (statsText != null)
        {
            statsText.text = $"Targets Hit: {targetsHit}/{totalTargets}\n" +
                            $"Accuracy: {accuracy:F1}%\n" +
                            $"Weak Points: {weakPointsHit}\n" +
                            $"Best Combo: x{maxCombo}";
        }

        if (gradeRevealSound != null)
            gradeRevealSound.Play();

        Debug.Log($"End Screen - Grade: {grade}, Score: {currentScore}, Accuracy: {accuracy:F1}%");
    }

    string CalculateGrade()
    {
        float scorePercent = (float)currentScore / perfectScore;
        float accuracy = totalTargets > 0 ? (float)targetsHit / totalTargets : 0f;
        float weakPointPercent = totalTargets > 0 ? (float)weakPointsHit / totalTargets : 0f;

        Debug.Log($"Grade calc - Score%: {scorePercent:F2}, Acc: {accuracy:F2}, WP%: {weakPointPercent:F2}");

        if (scorePercent >= 1f && accuracy >= 0.95f && weakPointPercent >= 0.8f)
            return "S+";
        if (scorePercent >= 0.9f && accuracy >= 0.85f)
            return "S";
        if (scorePercent >= 0.75f && accuracy >= 0.7f)
            return "A";
        if (scorePercent >= 0.6f && accuracy >= 0.6f)
            return "B";
        if (scorePercent >= 0.4f && accuracy >= 0.4f)
            return "C";
        if (scorePercent >= 0.2f)
            return "D";
        return "F";
    }

    Color GetGradeColor(string grade)
    {
        switch (grade)
        {
            case "S+": return new Color(1f, 0.84f, 0f);
            case "S": return new Color(0.9f, 0.9f, 0.1f);
            case "A": return new Color(0.2f, 1f, 0.2f);
            case "B": return new Color(0.3f, 0.7f, 1f);
            case "C": return new Color(1f, 0.6f, 0.2f);
            case "D": return new Color(1f, 0.3f, 0.3f);
            case "F": return new Color(0.5f, 0.5f, 0.5f);
            default: return Color.white;
        }
    }

    public void ResetScore()
    {
        Debug.Log("Resetting score");
        currentScore = 0;
        comboCount = 0;
        targetsHit = 0;
        weakPointsHit = 0;
        totalTargets = 0;
        maxCombo = 0;
        UpdateUI();

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        if (weakPointText != null)
            weakPointText.gameObject.SetActive(false);
    }

    public int GetScore() => currentScore;
    public int GetTargetsHit() => targetsHit;
    public int GetTotalTargets() => totalTargets;
}