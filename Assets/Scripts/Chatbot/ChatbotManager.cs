using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.IO;
using System;
using System.Diagnostics;

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
    public GameObject noticeText;


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
            UnityEngine.Debug.Log("No logged-in user found. Cannot load chat history.");
            chatbotPopup.SetActive(false);
            return;
        }

        if (File.Exists(sessionFilePath))
        {
            string fileContent = File.ReadAllText(sessionFilePath);
            SessionData data = JsonUtility.FromJson<SessionData>(fileContent);
            if (data != null && !data.isActive)
            {
                UnityEngine.Debug.Log("User is not active -> Disabling StorAI chatbot");
                chatbotPopup.SetActive(false);
                return;
            }
        }

        UnityEngine.Debug.Log("Logged in as user: " + userId);

        // Initialize paths
        InitializePaths();

        // Load chat history for the logged-in user
        LoadChatHistory();

        if (chatSessions == null || chatSessions.Count == 0)
        {
            CreateNewSession();
            noticeText.SetActive(true);
        }
        else
        {
            // Load the last session
            LoadSession(chatSessions[chatSessions.Count - 1]);
            noticeText.SetActive(false);

        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Toggle the chatbot popup
            if (chatbotPopup.activeSelf)
            {
                CloseChatbotPopUp();
            }
            else
            {
                OpenChatbotPopUp();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            OpenChatbotPopUp();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnSendButtonClicked();
            chatInputField.ActivateInputField(); // Keep the input field active for convenience
        }
    }

    private string aiGenerateTextUrl = "https://storai.net/ai/generate-text";


    public void CreateNewSession()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);
        UnityEngine.Debug.Log("CreateNewSession called. userId: " + userId);

        if (string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogError("User ID is missing. Cannot create new session.");
            return;
        }

        sessionCount++;
        string sessionName = "Session " + sessionCount;

        ChatSession newSession = new ChatSession(sessionName);
        chatSessions.Add(newSession);

        noticeText.gameObject.SetActive(false);

        // Create a session button in the UI
        GameObject sessionButtonObj = Instantiate(sessionButtonPrefab, sessionListContent.transform);
        sessionButtonObj.GetComponentInChildren<UnityEngine.UI.Text>().text = sessionName;

        // Initialize the SessionButtonController and add click events
        SessionButtonController buttonController = sessionButtonObj.GetComponent<SessionButtonController>();
        if (buttonController != null)
        {
            buttonController.Initialize(this, sessionName);

            // Set up the click events: load the session and set the button color
            Button sessionButton = sessionButtonObj.GetComponent<Button>();
            sessionButton.onClick.AddListener(() => LoadSession(newSession));
            sessionButton.onClick.AddListener(buttonController.SetActiveColor);
        }

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
                UnityEngine.Debug.LogError("No session token found. Cannot authenticate the request.");
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
                UnityEngine.Debug.LogError("HTTP Error: " + www.error);
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
                    UnityEngine.Debug.LogError("JSON Parsing Error: " + e.Message);
                    aiResponse = "Error: Failed to parse AI response. Please try again later.";
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Unexpected response code: " + www.responseCode);
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
            string fileContent = File.ReadAllText(sessionFilePath).Trim();
            if (!string.IsNullOrEmpty(fileContent))
            {
                try
                {
                    SessionData data = JsonUtility.FromJson<SessionData>(fileContent);
                    if (data != null && !string.IsNullOrEmpty(data.sessionToken))
                    {
                        return data.sessionToken;
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Error parsing session file: " + e.Message);
                }
            }
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

        UnityEngine.Debug.Log("Chat history cleared for user: " + userId);
    }

    private void InitializePaths()
    {
        string persistentPath = Application.persistentDataPath;

        if (string.IsNullOrEmpty(persistentPath))
        {
            UnityEngine.Debug.LogError("Application.persistentDataPath is empty!");
            return;
        }

        // Construct paths
        userDirectoryPath = Path.Combine(persistentPath, "chat_history", userId);
        userChatPath = Path.Combine(userDirectoryPath, "chat_history.json");

        UnityEngine.Debug.Log("Application.persistentDataPath: " + persistentPath);
        UnityEngine.Debug.Log("userDirectoryPath: " + userDirectoryPath);
        UnityEngine.Debug.Log("userChatPath: " + userChatPath);

        if (!Directory.Exists(userDirectoryPath))
        {
            Directory.CreateDirectory(userDirectoryPath);
            UnityEngine.Debug.Log("Created user directory at: " + userDirectoryPath);
        }
    }

    private void SaveChatHistory()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);

        UnityEngine.Debug.Log("SaveChatHistory called. userId: " + userId);

        if (string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogError("User ID is missing. Cannot save chat history.");
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

            UnityEngine.Debug.Log("Chat history saved for user: " + userId + " at " + userChatPath);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Failed to save chat history: " + e.Message);
        }
    }

    private void LoadChatHistory()
    {
        userId = PlayerPrefs.GetString("LoggedInUsername", string.Empty);

        if (string.IsNullOrEmpty(userId))
        {
            UnityEngine.Debug.LogError("User ID is missing. Cannot load chat history.");
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

                        UnityEngine.Debug.Log("Chat history loaded for user: " + userId);
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("Chat history is empty or invalid.");
                        chatSessions = new List<ChatSession>();
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Chat history file is empty.");
                    chatSessions = new List<ChatSession>();
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Failed to load chat history: " + e.Message);
                chatSessions = new List<ChatSession>();
            }
        }
        else
        {
            UnityEngine.Debug.Log("No chat history file found for user: " + userId);
            chatSessions = new List<ChatSession>();
        }
    }

    private void RefreshSessionListUI()
    {
        // Clear existing buttons in the session list
        foreach (Transform child in sessionListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Create a button for each session with a delete option
        foreach (ChatSession session in chatSessions)
        {
            GameObject sessionButtonObj = Instantiate(sessionButtonPrefab, sessionListContent.transform);
            sessionButtonObj.GetComponentInChildren<UnityEngine.UI.Text>().text = session.sessionName;

            // Access the SessionButtonController and initialize it with the ChatbotManager instance
            SessionButtonController buttonController = sessionButtonObj.GetComponent<SessionButtonController>();
            if (buttonController != null)
            {
                buttonController.Initialize(this, session.sessionName);

                // Add OnClick event to turn this button green and reset others
                Button sessionButton = sessionButtonObj.GetComponent<Button>();
                sessionButton.onClick.AddListener(buttonController.SetActiveColor);
                sessionButton.onClick.AddListener(() => LoadSession(session));

            }
        }
    }
    [Serializable]
    private class SessionData
    {
        public string sessionToken;
        public bool isActive;
    }


    public void ResetOtherSessionButtonColors(SessionButtonController activeButton)
    {
        // Iterate through all session buttons and reset their color, except the active one
        foreach (Transform child in sessionListContent.transform)
        {
            SessionButtonController buttonController = child.GetComponent<SessionButtonController>();
            if (buttonController != null && buttonController != activeButton)
            {
                buttonController.SetDefaultColor();
            }
        }
    }

    public void DeleteSessionByName(string sessionName)
    {
        // Find the session by name
        ChatSession sessionToDelete = chatSessions.Find(session => session.sessionName == sessionName);

        if (sessionToDelete != null)
        {
            // Remove session from list
            chatSessions.Remove(sessionToDelete);

            // Save the updated chat history
            SaveChatHistory();

            // Refresh the session list UI
            RefreshSessionListUI();

            // Load a different session if the deleted session was active
            if (currentSession == sessionToDelete)
            {
                if (chatSessions.Count > 0)
                {
                    LoadSession(chatSessions[chatSessions.Count - 1]); // Load the most recent session
                }
                else
                {
                    CreateNewSession(); // Create a new session if none exist
                }
            }

            UnityEngine.Debug.Log("Session deleted: " + sessionName);
        }
        else
        {
            UnityEngine.Debug.LogError("Session not found: " + sessionName);
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