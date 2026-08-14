using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load when Play is pressed, as it appears in Build Settings")]
    public string GameplaySceneName = "HubLevel";

    public void PlayGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit requested");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
