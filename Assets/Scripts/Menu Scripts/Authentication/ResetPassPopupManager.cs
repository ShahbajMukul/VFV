using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class ResetPassPopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject resetPassPopup;
    public GameObject resetPassEnterCodePopup;
    public InputField emailInput;
    public UnityEngine.UI.Text errorMessageText;

    private string resetPasswordUrl = "http://localhost:3000/api/forgot-password";

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
    }

    public void OnResetButtonClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            errorMessageText.gameObject.SetActive(true); 
            Debug.LogWarning("Validation failed: All fields must be filled.");
            return;
        }

        StartCoroutine(SubmitResetPassReq(emailInput.text));
    }

    private IEnumerator SubmitResetPassReq(string email)
    {
        Debug.Log("SubmitResetPassReq coroutine started for email: " + email);

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
                errorMessageText.text = "Error: Unable to process the request.";
                errorMessageText.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                if (www.responseCode == 200)
                {
                    if (www.downloadHandler.text.Contains("Reset code sent successfully"))
                    {
                        Debug.Log("Reset code sent successfully!");
                        errorMessageText.text = "Reset code sent successfully to your email.";
                        // Let's just take the user to the new password page
                        CloseResetPassPopup();
                        resetPassEnterCodePopup.SetActive(true);
                    }
                    else if (www.downloadHandler.text.Contains("No user found for email"))
                    {
                        Debug.Log("Email not found in the system.");
                        errorMessageText.text = "The email address you entered is not registered.";
                    }
                    else
                    {
                        errorMessageText.text = "A reset code will be sent if the email is registered";
                    }
                }
                else if (www.responseCode == 404)
                {
                    Debug.LogWarning("No user found with that email.");
                    errorMessageText.text = "No user found with the provided email address.";
                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    errorMessageText.text = $"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}";
                }

                errorMessageText.gameObject.SetActive(true);
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
    }
}