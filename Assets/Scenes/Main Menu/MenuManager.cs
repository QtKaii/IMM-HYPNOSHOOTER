using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    private StartButtonHandler startButton;
    private ExitButtonHandler exitButton;
    private BeverageParticleSystem beverageSystem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MenuManager] Instance created");
        }
        else
        {
            Destroy(gameObject);
            Debug.LogWarning("[MenuManager] Removing duplicate MenuManager");
            return;
        }
    }

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Find buttons in scene
        startButton = FindObjectOfType<StartButtonHandler>();
        exitButton = FindObjectOfType<ExitButtonHandler>();
        beverageSystem = FindObjectOfType<BeverageParticleSystem>();

        if (startButton == null)
        {
            Debug.LogError("[MenuManager] StartButtonHandler not found in scene!");
        }

        if (exitButton == null)
        {
            Debug.LogError("[MenuManager] ExitButtonHandler not found in scene!");
        }

        if (beverageSystem == null)
        {
            Debug.LogError("[MenuManager] BeverageParticleSystem not found in scene!");
        }

        Debug.Log("[MenuManager] Components initialized");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
