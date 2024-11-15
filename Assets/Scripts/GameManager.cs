using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject bulletPrefab;
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    [Header("Game State")]
    public int score = 0;
    public int round = 1;
    public float difficulty = 1f;
    private int enemiesNeededForNextRound = 10;
    private int enemiesDefeatedThisRound = 0;

    [Header("Lists")]
    public List<Enemy> enemies = new List<Enemy>();
    public PlayerHandler player;
    public EnemyManager enemyManager;

    private StatUpgradeUI statUpgradeUI;

    private GameObject gameOverPanel;

    private void Awake()
    {
       
        if (Instance == null)
        {
            Instance = this;
        
        }
        else
        {
            Destroy(gameObject);
            Debug.LogWarning("removing duplicate GameManager");
            return; // stops multiple instances
        }

        InitializePrefabs();

        // Initialize StatUpgradeUI
        InitializeStatUpgradeUI();
    }

    private void InitializePrefabs()
    {
        // Create default bullet prefab
        if (bulletPrefab == null)
        {
            throw new System.Exception("Bullet prefab is not set.");
        }
    }

    private void InitializeStatUpgradeUI()
    {
        GameObject statUpgradeUIObject = new GameObject("StatUpgradeUI");
        statUpgradeUI = statUpgradeUIObject.AddComponent<StatUpgradeUI>();
        // Set the StatUpgradeU parent to preserve the hierarchy
        statUpgradeUIObject.transform.parent = this.transform;
        Debug.Log("StatUpgradeUI initialized.");
    }

    private void Start()
    {
        SetupInitialGameState();
    }

    private void SetupInitialGameState()
    {
        // Spawn player if not already in scene
        if (player == null)
        {
            SpawnPlayer();
        }

        // Find or create EnemyManager
        if (enemyManager == null)
        {
            enemyManager = FindAnyObjectByType<EnemyManager>();
            if (enemyManager == null)
            {
                GameObject enemyManagerObject = new GameObject("EnemyManager");
                enemyManager = enemyManagerObject.AddComponent<EnemyManager>();
                Debug.Log("EnemyManager instantiated.");
            }
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            throw new System.Exception("Player prefab is not set.");
        }

        GameObject playerObject = Instantiate(playerPrefab);
        player = playerObject.GetComponent<PlayerHandler>();
        Debug.Log("Player spawned.");
    }

   

    public void AddScore(int points)
    {
        score += points;
        enemiesDefeatedThisRound++;
        
        Debug.Log($"Score updated: {score}"); // Debug log for tracking score updates

        if (enemiesDefeatedThisRound >= enemiesNeededForNextRound)
        {
            AdvanceRound();
        }
    }

    public void AdvanceRound()
    {
        round++;
        difficulty *= 1.1f; // Increase difficulty by .1
        
        // Reset enemy counter and increase requirement
        enemiesDefeatedThisRound = 0;
        enemiesNeededForNextRound = Mathf.RoundToInt(enemiesNeededForNextRound * 1.5f);
        
        // Update enemy spawning difficulty
        if (enemyManager != null)
        {
            enemyManager.SetDifficulty(round, difficulty);
            Debug.Log($"EnemyManager difficulty updated for Round {round}.");
        }

        Debug.Log($"Round advanced to {round} with difficulty {difficulty:F1}.");

        // Pause the game and show the stat upgrade UI
        PauseGame();
        statUpgradeUI.ShowUpgradeOptions();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        Debug.Log("Game Paused.");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Game Resumed.");
    }

    public void ApplyStatUpgrade(StatType selectedStat)
    {
        switch (selectedStat)
        {
            case StatType.MoveSpeed:
                player.moveSpeed += 1;
                Debug.Log($"MoveSpeed increased to {player.moveSpeed}");
                break;
            case StatType.ShootCooldown:
                player.shootCooldown = Mathf.Max(0.1f, player.shootCooldown - 0.1f);
                Debug.Log($"ShootCooldown decreased to {player.shootCooldown}");
                break;
            case StatType.Health:
                player.maxHealth += 1;
                player.currentHealth += 1;
                Debug.Log($"Health increased to {player.maxHealth}");
                break;
        }

        // Resume the game after applying the upgrade
        ResumeGame();
    }

    public void GameOver()
    {
        // Hide player and enemies
        HideAllEnemiesAndPlayer();
        Debug.Log("Player and all enemies have been hidden.");

        // Disable enemymanager to stop spawning
        if (enemyManager != null)
        {
            enemyManager.enabled = false;
            Debug.Log("EnemyManager disabled to stop enemy spawning.");
        }

        // Show Game Over UI
        DisplayGameOverUI();
        Debug.Log("Game Over UI displayed.");
    }

    private void HideAllEnemiesAndPlayer()
    {
        if (player != null)
        {
            player.gameObject.SetActive(false);
            Debug.Log("Player hidden.");
        }

        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        Debug.Log("All enemies hidden.");
    }

    private void DisplayGameOverUI()
    {
        // Check if GameOverPanel already exists to prevent duplicates
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            return;
        }

        // Create Canvas
        GameObject canvasObject = new GameObject("GameOverCanvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Create Game Over Panel
        gameOverPanel = new GameObject("GameOverPanel");
        gameOverPanel.transform.parent = canvas.transform;
        RectTransform panelRect = gameOverPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        // Add Image to Panel
        Image panelImage = gameOverPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

        // Create "Game Over" Text
        GameObject textObject = new GameObject("GameOverText");
        textObject.transform.parent = gameOverPanel.transform;
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(300, 50);
        textRect.anchorMin = new Vector2(0.5f, 0.7f);
        textRect.anchorMax = new Vector2(0.5f, 0.7f);
        textRect.anchoredPosition = Vector2.zero;
        Text gameOverText = textObject.AddComponent<Text>();
        gameOverText.text = "Game Over";
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameOverText.color = Color.white;
        gameOverText.fontSize = 36;

        // Create Restart Button
        GameObject restartButtonObject = new GameObject("RestartButton");
        restartButtonObject.transform.parent = gameOverPanel.transform;
        RectTransform restartRect = restartButtonObject.AddComponent<RectTransform>();
        restartRect.sizeDelta = new Vector2(200, 50);
        restartRect.anchorMin = new Vector2(0.5f, 0.3f);
        restartRect.anchorMax = new Vector2(0.5f, 0.3f);
        restartRect.anchoredPosition = Vector2.zero;

        Button restartButton = restartButtonObject.AddComponent<Button>();
        Image restartImage = restartButtonObject.AddComponent<Image>();
        restartImage.color = Color.white;
        restartButton.targetGraphic = restartImage;

        // Create Text for Restart Button
        GameObject restartButtonTextObject = new GameObject("Text");
        restartButtonTextObject.transform.parent = restartButtonObject.transform;
        RectTransform restartTextRect = restartButtonTextObject.AddComponent<RectTransform>();
        restartTextRect.sizeDelta = new Vector2(200, 50);
        restartTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        restartTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        restartTextRect.anchoredPosition = Vector2.zero;
        Text restartButtonText = restartButtonTextObject.AddComponent<Text>();
        restartButtonText.text = "Restart";
        restartButtonText.alignment = TextAnchor.MiddleCenter;
        restartButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        restartButtonText.color = Color.black;
        restartButtonText.fontSize = 24;

        // Add Listener to Restart Button
        restartButton.onClick.AddListener(() => RestartGame());

        Debug.Log("Game Over UI created successfully.");
    }

    private void RestartGame()
    {
        Debug.Log("Restarting the game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (player != null)
        {
            DrawPlayerUI(
                cooldownRemaining: Time.time - player.lastDashTime,
                dashCooldown: player.dashCooldown,
                currentAmmo: player.currentAmmo,
                maxAmmo: player.maxAmmo,
                isReloading: player.isReloading,
                reloadStartTime: player.reloadStartTime,
                reloadTime: player.reloadTime,
                healthRatio: player.GetHealthRatio()
            );
        }
    }

    public void DrawPlayerUI(
        float cooldownRemaining, 
        float dashCooldown, 
        int currentAmmo, 
        int maxAmmo, 
        bool isReloading, 
        float reloadStartTime, 
        float reloadTime,
        float healthRatio)
    {
        float screenHeight = Screen.height;
        float screenWidth = Screen.width;

        // Background panels 
        // Top panel background (Wave, Difficulty, Score)
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(screenWidth / 2 - 170, 5, 340, 50), Texture2D.whiteTexture);

        // Left panel background (Health)
        GUI.DrawTexture(new Rect(5, 5, 325, 35), Texture2D.whiteTexture);

        // Bottom panel background (Dash Cooldown, Ammo)
        GUI.DrawTexture(new Rect(5, screenHeight - 65, 230, 60), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screenWidth - 160, screenHeight - 45, 150, 35), Texture2D.whiteTexture);

        // Reset GUI color
        GUI.color = Color.white;

        // Top UI: Wave, Difficulty, Score with shadow effect
        GUIStyle topStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(screenWidth / 2 - 150, 15, 300, 30), 
            $"Wave: {round} | Difficulty: {difficulty:F1}", topStyle);

        GUI.Label(new Rect(screenWidth / 2 + 100, 15, 190, 30), 
            $"Score: {score}", new GUIStyle(topStyle) { alignment = TextAnchor.MiddleRight });

    
        GUI.color = Color.gray;
        GUI.DrawTexture(new Rect(9, 9, 202, 27), Texture2D.whiteTexture);
        
        // Background
        GUI.color = new Color(0.2f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(10, 10, 200, 25), Texture2D.whiteTexture);
        
        // Health bar
        GUI.color = new Color(0.8f, 0.2f, 0.2f); 
        GUI.DrawTexture(new Rect(10, 10, 200, 25), Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.8f, 0.2f);
        GUI.DrawTexture(new Rect(10, 10, 200 * healthRatio, 25), Texture2D.whiteTexture);

        // Health Text 
        GUI.color = Color.white;
        GUI.Label(new Rect(220, 10, 100, 25), 
            $"Health: {player.currentHealth}/{player.maxHealth}", 
            new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            });

        // Dash Cooldown
        GUI.Label(new Rect(20, screenHeight - 60, 150, 20), "Dash Cooldown:", 
            new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } });
        
        // Dash cooldown background
        GUI.color = new Color(0.2f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(20, screenHeight - 30, 200, 20), Texture2D.whiteTexture);
        
        // Dash cooldown bar
        float normalizedCooldown = Mathf.Clamp01(cooldownRemaining / dashCooldown);
        float dashCooldownWidth = 200 * (1 - normalizedCooldown);
        GUI.color = new Color(1f, 0.8f, 0.2f); // Slightly darker yellow
        GUI.DrawTexture(new Rect(20, screenHeight - 30, dashCooldownWidth, 20), Texture2D.whiteTexture);

        // Ammo Display with better contrast
        GUI.color = Color.white;
        string ammoText = isReloading 
            ? $"Reloading... {Mathf.CeilToInt(reloadTime - (Time.time - reloadStartTime))}s" 
            : $"Ammo: {currentAmmo}/{maxAmmo}";
        
        GUI.Label(new Rect(screenWidth - 150, screenHeight - 40, 140, 30), 
            ammoText, 
            new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            });
    }
}

// Enum for different stats that can be upgraded
public enum StatType
{
    MoveSpeed,
    ShootCooldown,
    Health
}