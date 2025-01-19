using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    [SerializeField] private int sceneIndex = 1; // Default to index 1 (typically the game scene)
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(LoadScene);
            Debug.Log($"[StartButtonHandler] Initialized with scene index: {sceneIndex}");
        }
        else
        {
            Debug.LogError("[StartButtonHandler] No Button component found!");
        }
    }

    public void LoadScene()
    {
        Debug.Log($"[StartButtonHandler] Loading scene at index: {sceneIndex}");
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError($"[StartButtonHandler] Invalid scene index: {sceneIndex}! Must be between 0 and {SceneManager.sceneCountInBuildSettings - 1}");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(LoadScene);
        }
    }
}
