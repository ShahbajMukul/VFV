using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class RegistrationPopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public InputField emailInput;
    public InputField usernameInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public InputField registrationCodeInput;
    public UnityEngine.UI.Text errorMessageText;
    public Button ChatbotButton;


    private string registrationUrl = "http://localhost:3000/api/register";

    void Start()
    {
        // show LoginPanel by default (done)
    }

    public void ShowRegistrationPopup()
    {

        // Close other popups if open
        if (loginPopup != null)
        {
            loginPopup.SetActive(false);
        }

        // Show registration popup
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void OnRegisterButtonClicked()
    {


        if (string.IsNullOrEmpty(emailInput.text) ||
            string.IsNullOrEmpty(usernameInput.text) ||
            string.IsNullOrEmpty(passwordInput.text) ||
            string.IsNullOrEmpty(confirmPasswordInput.text) ||
            string.IsNullOrEmpty(registrationCodeInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            return;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            errorMessageText.text = "Passwords do not match!";
            return;
        }

        if (!ValidatePasswordComplexity(passwordInput.text))
        {
            errorMessageText.text = "Password must be at least 8 characters long, include at least one uppercase letter, one symbol, and one number.";
            Debug.LogWarning("Validation failed: Password does not meet complexity requirements.");
            return;
        }

        Debug.Log($"Attempting to register with email: {emailInput.text}, username: {usernameInput.text}, password: {passwordInput.text}, secretCode: {registrationCodeInput.text}");

        StartCoroutine(RegisterUser(emailInput.text, usernameInput.text, passwordInput.text, registrationCodeInput.text));
    }

    private IEnumerator RegisterUser(string email, string username, string password, string registrationCode)
    {
        string jsonData = $"{{\"email\":\"{email}\",\"username\":\"{username}\",\"password\":\"{password}\",\"secretCode\":\"{registrationCode}\"}}";
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
                errorMessageText.text = "Registration failed: " + www.downloadHandler.text;
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                if (www.responseCode == 201)
                {
                    //// ToDo: Save user info in game so that they don't have to relogin everytime
                    Debug.Log("Registration successful!");
                    CloseRegistrationPopup();
                    ShowLoginPopup();
                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    errorMessageText.text = $"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}";
                }
            }
        }
    }

    public void OnCloseButtonClicked()
    {
        if (ChatbotButton != null)
        {
            ChatbotButton.interactable = false;  // Disable the chatbot button
        }
        CloseRegistrationPopup();
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

        // Close registration popup
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(false);
        }

        // Open login popup
        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
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
}