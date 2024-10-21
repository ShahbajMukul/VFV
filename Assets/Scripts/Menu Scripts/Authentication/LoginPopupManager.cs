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
    public InputField usernameInput;
    public InputField passwordInput;
    public UnityEngine.UI.Text errorMessageText;
    public UnityEngine.UI.Text loginStatusMsgLabel;

    private string loginUrl = "http://localhost:3000/api/login";
    private string checkSessionUrl = "http://localhost:3000/api/check-session";

    void Start()
    {
        CheckLoginState();
    }

    void CheckLoginState()
    {
        StartCoroutine(CheckSession());
    }

    private IEnumerator CheckSession()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(checkSessionUrl))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Cookie", PlayerPrefs.GetString("SessionCookie", ""));

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("Error checking session: " + www.error);

                // Show login popup if session check fails
                if (loginPopup != null)
                {
                    loginPopup.SetActive(true);
                }
                if (loginStatusMsgLabel != null)
                {
                    loginStatusMsgLabel.text = "";
                }
            }
            else
            {
                if (www.responseCode == 200)
                {
                    Debug.Log("User already logged in.");
                    if (loginStatusMsgLabel != null)
                    {
                        loginStatusMsgLabel.text = "You are already logged in.";
                    }

                    // Hide login popup and show the default menu buttons
                    if (loginPopup != null)
                    {
                        loginPopup.SetActive(false);
                    }

                    // We don't want to show the user the chatbot right after logging in. If they want to use it, they would need to click the button
                    //if (ChatPopupPanel != null)
                    //{
                    //    ChatPopupPanel.SetActive(true);
                    //}
                }
                else
                {
                    // Show login popup if session is not valid
                    if (loginPopup != null)
                    {
                        loginPopup.SetActive(true);
                    }
                    if (loginStatusMsgLabel != null)
                    {
                        loginStatusMsgLabel.text = "";
                    }
                }
            }
        }
    }

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            Debug.LogWarning("Validation failed: All fields must be filled.");
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

            Debug.Log("Sending login request with payload: " + jsonData);

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError("HTTP error received from server: " + www.error);
                Debug.LogError("Server Response: " + www.downloadHandler.text);

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

                if (www.responseCode == 200)
                {
                    Debug.Log("Login successful!");

                    // Save the session cookie from the response headers
                    string setCookieHeader = www.GetResponseHeader("Set-Cookie");
                    if (!string.IsNullOrEmpty(setCookieHeader))
                    {
                        PlayerPrefs.SetString("SessionCookie", setCookieHeader);
                        PlayerPrefs.Save();
                    }

                    errorMessageText.text = "Login successful!"; // Show success message on screen

                    // Hide login popup and show chat panel after successful login
                    if (loginPopup != null)
                    {
                        loginPopup.SetActive(false);
                    }
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