using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using UnityEngine.UI;
public class ChatbotManager : MonoBehaviour
{
    public GameObject chatbotPopup;
    public GameObject messagePrefab;  
    public Transform chatContent;     
    public InputField chatInputField; 
    public ScrollRect chatScrollView; 


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

    public void OpenChatbotPopUp()
    {
        chatbotPopup.SetActive(true);
    }

    public void CloseChatbotPopUp()
    {
        chatbotPopup.gameObject.SetActive(false);
    }
}
