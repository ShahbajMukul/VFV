using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class ConnectionErrorManager : MonoBehaviour
{
    public GameObject ConnectionErrorPanel;
    public GameObject RegisterPanel;
    public GameObject LoginPanel;
    public UnityEngine.UI.Text ConnectionErrorStatusLabel;
    public Button ChatbotButton;

    void Start()
    {
        StartCoroutine(CheckConnectionToServer());
    }

    IEnumerator CheckConnectionToServer()
    {
        // This will result in 'loggedIn: false' but we are just checking if we can connect to the server. so it doesn't matter
        using (UnityWebRequest webRequest = UnityWebRequest.Get("http://localhost:3000/api/check-session"))
        {
            // Timeout 3 seconds
            webRequest.timeout = 3;

            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                UnityEngine.Debug.Log($"Error connecting to the server! {webRequest.error}");
                ShowConnectionErrorPanel(webRequest.error);
            }
            else
            {
                UnityEngine.Debug.Log("Connection to the server is successful!");
                if (ConnectionErrorPanel != null)
                {
                    ConnectionErrorPanel.SetActive(false);
                }
            }
        }
    }

    private void ShowConnectionErrorPanel(string errorMsg)
    {
        // Set the error message in the UI Text
        if (ConnectionErrorStatusLabel != null)
        {
            ConnectionErrorStatusLabel.text = errorMsg ?? "Undefined Error";
        }

        // Show the error panel
        if (ConnectionErrorPanel != null)
        {
            ConnectionErrorPanel.SetActive(true);
        }
    }

    public void OnRetryButtonClicked()
    {
        StartCoroutine(CheckConnectionToServer());
    }

    public void OnCtnuWOInternetButtonClicked()
    {
        ConnectionErrorPanel?.SetActive(false);
        RegisterPanel?.SetActive(false);
        LoginPanel?.SetActive(false);

        if (ChatbotButton != null)
        {
            ChatbotButton.interactable = false;  // Disable the chatbot button
        }
    }
}
