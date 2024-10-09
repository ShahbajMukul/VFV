//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ChatbotUIHandler : MonoBehaviour
//{
//    public GameObject fullscreenToggle;
//    public GameObject chatbotPanel;

//    // Start is called before the first frame update
//    private void Start()
//    {
//        // Minimizing the Options Panel
//        GameObject.Find("ChatbotPanel").transform.localScale = new Vector3(0, 0, 0);

//        //initializes fullscreen toggle
//        fullscreenToggle = GameObject.Find("FSToggle");
//        if (Screen.fullScreen)
//        {
//            fullscreenToggle.GetComponent<Toggle>().isOn = true;
//        }
//        else
//        {
//            fullscreenToggle.GetComponent<Toggle>().isOn = false;
//        }
//        /**/
//    }

//    public void ToggleFullScreen()
//    {
//        Screen.fullScreen = !Screen.fullScreen;
//    }
//    public void ShowChatbotPanel()
//    {
//        GameObject.Find("ChatbotPanel").transform.localScale = new Vector3(3, 3, 1);
//    }
//    public void HideChatbotPanel()
//    {
//        GameObject.Find("ChatbotPanel").transform.localScale = new Vector3(0, 0, 0);
//    }

//}


