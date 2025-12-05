using UnityEngine;
using UnityEngine.UI; // Wichtig für Buttons
using Unity.Netcode;  // Wichtig für Multiplayer

public class NetworkUI : MonoBehaviour
{
    public Button hostButton;
    public Button clientButton;

    void Start()
    {
        // Wenn man auf Host klickt...
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
            HideButtons();
        });

        // Wenn man auf Client klickt...
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            HideButtons();
        });
    }

    void HideButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }
}