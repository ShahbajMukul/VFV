using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;

public class LoginPopupManager : MonoBehaviour
{
    public GameObject loginPopup;
    public GameObject registrationPopup;
    public GameObject resetPassPopup;
    public InputField emailInput;
    public InputField passwordInput;
    public UnityEngine.UI.Text errorMessageText;

    private string loginUrl = "http://localhost:3000/api/login";

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            Debug.LogWarning("Validation failed: All fields must be filled.");
            return;
        }

        StartCoroutine(LoginUser(emailInput.text, passwordInput.text));
    }

    private IEnumerator LoginUser(string identifier, string password)
    {
        string jsonData;

        if (IsEmail(identifier))
        {
            // Send email as username field if it is an email
            jsonData = $"{{\"email\":\"{identifier}\",\"password\":\"{password}\"}}";
        }
        else
        {
            // Send username as identifier if it is not an email
            jsonData = $"{{\"username\":\"{identifier}\",\"password\":\"{password}\"}}";
        }

        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(loginUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("Sending login request with payload: " + jsonData);

            yield return www.SendWebRequest();

            Debug.Log("HTTP Response Code: " + www.responseCode);
            Debug.Log("HTTP Response Text: " + www.downloadHandler.text);

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP error received from server: " + www.error);
                Debug.LogError("Server Response: " + www.downloadHandler.text);

                // Handle incorrect credentials
                if (www.responseCode == 401)
                {
                    errorMessageText.text = "Incorrect email/username or password.";
                }
                else
                {
                    errorMessageText.text = "Login failed: " + www.downloadHandler.text;
                }
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                if (www.responseCode == 200) // Assuming a 200 OK response for successful login
                {
                    Debug.Log("Login successful!");
                    errorMessageText.text = "";
                    CloseLoginPopup();
                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    errorMessageText.text = $"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}";
                }
            }
        }
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
        if (loginPopup != null)
        {
            loginPopup.SetActive(false);
        }
    }
}