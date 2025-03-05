using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class ChatbotHandler : MonoBehaviour
{
    public GameObject chatbotPopupPanel;
    public GameObject AskChatbotButton;
    public GameObject TextWarning;

    private string sessionFilePath;

    void Start()
    {

        // Path for saving session token
        sessionFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "sessionToken.txt");

        // Load the session token from file
        if (File.Exists(sessionFilePath))
        {
            string sessionToken = File.ReadAllText(sessionFilePath);
            if (string.IsNullOrEmpty(sessionToken) == false)
            {
                SessionData data = JsonUtility.FromJson<SessionData>(sessionToken);
                if (data != null && !data.isActive)
                {
                    Debug.Log("User found but not active -> Hide chatbot button");
                    HideAskChatbotButton();
                    return;
                }
                Debug.Log("Stored session token found. Skipping login...");

            }
            else
            {
                if (chatbotPopupPanel.activeSelf)
                {
                    TextWarning.SetActive(true);
                }
                else
                {
                    TextWarning.SetActive(false);
                }
            }
        }

        string loggedInUsername = PlayerPrefs.GetString("LoggedInUsername", string.Empty);
        // hide the button if the user is not logged in
        if (!string.IsNullOrEmpty(loggedInUsername))
        {
            ShowAskChatbotButton();
        }
        else
        {
            HideAskChatbotButton();
        }
    }


    public void AskChatbotButtonClicked()
    {
        if (chatbotPopupPanel != null)
            chatbotPopupPanel.SetActive(true);
    }

    [System.Serializable]
    private class SessionData
    {
        public string sessionToken;
        public bool isActive;
    }


    public void HideAskChatbotButton()
    {
        if(AskChatbotButton != null)
            AskChatbotButton.gameObject.SetActive(false);
    }

    public void ShowAskChatbotButton()
    {
        if (AskChatbotButton != null)
        {
            AskChatbotButton.gameObject.SetActive(true);
        }
    }
}
