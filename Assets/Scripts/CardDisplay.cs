using UnityEngine;
using TMPro;
// Wir brauchen hier KEIN Unity.Netcode mehr, da dieses Skript nur Optik ist!

public class CardDisplay : MonoBehaviour
{
    public SpriteRenderer backgroundRenderer;
    public TMP_Text valueText;
    public int handIndex; // Index der Karte in der Hand (für spätere Erweiterungen)

    // Diese Variable speichert die Daten lokal, falls wir sie später beim Klicken brauchen
    private CardData _myCardData;

    public void SetCardData(CardData data)
    {
        _myCardData = data;
        UpdateVisuals(data);
    }

    void UpdateVisuals(CardData data)
    {
        // 1. Hintergrundfarbe setzen
        switch (data.color)
        {
            case CardColor.Red: backgroundRenderer.color = new Color(1f, 0.5f, 0.5f); break; // Helles Rot
            case CardColor.Blue: backgroundRenderer.color = new Color(0.5f, 0.5f, 1f); break; // Helles Blau
            case CardColor.Green: backgroundRenderer.color = new Color(0.5f, 1f, 0.5f); break; // Helles Grün
            case CardColor.Yellow: backgroundRenderer.color = new Color(1f, 1f, 0.5f); break; // Helles Gelb
            // FIX für Zauberer/Narr: Wir machen sie dunkler (grau), damit weißer Text lesbar ist
            case CardColor.Special: backgroundRenderer.color = Color.darkGray; break;
            default: backgroundRenderer.color = Color.white; break;
        }

        // 2. Text setzen
        if (data.value == CardValue.Wizard)
        {
            valueText.text = "Z";
            valueText.color = Color.yellow; // Zauberer stechen hervor
        }
        else if (data.value == CardValue.Jester)
        {
            valueText.text = "N";
            valueText.color = Color.white;
        }
        else
        {
            int number = (int)data.value - 1;
            valueText.text = number.ToString();
            valueText.color = Color.black; // Zahlen in Schwarz für besseren Kontrast
        }
    }

    public void ShowCardBack()
    {
        backgroundRenderer.color = Color.black; // Rückseite schwarz
        valueText.text = ""; // Kein Text
    }
    
}