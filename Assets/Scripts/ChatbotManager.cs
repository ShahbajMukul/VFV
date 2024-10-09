using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChatbotManager : MonoBehaviour
{
    public GameObject aiMessagePrefab;
    public GameObject userMessagePrefab;
    public Transform contentTransform;
    public TMP_InputField inputField;

    // Called when the send button is clicked
    public void OnSendButtonClick()
    {
        string userInput = inputField.text;

        if (!string.IsNullOrEmpty(userInput))
        {
            // Create user message
            GameObject userMessage = Instantiate(userMessagePrefab, contentTransform);
            userMessage.GetComponentInChildren<TextMeshProUGUI>().text = userInput;

            // Clear the input field
            inputField.text = "";

            // Simulate AI response (you can replace this with actual AI logic)
            StartCoroutine(SimulateAIResponse());
        }
    }

    // Simulate an AI response with a delay
    private IEnumerator SimulateAIResponse()
    {
        yield return new WaitForSeconds(1f); // Simulate a short delay before AI responds

        string aiResponse = "AI Response here..."; // Replace this with actual AI response

        // Create AI message
        GameObject aiMessage = Instantiate(aiMessagePrefab, contentTransform);
        aiMessage.GetComponentInChildren<TextMeshProUGUI>().text = aiResponse;
    }
}
