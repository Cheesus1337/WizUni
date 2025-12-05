using UnityEngine;
using TMPro;
using Unity.Netcode; // Wichtig!

// ÄNDERUNG: Wir erben von NetworkBehaviour, nicht mehr MonoBehaviour
public class CardDisplay : NetworkBehaviour
{
    public SpriteRenderer backgroundRenderer;
    public TMP_Text valueText;
    public NetworkVariable<CardData> netCardData;

    // Diese Funktion wird automatisch aufgerufen, wenn das Objekt im Netzwerk erscheint
    public override void OnNetworkSpawn()
    {
        // Wir abonnieren Änderungen. Wenn sich der Wert ändert, ruf UpdateVisuals auf.
        netCardData.OnValueChanged += OnCardDataChanged;

        // Einmal initial aufrufen, damit die Startwerte angezeigt werden
        UpdateVisuals(netCardData.Value);
    }

    // Wir müssen uns wieder abmelden, wenn das Objekt zerstört wird (sauberer Code!)
    public override void OnNetworkDespawn()
    {
        netCardData.OnValueChanged -= OnCardDataChanged;
    }

    // Das Event-System gibt uns den alten und den neuen Wert. Wir brauchen nur den neuen.
    private void OnCardDataChanged(CardData oldData, CardData newData)
    {
        UpdateVisuals(newData);
    }

    // Hier hat sich kaum was geändert, außer dass wir "data" übergeben bekommen
    void UpdateVisuals(CardData data)
    {
        switch (data.color)
        {
            case CardColor.Red: backgroundRenderer.color = Color.red; break;
            case CardColor.Blue: backgroundRenderer.color = Color.blue; break;
            case CardColor.Green: backgroundRenderer.color = Color.green; break;
            case CardColor.Yellow: backgroundRenderer.color = Color.yellow; break;
            case CardColor.Special: backgroundRenderer.color = Color.white; break;
            default: backgroundRenderer.color = Color.gray; break; // Fallback
        }

        if (data.value == CardValue.Wizard) valueText.text = "Z";
        else if (data.value == CardValue.Jester) valueText.text = "N";
        else
        {
            int number = (int)data.value - 1;
            valueText.text = number.ToString();
        }
    }

    // Die alte SetCardData brauchen wir eigentlich nicht mehr, 
    // aber wir können sie drin lassen, falls wir es mal lokal testen wollen.
    public void SetCardData(CardData data)
    {
        UpdateVisuals(data);
        // Im Multiplayer nutzen wir das hier NICHT mehr direkt!
    }

    // Fix for CS1061: Add ShowCardBack method
    public void ShowCardBack()
    {
        // Example implementation: Hide value and set background to a "back" sprite or color
        if (backgroundRenderer != null)
        {
            // Set to a default back color (gray) or assign a back sprite if available
            backgroundRenderer.color = Color.gray;
        }
        if (valueText != null)
        {
            valueText.text = string.Empty;
        }
    }
}