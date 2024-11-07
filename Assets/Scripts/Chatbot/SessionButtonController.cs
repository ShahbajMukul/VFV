using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class SessionButtonController : MonoBehaviour
{
    public ChatbotManager chatbotManager;
    public Button deleteButton;
    private string sessionName;

    public void Initialize(ChatbotManager manager, string session)
    {
        chatbotManager = manager;
        sessionName = session;

        // Assign the delete action
        deleteButton.onClick.AddListener(() => chatbotManager.DeleteSessionByName(sessionName));
    }

    public void SetActiveColor()
    {
        // Set this button to green
        GetComponent<UnityEngine.UI.Image>().color = Color.green;

        // Reset other session buttons via ChatbotManager
        chatbotManager.ResetOtherSessionButtonColors(this);
    }

    public void SetDefaultColor()
    {
        // Reset this button's color to default (e.g., white)
        GetComponent<UnityEngine.UI.Image>().color = Color.white;
    }
}
