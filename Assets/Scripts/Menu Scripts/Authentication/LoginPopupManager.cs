using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

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


    private string loginUrl = "http://localhost:3000/api/login";
    // private string checkSessionUrl = "http://localhost:3000/api/check-session";

    void Start()
    {
        // Check if a session token is stored locally
        if (PlayerPrefs.HasKey("SessionToken"))
        {
            UnityEngine.Debug.Log("Stored session token found. Skipping login...");
            loginPopup.SetActive(false);
            MenuLogoutButton?.gameObject.SetActive(true);
            // loginStatusMsgLabel.text = "You are already logged in.";
        }
        else
        {
            loginPopup.SetActive(true);  // Show login popup if no token is stored
        }
    }

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
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
                errorMessageText.text = "Login failed: " + www.downloadHandler.text;
            }
            else
            {
                if (www.responseCode == 200)
                {
                    // On successful login, save the session token
                    string sessionToken = www.GetResponseHeader("Set-Cookie");
                    if (!string.IsNullOrEmpty(sessionToken))
                    {
                        PlayerPrefs.SetString("SessionToken", sessionToken);
                        PlayerPrefs.Save();

                        UnityEngine.Debug.Log("Login successful! Session token saved locally.");
                        errorMessageText.text = "Login successful!";
                        loginPopup.SetActive(false);
                        ChatbotButton.gameObject.SetActive(true);
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
                }
            }
        }
    }



    public void Logout()
    {
        // Clear the stored session token
        PlayerPrefs.DeleteKey("SessionToken");
        loginPopup.SetActive(true);
        loginStatusMsgLabel.text = "You have been logged out.";
        UnityEngine.Debug.Log("Session token cleared. User logged out.");
        MenuLoginButton.gameObject.SetActive(true);
        MenuLogoutButton.gameObject.SetActive(false);


    }

    private bool IsEmail(string input)
    {
        return input.Contains("@") && input.Contains(".");
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
            ChatbotButton.interactable = false;  // Disable the chatbot button
        }
        MenuLoginButton.gameObject.SetActive(true);
        MenuLogoutButton.gameObject.SetActive(false);

        loginPopup?.SetActive(false);

    }
}
