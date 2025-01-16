using UnityEngine;
using UnityEngine.SceneManagement;

public class SkipButton : MonoBehaviour
{
    public string sceneName = "NewMainMenu";

    public void OnSkipButtonClicked()
    {
        SceneManager.LoadScene(sceneName);
    }
}