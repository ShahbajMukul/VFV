using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RegCodeEnterPopupManager : MonoBehaviour
{

    public InputField regiCodeInput;

    public Button openRegCodeEnterPopupButton;
    public Button activateAccButton;
    public Button chatbotButton;


    public GameObject regCodeEnterPopup;
    public Text errorMessageText;

    private string regCodeEnterUrl = "http://localhost:3000/api/enter-code";


    // Start is called before the first frame update
    void Start()
    {
        errorMessageText.text = "";
    }


    public void OnActivateButtonClicked()
    {
        // verify the entered data

        // strat coroutine

        // testing
        openRegCodeEnterPopupButton.gameObject.SetActive(false);
        chatbotButton.gameObject.SetActive(true);
        chatbotButton.interactable = true;
        CloseRegiCodeEnterPopup();
        // end test
    }

    /// Coroutine

    // handle all cases gracefully

    // if status code 401 show internal error and they may need to logout and create a new account in errorMessageText. log actual reason
    // openRegCodeEnterPopupButton.SetActive(false);

    // 200 = user is active and should have access to StorAI
    //openRegCodeEnterPopupButton.gameObject.SetActive(false);
    //chatbotButton.gameObject.SetActive(true);
    //chatbotButton.interactable = true;
    //CloseRegiCodeEnterPopup();
    // save the username using PlayerPrefs for LoggedInUsername for StorAI access in game



    public void ShowRegiCodeEnterPopup()
    {
        if (regCodeEnterPopup != null)
        {
            regCodeEnterPopup.SetActive(true);
        }
    }

    public void CloseRegiCodeEnterPopup()
    {
        if (regCodeEnterPopup != null)
        {
            regCodeEnterPopup.SetActive(false);
        }
    }

}
