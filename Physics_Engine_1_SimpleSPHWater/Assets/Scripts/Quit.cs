using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Quit : MonoBehaviour
{
    public Button btn;

    private void Start()
    {
        btn.onClick.AddListener(OnStartGame);
        btn.onClick.AddListener(OnExitGame);
    }

    public void OnStartGame()
    {
        SceneManager.LoadScene(1);
    }
    public void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}