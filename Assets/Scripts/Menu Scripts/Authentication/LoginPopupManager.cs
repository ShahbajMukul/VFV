using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;
using System.IO;
using Debug = UnityEngine.Debug;

public class LoginPopupManager : MonoBehaviour
{
    public GameObject loginPopup;
    public GameObject registrationPopup;
    public GameObject resetPassPopup;
    public InputField usernameInput;
    public InputField passwordInput;
    public UnityEngine.UI.Text errorMessageText;
    public UnityEngine.UI.Text loginStatusMsgLabel;
    public Button ChatbotButton;
    public Button MenuLoginButton;
    public Button MenuLogoutButton;
    public Button MenuOpenRegCodeEnterPopup;


    private string loginUrl = "https://storai.net/api/login";
    private string sessionFilePath;
    SessionData sessionData = new SessionData();

    void Start()
    {

        // Path for saving session token
        sessionFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");


        // Load the session token from file
        if (File.Exists(sessionFilePath))
        {
            string sessionToken = File.ReadAllText(sessionFilePath);
            sessionData = JsonUtility.FromJson<SessionData>(sessionToken);
            if (!string.IsNullOrEmpty(sessionData.sessionToken))
            {
                UnityEngine.Debug.Log("Stored session token found. Skipping login...");
                loginPopup.SetActive(false);
                MenuLogoutButton?.gameObject.SetActive(true);
                MenuLoginButton?.gameObject.SetActive(false);
                if (sessionData.isActive)
                {
                    MenuOpenRegCodeEnterPopup.gameObject.SetActive(false);
                }
                else
                {
                    MenuOpenRegCodeEnterPopup.gameObject.SetActive(true);
                }
            }
            else
            {
                PromptForLogin();
            }
        }
        else
        {
            PromptForLogin();
        }

        HideErrorMessage();
    }

    private void PromptForLogin()
    {
        UnityEngine.Debug.LogWarning("No session token found, prompting user to log in.");
        loginPopup.SetActive(true);

        MenuLoginButton?.gameObject.SetActive(true);   // Show Login
        MenuLogoutButton?.gameObject.SetActive(false); // Hide Logout

        MenuOpenRegCodeEnterPopup?.gameObject.SetActive(false);  //hideEnterRegistrationCode 
    }

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            ShowErrorMessage("All fields must be filled!");
            UnityEngine.Debug.LogWarning("Validation failed: All fields must be filled.");
            return;
        }

        StartCoroutine(LoginUser(usernameInput.text, passwordInput.text));
    }

    private IEnumerator LoginUser(string identifier, string password)
    {
        string jsonData = IsEmail(identifier)
            ? $"{{\"email\":\"{identifier}\",\"password\":\"{password}\"}}"
            : $"{{\"username\":\"{identifier}\",\"password\":\"{password}\"}}";

        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(loginUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.Log("Sending login request with payload: " + jsonData);

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                UnityEngine.Debug.LogError("HTTP error received from server: " + www.error);
                var jsonResponse = JsonUtility.FromJson<Response>(www.downloadHandler.text);
                ShowErrorMessage("Login failed: " + jsonResponse.message);
            }
            else if (www.responseCode == 200)
            {
                // On successful login, save the session token
                string sessionToken = www.GetResponseHeader("Set-Cookie");
                if (!string.IsNullOrEmpty(sessionToken))
                {
                    SaveSessionData(sessionToken, true);

                    // Parse the username from the response
                    string jsonResponse = www.downloadHandler.text;
                    LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                    string username = loginResponse.username;
                    if (string.IsNullOrEmpty(username))
                    {
                        if (!IsEmail(identifier))
                        {
                            username = identifier;
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Username not returned by server and identifier was email.");
                            ShowErrorMessage("Unable to retrieve username. Please try again.");
                            yield break;
                        }
                    }

                    // Save the username in PlayerPrefs
                    PlayerPrefs.SetString("LoggedInUsername", username);
                    PlayerPrefs.Save();

                    UnityEngine.Debug.Log("Login successful! Session token and username saved locally.");
                    errorMessageText.text = "Login successful!";

                    loginPopup.SetActive(false);
                    ChatbotButton.gameObject.SetActive(true);
                    ChatbotButton.interactable = true;
                    MenuLoginButton.gameObject.SetActive(false);
                    MenuLogoutButton.gameObject.SetActive(true);
                    usernameInput.text = string.Empty;
                    passwordInput.text = string.Empty;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No session token found in response.");
                    errorMessageText.text = "Login successful, but session token not found.";
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("Unexpected response code: " + www.responseCode);
                errorMessageText.text = "Unexpected response from server.";
                ShowErrorMessage("Login failed: Unexpected response from server. Try again later!");
            }
        }
    }

    private void SaveSessionData(string sessionToken, bool isActive)
    {
        SessionData data = new SessionData();
        data.sessionToken = sessionToken;
        data.isActive = isActive;

        try
        {
            string jsonData = JsonUtility.ToJson(data);
            File.WriteAllText(sessionFilePath, jsonData);
            Debug.Log("Session data saved.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving session data: " + ex.Message);
        }
    }

    public void Logout()
    {
        // Remove stored session token
        if (File.Exists(sessionFilePath))
        {
            File.Delete(sessionFilePath);
        }

        // Clear the stored username
        PlayerPrefs.DeleteKey("LoggedInUsername");
        PlayerPrefs.Save();

        // Clear chat history
        ChatbotManager chatbotManager = FindObjectOfType<ChatbotManager>();
        if (chatbotManager != null)
        {
            chatbotManager.ClearChatHistory();
        }

        // Update UI to show login again
        loginPopup.SetActive(true);
        loginStatusMsgLabel.text = "You have been logged out.";
        UnityEngine.Debug.Log("Session token and username cleared. User logged out.");
        MenuLoginButton.gameObject.SetActive(true);
        MenuLogoutButton.gameObject.SetActive(false);
        MenuOpenRegCodeEnterPopup.gameObject.SetActive(false);
    }

    private bool IsEmail(string input)
    {
        return Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    public void ShowRegiPopup()
    {
        CloseLoginPopup();
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void ShowResetPopup()
    {
        CloseLoginPopup();
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(true);
        }
    }

    public void CloseLoginPopup()
    {
        if (ChatbotButton != null)
        {
            ChatbotButton.interactable = false;
        }
        MenuLoginButton.gameObject.SetActive(true);
        MenuLogoutButton.gameObject.SetActive(false);

        loginPopup?.SetActive(false);
    }

    private void ShowErrorMessage(string message)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
    }

    private void HideErrorMessage()
    {
        errorMessageText.text = "";
        errorMessageText.gameObject.SetActive(false);
    }

    [Serializable]
    private class Response
    {
        public string message;
    }

    [Serializable]
    private class LoginResponse
    {
        public string username;
    }

    [System.Serializable]
    private class SessionData
    {
        public string sessionToken;
        public bool isActive;
    }
}
