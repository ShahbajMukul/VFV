using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ReqRegiPopUpManager : MonoBehaviour
{
    public InputField usernameInput;
    public InputField userEmailInput;

    public Button ReqButton;
    public Button CtnuWOButton;

    public Button openRegCodeEnterButton;
    public Button panelLoginButton;

    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject reqRegiPopup;
    public GameObject resetPassPopup;
    public Text errorMessageText;


    // url for req access code
    private string reqAccessCodeUrl = "https://storai.net/email/RegReqEmail";


    void Start()
    {
        errorMessageText.text = "";
        panelLoginButton.gameObject.SetActive(false);
    }



    public void OnReqButtonClicked()
    {
        errorMessageText.text = "Processing Request. Please wait...";

        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(userEmailInput.text))
        {
            errorMessageText.text = "Having issues. Please check the logs!";
            Debug.Log("Could not derive the username or email from the last window");
        }


        Debug.Log($"Attempting to send registration request with username: {usernameInput.text}, email: {userEmailInput.text}");

        StartCoroutine(SendRegReqEmail(usernameInput.text, userEmailInput.text));
    }

    private IEnumerator SendRegReqEmail(string username, string email)
    {
        // Build JSON payload
        RegistrationRequest payload = new RegistrationRequest
        {
            username = username,
            email = email
        };

        string jsonData = JsonUtility.ToJson(payload);
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(reqAccessCodeUrl, "POST"))
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
                errorMessageText.text = "Request failed: " + www.downloadHandler.text;
            }
            else
            {
                Debug.Log("Server connected successfully! Response: " + www.downloadHandler.text);

                EmailResponse response = JsonUtility.FromJson<EmailResponse>(www.downloadHandler.text);
                errorMessageText.text = response.message;

                if (www.responseCode == 200)
                {
                    Debug.Log("Request successful!");
                    errorMessageText.text = "Request successful. You can close this window now.";

                    ReqButton.interactable = false;
                    CtnuWOButton.interactable = false;

                    openRegCodeEnterButton.gameObject.SetActive(true);

                }
                else
                {
                    Debug.LogWarning("Unexpected response code: " + www.responseCode);
                    errorMessageText.text = $"Unexpected response from server: {www.responseCode} - {www.downloadHandler.text}";
                }
            }
        }
    }

    public void ShowRegiPopup()
    {
        ClosereqRegiPopup();
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void ShowLoginPopup()
    {
        ClosereqRegiPopup();
        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
        }
    }

    public void ClosereqRegiPopup()
    {
        if (reqRegiPopup != null)
        {
            reqRegiPopup.SetActive(false);
        }
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(false);
        }

    }

    public void ShowresetPassPopup()
    {
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(true);
            Debug.Log("resetPassPopup opened.");
        }
        else
        {
            Debug.LogError("resetPassPopup is not assigned");
        }
    }




    [System.Serializable]
    public class RegistrationRequest
    {
        public string username;
        public string email;
    }

    [System.Serializable]
    public class EmailResponse
    {
        public string message;
    }
}
