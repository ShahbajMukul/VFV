using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System;
using System.Text.RegularExpressions;

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

    void Start()
    {
        string sessionToken = PlayerPrefs.GetString("SessionToken", string.Empty);
        if (!string.IsNullOrEmpty(sessionToken))
        {
            UnityEngine.Debug.Log("Stored session token found. Skipping login...");
            
        }
        else
        {
            UnityEngine.Debug.LogWarning("No session token found, prompting user to log in.");

        }
        // Check if a session token is stored locally
        if (PlayerPrefs.HasKey("SessionToken"))
        {

            UnityEngine.Debug.Log("Stored session token found. Skipping login...");
            loginPopup.SetActive(false);
            MenuLogoutButton?.gameObject.SetActive(true);
            MenuLoginButton?.gameObject.SetActive(false);

            // loginStatusMsgLabel.text = "You are already logged in.";
        }
        else
        {
            loginPopup.SetActive(true);  // Show login popup if no token is stored
        }

        if (!string.IsNullOrEmpty(sessionToken))
        {
            PlayerPrefs.SetString("SessionToken", sessionToken);
            PlayerPrefs.Save();  // Save PlayerPrefs to ensure it persists
            UnityEngine.Debug.Log("Session token saved: " + sessionToken);
        }
        else
        {
            UnityEngine.Debug.LogWarning("No session token found in response.");
        }
        // Hide error message and panel at start
        HideErrorMessage();
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
            www.GetResponseHeader("Set-Cookie");
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

                        // Parse the username from the response
                        string jsonResponse = www.downloadHandler.text;
                        LoginResponse loginResponse = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                        string username = loginResponse.username;
                        if (string.IsNullOrEmpty(username))
                        {
                            // If username is still null, and identifier was a username, use it
                            if (!IsEmail(identifier))
                            {
                                username = identifier;
                            }
                            else
                            {
                                // Handle the case where the email was used to login, but username is not returned
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
    }

    public void Logout()
    {
        // Clear the stored session token and username
        PlayerPrefs.DeleteKey("SessionToken");
        PlayerPrefs.DeleteKey("LoggedInUsername");
        PlayerPrefs.Save();

        loginPopup.SetActive(true);
        loginStatusMsgLabel.text = "You have been logged out.";
        UnityEngine.Debug.Log("Session token and username cleared. User logged out.");
        MenuLoginButton.gameObject.SetActive(true);
        MenuLogoutButton.gameObject.SetActive(false);
    }

    private bool IsEmail(string input)
    {
        // Simple email validation
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
            ChatbotButton.interactable = false;  // Disable the chatbot button
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
    // Added to parse the login response
    [Serializable]
    private class LoginResponse
    {
        public string username;
    }
}