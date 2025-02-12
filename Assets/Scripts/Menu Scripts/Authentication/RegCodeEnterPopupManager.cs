using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegCodeEnterPopupManager : MonoBehaviour
{

    public InputField regiCodeInput;

    public Button openRegCodeEnterPopupButton;
    public Button activateAccButton;
    public Button chatbotButton;


    public GameObject regCodeEnterPopup;
    public Text errorMessageText;

    private string regCodeEnterUrl = "https://storai.net/api/enter-code";

    void Start()
    {
        // Initialize error message and set chatbot button inactive by default
        errorMessageText.text = "";
    }


    public void OnActivateButtonClicked()
    {
        // Get and validate the entered code
        string code = regiCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            errorMessageText.text = "Please enter a valid registration code.";
            Debug.LogWarning("No registration code entered.");
            return;
        }

        Debug.Log("Attempting to activate with code: " + code);

        // Start the coroutine to activate the account
        StartCoroutine(ActivateAccount(code));
    }

    private IEnumerator ActivateAccount(string code)
    {
        string jsonPayload = JsonUtility.ToJson(new RequestPayload { code = code });
        using (UnityWebRequest www = new UnityWebRequest(regCodeEnterUrl, "POST"))
        {
            byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP Error: " + www.error);
                errorMessageText.text = "An error occurred: " + www.error;
            }
            else
            {
                Debug.Log("Response: " + www.downloadHandler.text);
                var response = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);

                if (response.active)
                {
                    Debug.Log("Activation successful with User ID: " + response.userId);
                    errorMessageText.text = "Activation successful! StorAI is now enabled.";
                    SaveSession(response.userId); // Save the user ID returned from the server
                    ActivateChatbot();
                    CloseRegiCodeEnterPopup();
                }
                else
                {
                    errorMessageText.text = "Invalid registration code: " + response.message;
                }
            }
        }
    }

    [System.Serializable]
    private class ResponseData
    {
        public string message;
        public bool active;
        public string userId;
    }

    private void SaveSession(string username)
    {
        // Save the username for session persistence
        PlayerPrefs.SetString("LoggedInUsername", username);
        PlayerPrefs.Save();
        Debug.Log("Session saved for username: " + username);
    }

    private void ActivateChatbot()
    {
        // Enable the chatbot button
        chatbotButton.gameObject.SetActive(true);
        chatbotButton.interactable = true;
        Debug.Log("Chatbot activated and ready for use.");
    }

    public void ShowRegiCodeEnterPopup()
    {
        if (regCodeEnterPopup != null)
        {
            regCodeEnterPopup.SetActive(true);
        }
    }

    public void CloseRegiCodeEnterPopup()
    {
        if (regCodeEnterPopup != null)
        {
            regCodeEnterPopup.SetActive(false);
        }
    }

    [System.Serializable]
    private class RequestPayload
    {
        public string code;
    }
}