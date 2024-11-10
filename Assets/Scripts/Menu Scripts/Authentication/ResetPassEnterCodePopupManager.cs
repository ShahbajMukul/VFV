using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;

public class ResetPassEnterCodePopupManager : MonoBehaviour
{
    public GameObject resetPassEnterCodePopup;
    public InputField recoveryCodeInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public Text errorMessageText;

    private string resetPasswordUrl = "https://storai.net/api/reset-pwd"; 


    void Start()
    {
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
        StartCoroutine(ResetPassword(recoveryCodeInput.text, passwordInput.text));
    }

    private IEnumerator ResetPassword(string resetCode, string newPassword)
    {
        string jsonData = $"{{\"resetCode\":\"{resetCode}\",\"newPassword\":\"{newPassword}\"}}";
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(resetPasswordUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Adding Cookie header to retain the session
            if (PlayerPrefs.HasKey("SessionCookie"))
            {
                string sessionCookie = PlayerPrefs.GetString("SessionCookie");
                www.SetRequestHeader("Cookie", sessionCookie);
            }

            Debug.Log("Sending reset password request with payload: " + jsonData);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP Error: " + www.error);
                Debug.LogError("Server Response: " + www.downloadHandler.text);
                ShowErrorMessage("Error: Not authorized. Please try again.");
            }
            else if (www.responseCode == 200)
            {
                Debug.Log("Password reset successful!");
                ShowErrorMessage("Password reset successful!");
                resetPassEnterCodePopup.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Unexpected response: " + www.responseCode);
                ShowErrorMessage($"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}");
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