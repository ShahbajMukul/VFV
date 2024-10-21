using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Diagnostics;

public class ResetPassEnterCodePopupManager : MonoBehaviour
{
    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject resetPassEnterCodePopup;
    public GameObject resetPassPopup;

    public InputField recoveryCodeInput;
    public InputField passwordInput;
    public InputField confirmPasswordInput;
    public UnityEngine.UI.Text errorMessageText;


    public void ShowResetPopup()
    {
        if (resetPassEnterCodePopup != null)
        {
            resetPassEnterCodePopup.SetActive(true);
        }
        else
        {
            UnityEngine.Debug.LogError("resetPassEnterCodePopup is not assigned");
        }
    }

    public void OnNewPasswordSubmitButtonClicked()
    {
        if (string.IsNullOrEmpty(recoveryCodeInput.text) || 
            string.IsNullOrEmpty(passwordInput.text) || 
            string.IsNullOrEmpty(confirmPasswordInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            errorMessageText.gameObject.SetActive(true);
            UnityEngine.Debug.LogWarning("Validation failed: All fields must be filled.");
            return;
        }

        // StartCoroutine(SubmitUserPassResetReq(resetPassEnterCodePopup.text, passwordInput.text, confirmPasswordInput.text));
    }

    public void ShowRegiPopup()
    {
        CloseResetPassEnterCodePopup();
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void ShowLoginPopup()
    {
        CloseResetPassEnterCodePopup();
        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
        }
    }

    // If the user did not recieve the email, they can go back and try again
    public void ShowResetPassPopup()
    {
        CloseResetPassEnterCodePopup();
        if (resetPassPopup != null)
        {
            resetPassPopup.SetActive(true);
        }
    }

    public void CloseResetPassEnterCodePopup()
    {
        if (resetPassPopup != null)
        {
            resetPassEnterCodePopup.SetActive(false);
        }
    }

}
