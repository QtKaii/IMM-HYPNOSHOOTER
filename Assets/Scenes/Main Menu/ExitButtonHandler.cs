using UnityEngine;
using UnityEngine.UI;

public class ExitButtonHandler : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(QuitGame);
            Debug.Log("[ExitButtonHandler] Initialized successfully");
        }
        else
        {
            Debug.LogError("[ExitButtonHandler] No Button component found!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("[ExitButtonHandler] Quitting game!");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(QuitGame);
        }
    }
}
