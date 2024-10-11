using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class LoginPopupManager : MonoBehaviour
{
    public GameObject loginPopup;
    public GameObject RegistrationPopup;
    // add reset password parameter later
    public InputField emailInput;
    public InputField passwordInput;
    public UnityEngine.UI.Text errorMessageText;

    public void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text) ||
            string.IsNullOrEmpty(passwordInput.text  ))
        {
            errorMessageText.text = "All fields must be filled!";
            return;
        }

        // ToDo: Add Login api call
        UnityEngine.Debug.Log("Login successfull!");
        CloseLoginPopup();
    }

    public void CloseLoginPopup()
    {
        loginPopup.SetActive(false);
    }

    public void ShowRegiPopup()
    {
        CloseLoginPopup();
        RegistrationPopup.SetActive(true);
    }

    public void ShowResetPopup()
    {
        // hide login popup
        // Show Reset popup later 
    }
}
