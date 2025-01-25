using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReqRegiPopUpManager : MonoBehaviour
{

    public GameObject registrationPopup;
    public GameObject loginPopup;
    public GameObject ReqRegiPopup;
    public GameObject resetPassEnterCodePopup;
    public UnityEngine.UI.Text errorMessageText;


    // url for req access code

    void Start()
    {

    }



    public void OnReqButtonClicked()
    {

        // send the req
    }

    
    public void ShowRegiPopup()
    {
        CloseReqRegiPopup();
        if (registrationPopup != null)
        {
            registrationPopup.SetActive(true);
        }
    }

    public void ShowLoginPopup()
    {
        CloseReqRegiPopup();
        if (loginPopup != null)
        {
            loginPopup.SetActive(true);
        }
    }

    public void CloseReqRegiPopup()
    {
        if (ReqRegiPopup != null)
        {
            ReqRegiPopup.SetActive(false);
        }
        if (resetPassEnterCodePopup != null)
        {
            resetPassEnterCodePopup.SetActive(false);
        }
    }

    public void ShowResetPassEnterCodePopup()
    {
        if (resetPassEnterCodePopup != null)
        {
            resetPassEnterCodePopup.SetActive(true);
            Debug.Log("ResetPassEnterCodePopup opened.");
        }
        else
        {
            Debug.LogError("resetPassEnterCodePopup is not assigned");
        }
    }

    private void ShowErrorMessage(string message)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
    }

    private void HideErrorMessage()
    {
        errorMessageText.text = "";
        errorMessageText.gameObject.SetActive(false);
    }
}
