using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

public class StatUpgradeUI : MonoBehaviour
{
    private GameManager gameManager;

    // UI Elements
    private Canvas canvas;
    private GameObject upgradePanel;
    private GameObject statButtonPrefab;
    private Transform buttonContainer;

    private void Start()
    {
        gameManager = GameManager.Instance;
        CreateUI();
        toggleUpgradePanel(false);
    }

    private void CreateUI()
    {
        // Ensure an EventSystem exists
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("EventSystem created dynamically.");
        }

        // Create Canvas
        GameObject canvasObject = new GameObject("StatUpgradeCanvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Create Upgrade Panel
        upgradePanel = new GameObject("UpgradePanel");
        upgradePanel.transform.parent = canvas.transform;
        RectTransform panelRect = upgradePanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        // Add Image to Panel
        Image panelImage = upgradePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

        // Create Button Container
        GameObject container = new GameObject("ButtonContainer");
        container.transform.parent = upgradePanel.transform;
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(350, 200);
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;

        buttonContainer = container.transform;

        // Create Button Prefab
        statButtonPrefab = CreateButtonPrefab();

        // Create Close Button
        CreateCloseButton();
    }

    private GameObject CreateButtonPrefab()
    {
        GameObject buttonObject = new GameObject("StatButton");
        buttonObject.AddComponent<RectTransform>();
        Button button = buttonObject.AddComponent<Button>();

        // Add Image to Button
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = Color.white;
        button.targetGraphic = buttonImage;

        // Add Text to Button
        GameObject textObject = new GameObject("Text");
        textObject.transform.parent = buttonObject.transform;
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(160, 30);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        Text buttonText = textObject.AddComponent<Text>();
        buttonText.text = "Stat";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.color = Color.black;

        Debug.Log("StatButtonPrefab created successfully.");
        return buttonObject;
    }

    private void CreateCloseButton()
    {
        GameObject closeButtonObject = new GameObject("CloseButton");
        closeButtonObject.transform.parent = upgradePanel.transform;
        RectTransform closeRect = closeButtonObject.AddComponent<RectTransform>();
        closeRect.sizeDelta = new Vector2(100, 40);
        closeRect.anchorMin = new Vector2(0.5f, -0.5f);
        closeRect.anchorMax = new Vector2(0.5f, -0.5f);
        closeRect.anchoredPosition = new Vector2(0, -120);

        Button closeButton = closeButtonObject.AddComponent<Button>();
        Image closeImage = closeButtonObject.AddComponent<Image>();
        closeImage.color = Color.gray;
        closeButton.targetGraphic = closeImage; 

        // Add Text to Close Button
        GameObject textObject = new GameObject("Text");
        textObject.transform.parent = closeButtonObject.transform;
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(100, 40);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        Text buttonText = textObject.AddComponent<Text>();
        buttonText.text = "Close";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.color = Color.black;

        // Add Listener to Close Button
        closeButton.onClick.AddListener(() => toggleUpgradePanel(false));
        Debug.Log("CloseButton created successfully.");
    }

    public void ShowUpgradeOptions()
    {
        toggleUpgradePanel(true);
        GenerateUpgradeButtons();
        Debug.Log("Upgrade options displayed.");
    }

    private void toggleUpgradePanel(bool isActive)
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(isActive);
            Debug.Log($"Upgrade panel toggled to {(isActive ? "active" : "inactive")}.");
        }
        else
        {
            Debug.LogError("upgradePanel is null when attempting to toggle.");
        }
    }

    private void GenerateUpgradeButtons()
    {
        // Clear existing buttons except Close Button
        foreach (Transform child in buttonContainer)
        {
            if (child.name != "CloseButton")
            {
                Destroy(child.gameObject);
            }
        }

        // Get random stats to offer
        StatType[] availableStats = { StatType.MoveSpeed, StatType.ShootCooldown, StatType.Health };
        StatType[] choices = GetRandomStats(availableStats, 3);

        foreach (var stat in choices)
        {
            if (statButtonPrefab == null)
            {
                Debug.LogError("statButtonPrefab is null. Cannot instantiate button.");
                continue;
            }

            GameObject newButtonObject = Instantiate(statButtonPrefab, buttonContainer);
            Button newButton = newButtonObject.GetComponent<Button>();

            if (newButton != null)
            {
                Text buttonText = newButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = stat.ToString();
                }

                newButton.onClick.AddListener(() => OnStatSelected(stat));
                Debug.Log($"Button for {stat} created.");
            }
            else
            {
                Debug.LogError("Failed to get Button component from instantiated prefab.");
            }
        }
    }

    private StatType[] GetRandomStats(StatType[] stats, int count)
    {
        // Shuffle and take 'count' stats
        for (int i = 0; i < stats.Length; i++)
        {
            StatType temp = stats[i];
            int randomIndex = Random.Range(i, stats.Length);
            stats[i] = stats[randomIndex];
            stats[randomIndex] = temp;
        }
        return stats.Take(count).ToArray();
    }

    private void OnStatSelected(StatType selectedStat)
    {
        Debug.Log($"Stat selected: {selectedStat}");
        gameManager.ApplyStatUpgrade(selectedStat);
        toggleUpgradePanel(false);
    }
} 