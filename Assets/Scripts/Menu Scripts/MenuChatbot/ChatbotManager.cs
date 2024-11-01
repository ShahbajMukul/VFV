using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
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

    //Suggestions:
    /*our local PostgreSQL database in the backend can store the chat history for each user*/

    /*we can create new endpoints for saving and retrieving chat history in aiserver.js*/

    private List<ChatSession> chatSessions = new List<ChatSession>();
    private ChatSession currentSession;
    private int sessionCount = 0;

    void Start()
    {
        //StartCoroutine(GetChatHistory());
        CreateNewSession();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnSendButtonClicked();
        }
    }

    private string aiGenerateTextUrl = "http://localhost:3002/ai/generate-text";

    //the new endpoint would be similar like this:
    //private string aiSaveChatHistoryUrl = "http://localhost:3002/ai/save-chat-history";
    //private string aiGetChatHistoryUrl = "http://localhost:3002/ai/get-chat-history";

    public void CreateNewSession()
    {
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

            // Simulate AI response
            // StartCoroutine(TestAIResponse());

            // Actual AI reponse
            StartCoroutine(GetAIResponse(userInput));
        }
    }


    private IEnumerator TestAIResponse()
    {
        // Simulate a typing delay
        yield return new WaitForSeconds(1f);

        string aiResponse = "Greetings! I'm your friendly StorAI, here to make your gaming experience smoother. Feel free to ask me anything, from game tips to general questions. I'm always learning and improving, so don't hesitate to challenge me!";

        // Display the AI's message
        GameObject aiMessageObject = Instantiate(aiMessagePrefab, chatContent.transform);
        aiMessageObject.GetComponentInChildren<UnityEngine.UI.Text>().text = aiResponse;

        // Save the AI's message to the session
        ChatMessage aiChatMessage = new ChatMessage("AI", aiResponse);
        currentSession.messages.Add(aiChatMessage);

        // Scroll to the bottom
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }


    IEnumerator GetAIResponse(string userMessage)
    {
        // Prepare JSON payload
        string jsonData = $"{{\"ai_type\":\"default\",\"userMessage\":\"{userMessage}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        // Instantiate the AI message object in the UI (so it appears immediately)
        GameObject aiMessageObject = Instantiate(aiMessagePrefab, chatContent.transform);

        // Use UnityWebRequest with POST method
        using (UnityWebRequest www = new UnityWebRequest(aiGenerateTextUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

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

            // Update the AI message object's text with the response
            aiMessageObject.GetComponentInChildren<UnityEngine.UI.Text>().text = aiResponse;

            // Save the message to the current session
            ChatMessage aiChatMessage = new ChatMessage("AI", aiResponse);
            currentSession.messages.Add(aiChatMessage);

            // Scroll to the bottom to show the latest message
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
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



    public void OpenChatbotPopUp()
    {
        chatbotPopup.SetActive(true);
    }

    public void CloseChatbotPopUp()
    {
        chatbotPopup.SetActive(false);

        //if (currentSession.Count > 0)
        //{
        //    chatSessions.Add(new List<string>(currentSession));
        //    //StartCoroutine(SaveChatHistory(currentSession));
        //    currentSession.Clear();
        //    // LoadChatHistory();
        //}
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

    void OnEnterPressed()
    {
        if (Input.GetKeyDown(KeyCode.Return) && chatInputField.isFocused)
        {
            OnSendButtonClicked();
            chatInputField.DeactivateInputField();
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
        public string sessionName;
        public List<ChatMessage> messages;

        public ChatSession(string name)
        {
            sessionName = name;
            messages = new List<ChatMessage>();
        }
    }

    [System.Serializable]
    public class ChatMessage
    {
        public string sender;
        public string messageText;

        public ChatMessage(string sender, string text)
        {
            this.sender = sender;
            messageText = text;
        }
    }


}