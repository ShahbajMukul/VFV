using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RegCodeEnterPopupManager : MonoBehaviour
{

    public InputField regiCodeInput;

    public Button panelOpenRegiCodeEnterPopup;
    public Button activateAccButton;
    public Button chatbotButton;
    public Button panelOpenRegiCodeEnterPopupButton;


    public GameObject regCodeEnterPopup;
    public Text errorMessageText;

    private string regCodeEnterUrl = "https://storai.net/api/enter-code";
    private string sessionFilePath;
    void Start()
    {
        sessionFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");
        // Initialize error message and set chatbot button inactive by default
        errorMessageText.text = "";

        // Always hide the code button by default
        panelOpenRegiCodeEnterPopup.gameObject.SetActive(false);

        if (File.Exists(sessionFilePath))
        {
            string jsonData = File.ReadAllText(sessionFilePath);
            SessionData data = JsonUtility.FromJson<SessionData>(jsonData);

            // If the session file is valid and has a token
            if (data != null && !string.IsNullOrEmpty(data.sessionToken))
            {
                // If they're NOT active but have a token , show the code button
                if (!data.isActive)
                {
                    panelOpenRegiCodeEnterPopup.gameObject.SetActive(true);
                }
                else
                {
                    // If they are active, hide the entire code popup
                    regCodeEnterPopup.SetActive(false);
                    panelOpenRegiCodeEnterPopup.gameObject.SetActive(false);
                }
            }
            else
            {
                // We have a file but no valid token , treat as logged out = hide
                panelOpenRegiCodeEnterPopup.gameObject.SetActive(false);
            }
        }
        else
        {
            // No session file at all , user is not logged in = hide
            panelOpenRegiCodeEnterPopup.gameObject.SetActive(false);
        }
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

            string sessionToken = LoadSessionToken();
            if (!string.IsNullOrEmpty(sessionToken))
            {
                www.SetRequestHeader("Cookie", sessionToken);
            }

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP Error: " + www.error);
                if (www.responseCode == 400) // Bad Request
                {
                    try
                    {
                        // Attempt to parse the error message from the server
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(www.downloadHandler.text);
                        if (!string.IsNullOrEmpty(errorResponse.message))
                        {
                            errorMessageText.text = errorResponse.message; // Display server's error message
                        }
                        else
                        {
                            errorMessageText.text = "Wrong code. Please try again."; // Default message
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Failed to parse error response: " + ex.Message);
                        errorMessageText.text = "Wrong code. Please try again."; // Fallback message
                    }
                }
                else
                {
                    Debug.LogError(www.error);
                    errorMessageText.text = "An error occurred. Please try again later."; // Generic error for other HTTP errors
                }
            }
            else
            {
                Debug.Log("Response: " + www.downloadHandler.text);
                var response = JsonUtility.FromJson<ResponseData>(www.downloadHandler.text);

                if (response.active)
                {
                    Debug.Log("Activation successful with User ID: " + response.userId);
                    errorMessageText.text = "Activation successful! StorAI is now enabled.";
                    UpdateSessionDataToActive();  // Update session data first
                    SaveUserId(response.userId); // Then save user ID
                    ActivateChatbot();
                    MakeUIChanges();
                }
                else
                {
                    errorMessageText.text = "Invalid registration code: " + response.message;
                }
            }
        }
    }

    private void UpdateSessionDataToActive()
    {
        try
        {
            if (File.Exists(sessionFilePath))
            {
                string jsonData = File.ReadAllText(sessionFilePath);
                SessionData data = JsonUtility.FromJson<SessionData>(jsonData);
                data.isActive = true;
                File.WriteAllText(sessionFilePath, JsonUtility.ToJson(data));
                Debug.Log("Updated session data to active status");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error updating session status: " + e.Message);
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
                    Debug.LogError("Error parsing session file: " + e.Message);
                }
            }
        }
        return string.Empty;
    }


    private void MakeUIChanges()
    {
        activateAccButton.interactable = false;
        panelOpenRegiCodeEnterPopup.gameObject.SetActive(false);
    }

    [System.Serializable]
    private class ResponseData
    {
        public string message;
        public bool active;
        public string userId;
    }

    private void SaveUserId(string userId)
    {
        PlayerPrefs.SetString("LoggedInUsername", userId);
        PlayerPrefs.Save();
        Debug.Log("Saved user ID: " + userId);
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
    [System.Serializable]
    private class SessionData
    {
        public string sessionToken;
        public bool isActive;
    }

    [System.Serializable]
    private class ErrorResponse
    {
        public string message;
    }
}