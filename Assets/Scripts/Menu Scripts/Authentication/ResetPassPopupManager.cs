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
    public InputField emailInput;
    public UnityEngine.UI.Text errorMessageText;


    public void OnResetButtonClicked()
    {
        if (string.IsNullOrEmpty(emailInput.text))
        {
            errorMessageText.text = "All fields must be filled!";
            return;
        }

        // Start the reset coroutine
        // StartCoroutine(ResetUserPass(emailInput.text));
    }

    public void ShowRegiPopup()
    {
        CloseResetPassPopup();
        registrationPopup.SetActive(true);
    }

    public void ShowLoginPopup()
    {
        CloseResetPassPopup();
        loginPopup.SetActive(true);
    }

    public void CloseResetPassPopup()
    {
        resetPassPopup.SetActive(false);
    }
}
