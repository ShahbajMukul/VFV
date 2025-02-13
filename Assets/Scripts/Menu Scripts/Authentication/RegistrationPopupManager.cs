using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.IO;

public class RegistrationPopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject reqRegistrationCodePopup;
    public InputField emailInput;
    public InputField usernameInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public InputField registrationCodeInput;
    public UnityEngine.UI.Text errorMessageText;
    public Button ChatbotButton;
    public Button MenuLoginButton;
    public Button MenuLogoutButton;

    public Button MenuOpenRegCodeEnterPopup;

    private string registrationUrl = "https://storai.net/api/register-optional";
    private string sessionTokenFilePath;

    void Start()
    {
        // Set the file path for saving the session token
        sessionTokenFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");

        
        SessionData sessionData = LoadSessionData();

        if (sessionData != null && !string.IsNullOrEmpty(sessionData.sessionToken))
        {
            if (!sessionData.isActive)
            {
                Debug.Log("Inactive session - show registration code input");
                MenuOpenRegCodeEnterPopup.gameObject.SetActive(true);
                ChatbotButton.interactable = false;
            }
            else
            {
                Debug.Log("Active session - hide reg code button");
                MenuOpenRegCodeEnterPopup.gameObject.SetActive(false);
                MenuLoginButton.gameObject.SetActive(false);
                MenuLogoutButton.gameObject.SetActive(true);
                ChatbotButton.interactable = true;
            }
        }
        else
        {
            Debug.Log("No session - show login UI");
            ActivateUIPanel(loginPopup);
        }
    }

    private void ActivateUIPanel(GameObject targetPanel)
    {
        DeactivateAllPanels();

        // Activate the target panel
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }
    }

    private void DeactivateAllPanels()
    {
        Debug.Log("Deactivating all panels.");
        if (registrationPopup != null) registrationPopup.SetActive(false);
        if (loginPopup != null) loginPopup.SetActive(false);
        if (reqRegistrationCodePopup != null) reqRegistrationCodePopup.SetActive(false);
    }


    public void ShowRegistrationPopup()
    {
        ActivateUIPanel(registrationPopup);
    }

    public void ShowLoginPopup()
    {
        loginPopup.SetActive(true);
        registrationPopup.SetActive(false);
        reqRegistrationCodePopup.SetActive(false);
    }

    public void ShowReqRegistrationCodePopup()
    {
        ActivateUIPanel(reqRegistrationCodePopup);
    }

    public void OnRegisterButtonClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text) ||
            string.IsNullOrEmpty(usernameInput.text) ||
            string.IsNullOrEmpty(passwordInput.text) ||
            string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            ShowErrorMessage("All fields must be filled!");
            return;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowErrorMessage("Passwords do not match!");
            return;
        }

        if (!ValidatePasswordComplexity(passwordInput.text))
        {
            ShowErrorMessage("Password must be at least 8 characters long, include at least one uppercase letter, one symbol, and one number.");
            Debug.LogWarning("Validation failed: Password does not meet complexity requirements.");
            return;
        }

        Debug.Log($"Attempting to register with email: {emailInput.text}, username: {usernameInput.text}, password: {passwordInput.text}, secretCode: {registrationCodeInput.text}");

        StartCoroutine(RegisterUser(emailInput.text, usernameInput.text, passwordInput.text, registrationCodeInput.text));
    }

    private IEnumerator RegisterUser(string email, string username, string password, string registrationCode)
    {
        // Build JSON payload dynamically to exclude optional fields
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{");
        jsonBuilder.Append($"\"email\":\"{email}\",");
        jsonBuilder.Append($"\"username\":\"{username}\",");
        jsonBuilder.Append($"\"password\":\"{password}\"");

        if (!string.IsNullOrEmpty(registrationCode))
        {
            jsonBuilder.Append($",\"secretCode\":\"{registrationCode}\"");
        }

        jsonBuilder.Append("}");
        string jsonData = jsonBuilder.ToString();

        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(registrationUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending registration request with payload: " + jsonData);

            yield return www.SendWebRequest();

            Debug.Log("HTTP Response Code: " + www.responseCode);
            Debug.Log("HTTP Response Text: " + www.downloadHandler.text);

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP error received from server: " + www.error);
                Debug.LogError("Server Response: " + www.downloadHandler.text);
                ShowErrorMessage("Registration failed: " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                if (www.responseCode == 201 || www.responseCode == 200)
                {
                    Debug.Log("Registration successful!");
                    CloseRegistrationPopup();

                    /*// here
                    var response = JsonUtility.FromJson<RegistrationResponse>(www.downloadHandler.text);
                    Debug.Log("User active status: " + response.isActive);
                    */

                    // Parse the JSON with  the actual structure returned by the server
                    // { "message":  "user": { "id., "username "email, "isActive" } }
                    RegistrationResponse serverResponse = JsonUtility.FromJson<RegistrationResponse>(www.downloadHandler.text);

                    if (serverResponse != null && serverResponse.user != null)
                    {
                        bool userIsActive = serverResponse.user.isActive;
                        string setCookie = www.GetResponseHeader("Set-Cookie");

                        // Save both session token AND activation status
                        SaveSessionData(setCookie, userIsActive);

                        if (userIsActive)
                        {
                            ShowLoginPopup();
                        }
                        else
                        {
                            ShowReqRegistrationCodePopup();
                            MenuLoginButton.gameObject.SetActive(false);
                            MenuLogoutButton.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        // In case parsing fails or unexpected JSON
                        ShowErrorMessage("Unexpected response format from server.");
                    }
                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    ShowErrorMessage($"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}");
                }
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
            File.WriteAllText(sessionTokenFilePath, jsonData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving session: " + ex.Message);
        }
    }

    private SessionData LoadSessionData()
    {
        try
        {
            if (File.Exists(sessionTokenFilePath))
            {
                string jsonData = File.ReadAllText(sessionTokenFilePath);
                return JsonUtility.FromJson<SessionData>(jsonData);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error loading session: " + ex.Message);
        }
        return null;
    }

    [System.Serializable]
    private class SessionData
    {
        public string sessionToken;
        public bool isActive;
    }

    public void OnCloseButtonClicked()
    {
        if (ChatbotButton != null)
        {
            ChatbotButton.interactable = false;
        }
        MenuLoginButton?.gameObject.SetActive(true);
        CloseRegistrationPopup();

        HideErrorMessage();
    }

    void CloseRegistrationPopup()
    {
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(false);
        }
    }

    private bool ValidatePasswordComplexity(string password)
    {
        var hasUpperCase = false;
        var hasLowerCase = false;
        var hasDigits = false;
        var hasSpecialChar = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpperCase = true;
            if (char.IsLower(c)) hasLowerCase = true;
            if (char.IsDigit(c)) hasDigits = true;
            if (!char.IsLetterOrDigit(c)) hasSpecialChar = true;
        }

        return password.Length >= 8 && hasUpperCase && hasLowerCase && hasDigits && hasSpecialChar;
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

    [System.Serializable]
    private class RegistrationResponse
    {
        /*
        public bool isActive;
        public string sessionToken; // Added field for session token
        */


        public string message;
        public RegistrationUser user;
    }

    [System.Serializable]
    private class RegistrationUser
    {
        public int id;
        public string username;
        public string email;
        public bool isActive;
    }
}