using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
            return;
        }

        // Start the login coroutine
        StartCoroutine(LoginUser(emailInput.text, passwordInput.text));
    }

    private IEnumerator LoginUser(string email, string password)
    {
        // Create the JSON payload manually
        string jsonData = JsonUtility.ToJson(new
        {
            username = email,  // The backend uses 'username' field for login (email in your case)
            password = password
        });

        // Create a UnityWebRequest to POST the JSON data
        UnityWebRequest request = new UnityWebRequest(loginUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        
        if (request.isNetworkError || request.isHttpError)
        {
            Debug.LogError("Login failed: " + request.downloadHandler.text);
            errorMessageText.text = "Login failed: " + request.error;
        }
        else
        {
            Debug.Log("Login successful!");
            errorMessageText.text = "";
            CloseLoginPopup();
        }
    }

    public void ShowRegiPopup()
    {
        CloseLoginPopup();
        registrationPopup.SetActive(true);
    }

    public void ShowResetPopup()
    {
        CloseLoginPopup();
        resetPassPopup.SetActive(true);
    }

    public void CloseLoginPopup()
    {
        loginPopup.SetActive(false);
    }


}
