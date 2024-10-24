using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using UnityEngine.UI;
using System.Security.Cryptography;
public class ChatbotManager : MonoBehaviour
{
    public GameObject chatbotPopup;
    public GameObject messagePrefab;  
    public Transform chatContent;     
    public InputField chatInputField; 
    public ScrollRect chatScrollView;

    public Transform chatHistoryContent;
    public GameObject historyItemPrefab;

    // for development
    private List<List<string>> chatSessions = new List<List<string>>();
    private List<string> currentSessions = new List<string>();

    void Start()
    {
        LoadChatHistory();
    }



    public void OnSendButtonClicked()
    {
        string userMessage = chatInputField.text;

        if (string.IsNullOrEmpty(userMessage))
            return;

        DisplayMessage(userMessage, true);

        chatInputField.text = "";


        // imitate ai response
        Invoke("GenerateAIResponse", 1.0f);
    }

    void DisplayMessage(string message, bool isUser)
    {
        // Instantiate a new message object
        GameObject newMessage = Instantiate(messagePrefab, chatContent);

        // Set the message text
        UnityEngine.UI.Text messageText = newMessage.GetComponentInChildren<UnityEngine.UI.Text>();
        messageText.text = message;

        if (isUser)
        {
            messageText.alignment = TextAnchor.MiddleRight;  // Right-align for user messages
            messageText.color = Color.white;  
        }
        else
        {
            messageText.alignment = TextAnchor.MiddleLeft;  // Left-align for AI messages
            messageText.color = Color.magenta; 
        }



        // Scroll to the bottom of the chat after a new message
        Canvas.ForceUpdateCanvases();
        chatScrollView.verticalNormalizedPosition = 0;
    }

    // ToDo: Add actual API call
    void GenerateAIResponse()
    {
        string aiResponse = "This is a simulated AI response!";
        DisplayMessage(aiResponse, false);
    }

    void Update()
    {
        // If the user clicks enter, send the message
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnSendButtonClicked();
        }
    }

    public void OnNewChatButtonClicked()
    {
        // Delete the messages in the display panel
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }

        currentSessions.Clear();

        LoadChatHistory();
    }

    public void OpenChatbotPopUp()
    {
        chatbotPopup.SetActive(true);
    }

    public void CloseChatbotPopUp()
    {
        chatbotPopup.gameObject.SetActive(false);

        // For testing, no need after API implementation
        if (currentSessions.Count > 0)
        {
            chatSessions.Add(new List<string>(currentSessions) );
            currentSessions.Clear();
            LoadChatHistory();
        }
    }

    void LoadChatHistory()
    {
        foreach (Transform child in chatHistoryContent)
        {
            Destroy(child.gameObject);
        }

        // Populate history panel with API call
        for (int i = 0; i < chatSessions.Count; i++)
        {
            int sessionIndex = i;
            GameObject newHistoryItem = Instantiate(historyItemPrefab, chatHistoryContent);
            UnityEngine.UI.Text historyText = newHistoryItem.GetComponentInChildren<UnityEngine.UI.Text>();
            historyText.text = "Session " + (i + 1);

            // Add click event to load the session
            Button historyButton = newHistoryItem.GetComponent<Button>();
            historyButton.onClick.AddListener(() => LoadChatSession(sessionIndex));
        }
    }

    void LoadChatSession(int sessionIndex)
    {
        // Clear current chat display
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }

        // Use API call
        List<string> selectedSession = chatSessions[sessionIndex];
        foreach (string message in selectedSession)
        {
            bool isUser = message.StartsWith("User: ");
            DisplayMessage(message.Replace("User: ", "").Replace("AI: ", ""), isUser);
        }
    }

}
