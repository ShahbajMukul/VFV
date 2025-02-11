using UnityEngine;
using UnityEngine.SceneManagement;

public class SweetSpot : MonoBehaviour
{
    public void OpenStorAI()
    {
        Application.OpenURL("https://storai.net/dashboard");
        Time.timeScale = 0;
    }

    public void ShowSweetSpotUI()
    {
        gameObject.SetActive(true); 
        Time.timeScale = 0; 
    }

    public void HideSweetSpotUI()
    {
        transform.parent.gameObject.SetActive(false); 

    }
}