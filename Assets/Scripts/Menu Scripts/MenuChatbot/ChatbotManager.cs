using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class ChatbotManager : MonoBehaviour
{
    public GameObject chatbotPopup;
    public GameObject messagePrefab;
    public Transform chatContent;
    public InputField chatInputField;
    public ScrollRect chatScrollView;

    public Transform chatHistoryContent;
    public GameObject historyItemPrefab;

    //Suggestions:
    /*our local PostgreSQL database in the backend can store the chat history for each user*/

    /*we can create new endpoints for saving and retrieving chat history in aiserver.js*/

    private List<List<string>> chatSessions = new List<List<string>>();
    private List<string> currentSession = new List<string>();

    private string aiGenerateTextUrl = "http://localhost:3002/ai/generate-text";

    //the new endpoint would be similar like this:
    //private string aiSaveChatHistoryUrl = "http://localhost:3002/ai/save-chat-history";
    //private string aiGetChatHistoryUrl = "http://localhost:3002/ai/get-chat-history";

    void Start()
    {
        //StartCoroutine(GetChatHistory());
        chatInputField.onEndEdit.AddListener(delegate { OnEnterPressed(); });
    }

    public void OnSendButtonClicked()
    {
        string userMessage = chatInputField.text;

        if (string.IsNullOrEmpty(userMessage))
            return;

        DisplayMessage(userMessage, true);
        currentSession.Add("User: " + userMessage);

        chatInputField.text = "";

        // Call the AI API to get the response
        StartCoroutine(GetAIResponse(userMessage));
    }

    IEnumerator GetAIResponse(string userMessage)
    {
        string jsonData = $"{{\"ai_type\":\"default\",\"userMessage\":\"{userMessage}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(aiGenerateTextUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP Error: " + www.error);
                DisplayMessage("Error: Unable to get AI response. Please try again.", false);
            }
            else if (www.responseCode == 200)
            {
                string jsonResponse = www.downloadHandler.text;

                AIResponse response = JsonUtility.FromJson<AIResponse>(jsonResponse);
                if (response != null && !string.IsNullOrEmpty(response.content))
                {
                    DisplayMessage(response.content, false);
                    currentSession.Add("AI: " + response.content);
                }
                else
                {
                    DisplayMessage("Error: Invalid response from AI.", false);
                }
            }
        }
    }

    /*IEnumerator GetChatHistory()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(aiGetChatHistoryUrl))
        {
            www.SetRequestHeader("Accept", "application/json");
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("Error fetching chat history: " + www.error);
            }
            else
            {
                ChatHistoryResponse response = JsonUtility.FromJson<ChatHistoryResponse>(www.downloadHandler.text);
                if (response != null && response.sessions != null)
                {
                    foreach (ChatSession session in response.sessions)
                    {
                        List<string> messages = new List<string>(session.messages);
                        chatSessions.Add(messages);
                    }
                    LoadChatHistory();
                }
            }
        }
    }*/

    public void OnNewChatButtonClicked()
    {
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }

        currentSession.Clear();
        LoadChatHistory();
    }

    public void OpenChatbotPopUp()
    {
        chatbotPopup.SetActive(true);
    }

    public void CloseChatbotPopUp()
    {
        chatbotPopup.SetActive(false);

        if (currentSession.Count > 0)
        {
            chatSessions.Add(new List<string>(currentSession));
            //StartCoroutine(SaveChatHistory(currentSession));
            currentSession.Clear();
            LoadChatHistory();
        }
    }

    /*IEnumerator SaveChatHistory(List<string> session)
    {
        string jsonData = JsonUtility.ToJson(new ChatSession { sessionId = System.Guid.NewGuid().ToString(), messages = session.ToArray() });
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(aiSaveChatHistoryUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("Error saving chat history: " + www.error);
            }
        }
    }*/

    void LoadChatHistory()
    {
        foreach (Transform child in chatHistoryContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < chatSessions.Count; i++)
        {
            int sessionIndex = i;
            GameObject newHistoryItem = Instantiate(historyItemPrefab, chatHistoryContent);
            UnityEngine.UI.Text historyText = newHistoryItem.GetComponentInChildren<UnityEngine.UI.Text>();
            historyText.text = "Session " + (i + 1);

            Button historyButton = newHistoryItem.GetComponent<Button>();
            historyButton.onClick.AddListener(() => LoadChatSession(sessionIndex));
        }
    }

    void LoadChatSession(int sessionIndex)
    {
        foreach (Transform child in chatContent)
        {
            Destroy(child.gameObject);
        }

        List<string> selectedSession = chatSessions[sessionIndex];
        foreach (string message in selectedSession)
        {
            bool isUser = message.StartsWith("User: ");
            DisplayMessage(message.Replace("User: ", "").Replace("AI: ", ""), isUser);
        }
    }

    [System.Serializable]
    public class AIResponse
    {
        public string content;
    }

    [System.Serializable]
    public class ChatSession
    {
        public string sessionId;
        public string[] messages;
    }

    [System.Serializable]
    public class ChatHistoryResponse
    {
        public List<ChatSession> sessions;
    }

    private void DisplayMessage(string message, bool isUser)
    {
        
        GameObject newMessage = Instantiate(messagePrefab, chatContent);

        
        UnityEngine.UI.Text messageText = newMessage.GetComponentInChildren<UnityEngine.UI.Text>();
        messageText.text = message;

        if (isUser)
        {
            messageText.alignment = TextAnchor.MiddleRight;  
            messageText.color = Color.white;
        }
        else
        {
            messageText.alignment = TextAnchor.MiddleLeft;
            messageText.color = Color.magenta;
        }

        
        Canvas.ForceUpdateCanvases();
        chatScrollView.verticalNormalizedPosition = 0;
    }

    void OnEnterPressed()
    {
        if (Input.GetKeyDown(KeyCode.Return) && chatInputField.isFocused)
        {
            OnSendButtonClicked();
            chatInputField.DeactivateInputField(); 
        }
    }
}