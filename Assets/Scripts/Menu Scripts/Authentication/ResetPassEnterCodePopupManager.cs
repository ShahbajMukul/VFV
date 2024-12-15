using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

public class ResetPassEnterCodePopupManager : MonoBehaviour
{
    public GameObject resetPassEnterCodePopup;
    public InputField emailInput;
    public InputField recoveryCodeInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public Text errorMessageText;

    private string resetPasswordUrl = "https://storai.net/api/reset-pwd";
    private string verifyCodeUrl = "https://storai.net/api/verify-code";
    private string sessionFilePath;

    void Start()
    {
        // Path for retriving session token
        sessionFilePath = Path.Combine(Application.persistentDataPath, "sessionToken.txt");

        // Hide error message and panel at start
        HideErrorMessage();
    }

    private bool ValidatePassword(string password)
    {
        // Password must be at least 8 characters long, with at least one uppercase letter, one symbol, and one number
        string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$";
        return Regex.IsMatch(password, pattern);
    }

    public void OnNewPasswordSubmitButtonClicked()
    {
        if (resetPassEnterCodePopup == null)
        {
            Debug.LogError("ResetPassEnterCodePopup panel is not properly assigned in the inspector!");
            return;
        }

        if (string.IsNullOrEmpty(recoveryCodeInput.text) ||
            string.IsNullOrEmpty(passwordInput.text) ||
            string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            ShowErrorMessage("All fields must be filled!");
            Debug.LogWarning("Validation failed: Some fields are empty.");
            return;
        }

        if (!ValidatePassword(passwordInput.text))
        {
            ShowErrorMessage("Password must be at least 8 characters, include at least one uppercase letter, one symbol, and one number.");
            Debug.LogWarning("Validation failed: Password does not meet requirements.");
            return;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowErrorMessage("Passwords do not match!");
            Debug.LogWarning("Validation failed: Passwords do not match.");
            return;
        }

        Debug.Log("New Password Submit Button Clicked: Sending reset password request...");
        StartCoroutine(VerifyResetCode(recoveryCodeInput.text));
    }

    private IEnumerator VerifyResetCode(string resetCode)
    {
        string jsonData = $"{{\"email\":\"{emailInput.text}\",\"resetCode\":\"{resetCode}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        Debug.Log($"Sending verification request with email: {emailInput.text} and code: {resetCode}");

        using (UnityWebRequest www = new UnityWebRequest(verifyCodeUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            Debug.Log($"Server Response: {www.downloadHandler.text}");
            Debug.Log($"Response Code: {www.responseCode}");

            if (www.responseCode == 200)
            {
                var cookieHeader = www.GetResponseHeader("Set-Cookie");
                Debug.Log($"Received cookie: {cookieHeader}");

                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    File.WriteAllText(sessionFilePath, cookieHeader);
                    StartCoroutine(UpdatePassword(passwordInput.text));
                }
                else
                {
                    Debug.LogError("No session cookie received from server");
                    ShowErrorMessage("Server error: No session established");
                }
            }
            else
            {
                string errorMessage = www.downloadHandler.text;
                Debug.LogError($"Verify code failed. Status: {www.responseCode}, Error: {errorMessage}");
                ShowErrorMessage($"Verification failed: {errorMessage}");
            }
        }
    }

    private IEnumerator UpdatePassword(string newPassword)
    {
        string jsonData = $"{{\"newPassword\":\"{newPassword}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(resetPasswordUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            if (File.Exists(sessionFilePath))
            {
                string sessionToken = File.ReadAllText(sessionFilePath);
                www.SetRequestHeader("Cookie", sessionToken);
            }

            yield return www.SendWebRequest();

            if (www.responseCode == 200)
            {
                Debug.Log("Password reset successful!");
                ShowErrorMessage("Password reset successful! Please close this window and login with your username and password!");
                File.Delete(sessionFilePath);
            }
            else
            {
                Debug.LogError($"Password update failed: {www.downloadHandler.text}");
                ShowErrorMessage("Failed to update password. Please try again.");
            }
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
}
