using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;

public class RegistrationPopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public InputField emailInput;
    public InputField usernameInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public InputField registrationCodeInput;
    public UnityEngine.UI.Text errorMessageText;

    private string registrationUrl = "http://localhost:3000/api/register"; 


    void Start()
    {
        ShowRegistrationPopup();
    }

    public void ShowRegistrationPopup()
    {
        registrationPopup.SetActive(true);
    }

    public void OnRegisterButtonClicked()
    {
        // Validation
        if (string.IsNullOrEmpty(emailInput.text) ||
            string.IsNullOrEmpty(usernameInput.text) ||
            string.IsNullOrEmpty(passwordInput.text) ||
            string.IsNullOrEmpty(confirmPasswordInput.text) ||
            string.IsNullOrEmpty(registrationCodeInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            return;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            errorMessageText.text = "Passwords do not match!";
            return;
        }

        // Debug log to check email and password before sending request
        UnityEngine.Debug.Log($"Attempting to register with email: {emailInput.text}, password: {passwordInput.text}");

        StartCoroutine(RegisterUser(emailInput.text, usernameInput.text, passwordInput.text, registrationCodeInput.text));
    }

    private IEnumerator RegisterUser(string email, string username, string password, string registrationCode)
    {
        
        WWWForm form = new WWWForm();
        form.AddField("email", email);
        form.AddField("username", username);
        form.AddField("password", password);
        form.AddField("secretCode", registrationCode);

        UnityWebRequest request = UnityWebRequest.Post(registrationUrl, form);
        yield return request.SendWebRequest();

        if (request.isNetworkError || request.isHttpError)
        {
            errorMessageText.text = "Registration failed: " + request.error;
            UnityEngine.Debug.LogError("Registration failed: " + request.downloadHandler.text);
        }
        else
        {
            UnityEngine.Debug.Log("Registration successful!");
            CloseRegistrationPopup();
            ShowLoginPopup();
        }
    }

    public void OnCancelButtonClicked()
    {
        CloseRegistrationPopup();
    }

    void CloseRegistrationPopup()
    {
        registrationPopup.SetActive(false);
    }

    public void ShowLoginPopup()
    {
        CloseRegistrationPopup();
        loginPopup.SetActive(true);
    }
}