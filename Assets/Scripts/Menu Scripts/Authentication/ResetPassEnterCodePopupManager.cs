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

    private string resetPasswordUrl = "http://localhost:3000/api/reset-pwd";

    
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
            errorMessageText.text = "All fields must be filled!";
            errorMessageText.gameObject.SetActive(true);
            Debug.LogWarning("Validation failed: Some fields are empty.");
            return;
        }

        
        if (!ValidatePassword(passwordInput.text))
        {
            errorMessageText.text = "Password must be at least 8 characters, include at least one uppercase letter, one symbol, and one number.";
            errorMessageText.gameObject.SetActive(true);
            Debug.LogWarning("Validation failed: Password does not meet requirements.");
            return;
        }

        
        if (passwordInput.text != confirmPasswordInput.text)
        {
            errorMessageText.text = "Passwords do not match!";
            errorMessageText.gameObject.SetActive(true);
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

            Debug.Log("Sending reset password request with payload: " + jsonData);
            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError("HTTP Error: " + www.error);
                errorMessageText.text = "Error: Unable to process the request.";
                errorMessageText.gameObject.SetActive(true);
            }
            else if (www.responseCode == 200)
            {
                Debug.Log("Password reset successful!");
                errorMessageText.text = "Password reset successful!";
                errorMessageText.gameObject.SetActive(true);

                // Close the ResetPassEnterCodePopup after a successful password reset
                resetPassEnterCodePopup.SetActive(false);
            }
            else
            {
                Debug.LogWarning("Unexpected response: " + www.responseCode);
                errorMessageText.text = $"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}";
                errorMessageText.gameObject.SetActive(true);
            }
        }
    }
}