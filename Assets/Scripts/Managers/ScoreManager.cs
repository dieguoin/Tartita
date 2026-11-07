using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("Score Configuration")]
    [Tooltip("Multiplier from Unity world units of height to score points (1 world unit = 10 points).")]
    [SerializeField] private float scoreMultiplier = 10f;

    private int currentScore = 0;
    private int highScore = 0;
    private bool isNewHighScore = false;

    private float maxAltitudeReached = 0f;
    private float startPlayerY = -2.77f;
    private bool isTracking = false;
    private int nextMilestone = 100;
    private bool hasShownNewBestPopupThisRun = false;
    private bool hasShownTier1 = false;
    private bool hasShownTier2 = false;
    private bool hasShownTier3 = false;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;
    public bool IsNewHighScore => isNewHighScore;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            LoadHighScore();
            return;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void LoadHighScore()
    {
        if (SaveManager.instance != null && SaveManager.instance.CurrentSave != null)
        {
            highScore = SaveManager.instance.CurrentSave.highScore;
        }
    }

    public void SetHighScore(int value)
    {
        highScore = Mathf.Max(0, value);
    }

    public void ResetScore(float initialPlayerY = -2.77f)
    {
        currentScore = 0;
        maxAltitudeReached = 0f;
        startPlayerY = initialPlayerY;
        isNewHighScore = false;
        isTracking = true;
        nextMilestone = 100;
        hasShownNewBestPopupThisRun = false;
        hasShownTier1 = false;
        hasShownTier2 = false;
        hasShownTier3 = false;
        LoadHighScore();
    }

    public void ResumeTracking()
    {
        isTracking = true;
    }

    /// <summary>
    /// Updates climber altitude directly based on Tartita's highest reached world Y coordinate.
    /// </summary>
    public void UpdateAltitude(float playerY, float platformScrollSpeed = 0f, float deltaTime = 0f)
    {
        if (!isTracking) return;

        float currentAltitude = Mathf.Max(0f, playerY - startPlayerY);

        if (currentAltitude > maxAltitudeReached)
        {
            maxAltitudeReached = currentAltitude;
        }

        currentScore = Mathf.Max(0, Mathf.FloorToInt(maxAltitudeReached * scoreMultiplier));

        Vector3 popupPos = new Vector3(0f, playerY + 0.65f, 0f);

        // Check atmospheric weather milestones
        if (!hasShownTier1 && currentScore >= 250)
        {
            hasShownTier1 = true;
            if (VFXManager.instance != null)
            {
                Color topMint = new Color(0.6f, 1f, 0.95f, 1f);
                Color botSky = new Color(0.2f, 0.75f, 1f, 1f);
                VFXManager.instance.SpawnFloatingText("SUGAR DUSTING!", popupPos, topMint, botSky, 1.25f);
            }
        }
        else if (!hasShownTier2 && currentScore >= 750)
        {
            hasShownTier2 = true;
            if (VFXManager.instance != null)
            {
                Color topLavender = new Color(0.85f, 0.85f, 1f, 1f);
                Color botIce = new Color(0.45f, 0.65f, 1f, 1f);
                VFXManager.instance.SpawnFloatingText("FROSTED PEAKS!", popupPos, topLavender, botIce, 1.35f);
            }
        }
        else if (!hasShownTier3 && currentScore >= 1500)
        {
            hasShownTier3 = true;
            if (VFXManager.instance != null)
            {
                Color topFrost = new Color(1f, 1f, 1f, 1f);
                Color botGold = new Color(1f, 0.8f, 0.2f, 1f);
                VFXManager.instance.SpawnFloatingText("SUGAR BLIZZARD!", popupPos, topFrost, botGold, 1.45f);
            }
        }

        if (highScore > 0 && !hasShownNewBestPopupThisRun && currentScore > highScore)
        {
            hasShownNewBestPopupThisRun = true;
            if (VFXManager.instance != null)
            {
                VFXManager.instance.PlayNewBestPopup(popupPos);
            }
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayNewBest();
            }
            while (nextMilestone <= currentScore)
            {
                nextMilestone += 100;
            }
        }
        else if (currentScore >= nextMilestone)
        {
            if (VFXManager.instance != null)
            {
                VFXManager.instance.PlayMilestonePopup(popupPos, nextMilestone);
            }
            while (nextMilestone <= currentScore)
            {
                nextMilestone += 100;
            }
        }
    }

    public bool CheckAndSaveHighScore()
    {
        isTracking = false;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            isNewHighScore = true;
            if (SaveManager.instance != null)
            {
                SaveManager.instance.Save();
            }
        }
        else
        {
            isNewHighScore = false;
        }

        return isNewHighScore;
    }
}
