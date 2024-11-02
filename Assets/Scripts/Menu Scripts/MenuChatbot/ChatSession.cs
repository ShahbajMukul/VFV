using System.Collections.Generic;
using UnityEngine;

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
