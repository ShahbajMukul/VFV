using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

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

        // ToDo: Add backend register api call here

        UnityEngine.Debug.Log("Registration successful!");
        CloseRegistrationPopup();
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
