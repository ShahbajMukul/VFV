using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;

public class ChatbotHandler : MonoBehaviour
{
    public GameObject chatbotPopupPanel;
    public GameObject AskChatbotButton;

    public void AskChatbotButtonClicked()
    {
        if (chatbotPopupPanel != null)
            chatbotPopupPanel.SetActive(true);
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
