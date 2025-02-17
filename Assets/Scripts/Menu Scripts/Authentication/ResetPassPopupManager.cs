using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System;

public class ResetPassPopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject resetPassPopup;
    public GameObject resetPassEnterCodePopup;
    public InputField emailInput;
    public UnityEngine.UI.Text errorMessageText;


    private string resetPasswordUrl = "https://storai.net/api/forgot-password";
    private string sessionFilePath;

    void Start()
    {
        // Path for saving session token
        sessionFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");
        HideErrorMessage();
    }

    public void ShowResetPopup()
    {
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("resetPassPopup is not assigned");
        }
        // Hide error message and panel at start
        HideErrorMessage();
    }

    public void OnResetButtonClicked()
    {
        ShowErrorMessage("Processing. Please wait...");
        if (string.IsNullOrEmpty(emailInput.text))
        {
            ShowErrorMessage("All fields must be filled!");
            Debug.LogWarning("Validation failed: All fields must be filled.");
            return;
        }

        if (!IsValidEmail(emailInput.text))
        {
            ShowErrorMessage("Please enter a valid email address!");
            Debug.LogWarning("Validation failed: Invalid email address.");
            return;
        }

        StartCoroutine(ResetUserPass(emailInput.text));
    }

    private IEnumerator ResetUserPass(string email)
    {
        Debug.Log("ResetUserPass coroutine started for email: " + email);

        string jsonData = $"{{\"email\":\"{email}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(resetPasswordUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending password reset request with payload: " + jsonData);

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP error received from server: " + www.error);
                Debug.LogError("Server Response: " + www.downloadHandler.text);
                ShowErrorMessage("Error: Unable to process the request.");
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                if (www.responseCode == 200)
                {
                    // Debug cookie headers
                    Debug.Log("All response headers:");
                    foreach (var header in www.GetResponseHeaders())
                    {
                        Debug.Log($"Header: {header.Key}: {header.Value}");
                    }

                    // Check for Set-Cookie header (case-insensitive)
                    string cookieHeader = null;
                    foreach (var header in www.GetResponseHeaders())
                    {
                        if (string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase))
                        {
                            cookieHeader = header.Value;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(cookieHeader))
                    {
                        try
                        {
                            Debug.Log($"Attempting to save cookie to: {sessionFilePath}");
                            // Ensure directory exists
                            string directory = Path.GetDirectoryName(sessionFilePath);
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }

                            // Extract connect.sid value
                            var match = Regex.Match(cookieHeader, @"connect\.sid=([^;]+)");
                            if (match.Success)
                            {
                                string sessionId = match.Groups[1].Value;
                                File.WriteAllText(sessionFilePath, $"connect.sid={sessionId}");
                                Debug.Log("Session cookie saved successfully");
                            }
                            else
                            {
                                Debug.LogError("Cookie format not recognized: " + cookieHeader);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to save session cookie: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No Set-Cookie header found in response");
                    }

                    if (www.downloadHandler.text.Contains("Reset code sent successfully"))
                    {
                        Debug.Log("Reset code sent successfully!");
                        CloseResetPassPopup();
                        ShowErrorMessage("Processing...");
                        ShowResetPassEnterCodePopup();
                    }
                    else if (www.downloadHandler.text.Contains("No user found for email"))
                    {
                        Debug.Log("Email not found in the system.");
                        ShowErrorMessage("The email address you entered is not registered.");
                    }
                    else
                    {
                        CloseResetPassPopup();
                        ShowResetPassEnterCodePopup();
                        ShowErrorMessage("A reset code will be sent if the email is registered.");
                    }
                }
                else if (www.responseCode == 404)
                {
                    Debug.LogWarning("No user found with that email.");
                    ShowErrorMessage("No user found with the provided email address.");
                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    ShowErrorMessage($"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}");
                }
            }
        }
    }

    public void ShowRegiPopup()
    {
        CloseResetPassPopup();
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void ShowLoginPopup()
    {
        CloseResetPassPopup();
        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
        }
    }

    public void CloseResetPassPopup()
    {
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(false);
        }
        if (resetPassEnterCodePopup != null)
        {
            resetPassEnterCodePopup.SetActive(false);
        }
    }

    public void ShowResetPassEnterCodePopup()
    {
        if (resetPassEnterCodePopup != null)
        {
            resetPassEnterCodePopup.SetActive(true);
            Debug.Log("ResetPassEnterCodePopup opened.");
        }
        else
        {
            Debug.LogError("resetPassEnterCodePopup is not assigned");
        }
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

    private bool IsValidEmail(string email)
    {
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }
}