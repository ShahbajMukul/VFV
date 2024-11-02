using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.IO;
using System;

public class ChatbotManager : MonoBehaviour
{
    public GameObject chatbotPopup;
    public GameObject chatContent;
    public GameObject aiMessagePrefab;
    public GameObject userMessagePrefab;
    public InputField chatInputField;
    public ScrollRect chatScrollRect;

    public GameObject sessionListContent;
    public GameObject sessionButtonPrefab;

    private List<ChatSession> chatSessions = new List<ChatSession>();
    private ChatSession currentSession;
    private int sessionCount = 0;

    private string userId; // UserID references logged-in username
    private string userDirectoryPath;
    private string userChatPath;
    private string sessionFilePath;

    void Start()
    {
        // Load the session token from the session file
        sessionFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");
        string sessionToken = LoadSessionToken();

        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("No logged-in user found. Cannot load chat history.");
            chatbotPopup.SetActive(false);
            return;
        }

        Debug.Log("Logged in as user: " + userId);

        // Initialize paths
        InitializePaths();

        // Load chat history for the logged-in user
        LoadChatHistory();

        if (chatSessions == null || chatSessions.Count == 0)
        {
            CreateNewSession();
        }
        else
        {
            // Load the last session
            LoadSession(chatSessions[chatSessions.Count - 1]);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnSendButtonClicked();
        }
    }

    private string aiGenerateTextUrl = "http://localhost:3002/ai/generate-text";

    public void CreateNewSession()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);
        Debug.Log("CreateNewSession called. userId: " + userId);

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is missing. Cannot create new session.");
            return;
        }

        sessionCount++;
        string sessionName = "Session " + sessionCount;

        ChatSession newSession = new ChatSession(sessionName);
        chatSessions.Add(newSession);

        // Create a session button in the UI
        GameObject sessionButtonObj = Instantiate(sessionButtonPrefab, sessionListContent.transform);
        sessionButtonObj.GetComponentInChildren<UnityEngine.UI.Text>().text = sessionName;

        // Add a click event to the session button
        Button sessionButton = sessionButtonObj.GetComponent<Button>();
        sessionButton.onClick.AddListener(() => LoadSession(newSession));

        // Automatically switch to the new session
        LoadSession(newSession);

        // Save chat history after creating a new session
        SaveChatHistory();
    }

    public void OnSendButtonClicked()
    {
        string userInput = chatInputField.text;

        if (!string.IsNullOrEmpty(userInput) && currentSession != null)
        {
            // Display the user's message
            GameObject userMessage = Instantiate(userMessagePrefab, chatContent.transform);
            userMessage.GetComponentInChildren<UnityEngine.UI.Text>().text = userInput;

            // Save the user's message to the session
            ChatMessage userChatMessage = new ChatMessage("User", userInput);
            currentSession.messages.Add(userChatMessage);

            chatInputField.text = "";

            // Scroll to the bottom
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;

            // Save chat history
            SaveChatHistory();

            // Actual AI response
            StartCoroutine(GetAIResponse(userInput));
        }
    }

    IEnumerator GetAIResponse(string userMessage)
    {
        // Prepare JSON payload
        string jsonData = $"{{\"ai_type\":\"default\",\"userMessage\":\"{userMessage}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        // Instantiate the AI message object in the UI (so it appears immediately)
        GameObject aiMessageObject = Instantiate(aiMessagePrefab, chatContent.transform);

        using (UnityWebRequest www = new UnityWebRequest(aiGenerateTextUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            // Retrieve and add the session token for authorization
            string sessionToken = LoadSessionToken();
            if (string.IsNullOrEmpty(sessionToken))
            {
                Debug.LogError("No session token found. Cannot authenticate the request.");
                aiMessageObject.GetComponentInChildren<UnityEngine.UI.Text>().text = "Error: Unable to get AI response. Please try again.";
                yield break; // Exit the coroutine since no token is present
            }

            // Use only the session ID part, excluding other attributes
            www.SetRequestHeader("Cookie", sessionToken);

            // Default AI response in case of an error
            string aiResponse = "Error: Unable to get AI response. Please try again.";

            // Send request and wait for response
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP Error: " + www.error);
                aiResponse = "Error: Unable to get AI response. Please try again.";
            }
            else if (www.responseCode == 200)
            {
                string jsonResponse = www.downloadHandler.text;

                try
                {
                    // Attempt to parse the JSON response
                    AIResponse response = JsonUtility.FromJson<AIResponse>(jsonResponse);
                    if (response != null && !string.IsNullOrEmpty(response.content))
                    {
                        aiResponse = response.content;
                    }
                    else
                    {
                        aiResponse = "Error: Invalid response from AI. Please try again later.";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON Parsing Error: " + e.Message);
                    aiResponse = "Error: Failed to parse AI response. Please try again later.";
                }
            }
            else
            {
                Debug.LogWarning("Unexpected response code: " + www.responseCode);
                aiResponse = "Error: Unexpected response from AI.";
            }

            aiMessageObject.GetComponentInChildren<UnityEngine.UI.Text>().text = aiResponse;

            ChatMessage aiChatMessage = new ChatMessage("AI", aiResponse);
            currentSession.messages.Add(aiChatMessage);

            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;

            SaveChatHistory();
        }
    }

    private string LoadSessionToken()
    {
        if (File.Exists(sessionFilePath))
        {
            string token = File.ReadAllText(sessionFilePath).Trim();
            return token;
        }
        return string.Empty;
    }

    public void LoadSession(ChatSession session)
    {
        currentSession = session;

        // Clear the chat content UI
        foreach (Transform child in chatContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Load messages from the session into the UI
        foreach (ChatMessage msg in currentSession.messages)
        {
            GameObject messageObj;
            if (msg.sender == "User")
            {
                messageObj = Instantiate(userMessagePrefab, chatContent.transform);
            }
            else
            {
                messageObj = Instantiate(aiMessagePrefab, chatContent.transform);
            }
            messageObj.GetComponentInChildren<UnityEngine.UI.Text>().text = msg.messageText;
        }

        // Scroll to the bottom
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    public void OpenChatbotPopUp()
    {
        chatbotPopup.SetActive(true);
    }

    public void CloseChatbotPopUp()
    {
        chatbotPopup.SetActive(false);
    }

    public void ClearChatHistory()
    {
        // Clear chat sessions and delete history file
        chatSessions.Clear();
        sessionCount = 0;
        currentSession = null;

        if (File.Exists(userChatPath))
        {
            File.Delete(userChatPath);
        }

        // Clear the UI
        foreach (Transform child in chatContent.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in sessionListContent.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Chat history cleared for user: " + userId);
    }

    private void InitializePaths()
    {
        string persistentPath = Application.persistentDataPath;

        if (string.IsNullOrEmpty(persistentPath))
        {
            Debug.LogError("Application.persistentDataPath is empty!");
            return;
        }

        // Construct paths
        userDirectoryPath = Path.Combine(persistentPath, "chat_history", userId);
        userChatPath = Path.Combine(userDirectoryPath, "chat_history.json");

        Debug.Log("Application.persistentDataPath: " + persistentPath);
        Debug.Log("userDirectoryPath: " + userDirectoryPath);
        Debug.Log("userChatPath: " + userChatPath);

        if (!Directory.Exists(userDirectoryPath))
        {
            Directory.CreateDirectory(userDirectoryPath);
            Debug.Log("Created user directory at: " + userDirectoryPath);
        }
    }

    private void SaveChatHistory()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);

        Debug.Log("SaveChatHistory called. userId: " + userId);

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is missing. Cannot save chat history.");
            return;
        }

        // Re-initialize paths in case they have changed
        InitializePaths();

        try
        {
            ChatHistory chatHistory = new ChatHistory(chatSessions);

            string json = JsonUtility.ToJson(chatHistory, true);

            // Write to file
            File.WriteAllText(userChatPath, json);

            Debug.Log("Chat history saved for user: " + userId + " at " + userChatPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save chat history: " + e.Message);
        }
    }

    private void LoadChatHistory()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is missing. Cannot load chat history.");
            return;
        }

        InitializePaths();

        if (File.Exists(userChatPath))
        {
            try
            {
                string json = File.ReadAllText(userChatPath);
                if (!string.IsNullOrEmpty(json))
                {
                    ChatHistory history = JsonUtility.FromJson<ChatHistory>(json);
                    if (history != null && history.sessions != null)
                    {
                        chatSessions = history.sessions;
                        sessionCount = chatSessions.Count;

                        RefreshSessionListUI();

                        Debug.Log("Chat history loaded for user: " + userId);
                    }
                    else
                    {
                        Debug.LogWarning("Chat history is empty or invalid.");
                        chatSessions = new List<ChatSession>();
                    }
                }
                else
                {
                    Debug.LogWarning("Chat history file is empty.");
                    chatSessions = new List<ChatSession>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load chat history: " + e.Message);
                chatSessions = new List<ChatSession>();
            }
        }
        else
        {
            Debug.Log("No chat history file found for user: " + userId);
            chatSessions = new List<ChatSession>();
        }
    }

    private void RefreshSessionListUI()
    {
        foreach (Transform child in sessionListContent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (ChatSession session in chatSessions)
        {
            GameObject sessionButtonObj = Instantiate(sessionButtonPrefab, sessionListContent.transform);
            sessionButtonObj.GetComponentInChildren<UnityEngine.UI.Text>().text = session.sessionName;

            Button sessionButton = sessionButtonObj.GetComponent<Button>();
            sessionButton.onClick.AddListener(() => LoadSession(session));
        }
    }

    [System.Serializable]
    public class AIResponse
    {
        public string content;
    }
}

[System.Serializable]
public class ChatHistory
{
    public List<ChatSession> sessions;

    public ChatHistory(List<ChatSession> sessions)
    {
        this.sessions = sessions;
    }
}