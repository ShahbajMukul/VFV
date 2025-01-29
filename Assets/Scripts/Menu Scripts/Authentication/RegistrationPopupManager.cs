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

    private string registrationUrl = "http://localhost:3000/api/register-optional";
    private string sessionTokenFilePath;

    void Start()
    {
        // Show LoginPanel by default
        // HideErrorMessage();
        // Set the file path for saving the session token
        sessionTokenFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");

        // ToDo: Abdullah, this is now conflicting with the login popup when the game first launches.
        // Implement a method or flag so only one or the other shows up rather than overlapping.

        // Check if a session token exists
        string savedSessionToken = LoadSessionToken();
        if (!string.IsNullOrEmpty(savedSessionToken))
        {
            Debug.Log("Session token found, redirecting to Request Code UI.");
            ShowReqRegistrationCodePopup();
        }
        else
        {
            Debug.Log("No session token found, showing registration UI.");
            ShowRegistrationPopup();
        }
    }

    public void ShowRegistrationPopup()
    {
        if (loginPopup != null)
        {
            loginPopup.SetActive(false);
        }

        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }

        //HideErrorMessage();
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

                    var response = JsonUtility.FromJson<RegistrationResponse>(www.downloadHandler.text);

                    if (response.isActive)
                    {
                        ShowLoginPopup();
                    }
                    else
                    {
                        Debug.Log("User is inactive. Saving session token for future validation.");
                        SaveSessionToken(response.sessionToken); // Save session token for inactive users
                        ShowReqRegistrationCodePopup();
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

    private void SaveSessionToken(string sessionToken)
    {
        try
        {
            File.WriteAllText(sessionTokenFilePath, sessionToken);
            Debug.Log("Session token saved successfully to: " + sessionTokenFilePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving session token: " + ex.Message);
        }
    }

    private string LoadSessionToken()
    {
        try
        {
            if (File.Exists(sessionTokenFilePath))
            {
                string token = File.ReadAllText(sessionTokenFilePath);
                Debug.Log("Session token loaded successfully: " + token);
                return token;
            }
            else
            {
                Debug.LogWarning("Session token file does not exist.");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error loading session token: " + ex.Message);
            return null;
        }
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

    public void ShowLoginPopup()
    {
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(false);
        }

        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
        }
    }

    public void ShowReqRegistrationCodePopup()
    {
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(false);
        }

        if (reqRegistrationCodePopup != null)
        {
            reqRegistrationCodePopup.SetActive(true);
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

    private class RegistrationResponse
    {
        public bool isActive;
        public string sessionToken; // Added field for session token
    }
}