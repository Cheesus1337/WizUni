using UnityEngine;
using TMPro;
using System;

public class CardDisplay : MonoBehaviour
{
    [Header("Komponenten")]
    public SpriteRenderer backgroundRenderer;
    public TMP_Text valueText;

    [HideInInspector] // Optional, damit es den Inspector nicht vollspammt
    public int handIndex;


    [Header("Standard Sprites")]
    public Sprite cardBackSprite;  // Das Rückseiten-Bild
    // Das Standard-Bild (weißes Viereck), falls kein spezielles Bild gefunden wird
    public Sprite defaultFrontSprite;

    // Eine kleine Hilfsklasse, um die Bilder im Inspector zu ordnen
    [Serializable]
    public struct CardSet
    {
        public string name; // Nur für die Übersicht im Inspector (z.B. "Rote Karten")
        // Array für Zahlen 1-13. Index 0 = Wert 1, Index 12 = Wert 13
        public Sprite[] numberCards;
        public Sprite wizardSprite;
        public Sprite jesterSprite;
    }

    [Header("Karten Bilder Datenbank")]
    public CardSet redCards;
    public CardSet blueCards;
    public CardSet greenCards;
    public CardSet yellowCards;

    private CardData _myCardData;

    public void SetCardData(CardData data)
    {
        _myCardData = data;
        UpdateVisuals(data);
    }

    void UpdateVisuals(CardData data)
    {
        Sprite targetSprite = defaultFrontSprite;
        bool hideText = false; // Falls wir ein Bild haben, wollen wir den Text vielleicht ausblenden

        // 1. Das richtige "Set" basierend auf der Farbe wählen
        CardSet currentSet = new CardSet();
        bool foundSet = true;

        switch (data.color)
        {
            case CardColor.Red: currentSet = redCards; break;
            case CardColor.Blue: currentSet = blueCards; break;
            case CardColor.Green: currentSet = greenCards; break;
            case CardColor.Yellow: currentSet = yellowCards; break;
            default: foundSet = false; break;
        }

        // 2. Das spezifische Bild aus dem Set holen
        if (foundSet)
        {
            if (data.value == CardValue.Wizard)
            {
                if (currentSet.wizardSprite != null)
                {
                    targetSprite = currentSet.wizardSprite;
                    hideText = true; // Wir haben ein Bild, also weg mit dem "Z" Text
                }
            }
            else if (data.value == CardValue.Jester)
            {
                if (currentSet.jesterSprite != null)
                {
                    targetSprite = currentSet.jesterSprite;
                    hideText = true;
                }
            }
            else
            {
                // Zahlen 1-13 (Enum Value 1 ist Index 0)
                int index = (int)data.value - 1;
                // Prüfen, ob das Array groß genug ist und ein Bild hat
                if (currentSet.numberCards != null && index < currentSet.numberCards.Length && currentSet.numberCards[index] != null)
                {
                    targetSprite = currentSet.numberCards[index];
                    hideText = true;
                }
            }
        }

        // 3. Visuals anwenden
        backgroundRenderer.sprite = targetSprite;

        // Wenn wir ein echtes Bild nutzen, setzen wir die Farbe auf Weiß (damit das Bild nicht verfärbt wird)
        // Wenn wir kein Bild haben (default), nutzen wir die alte Einfärbe-Logik als Fallback
        if (targetSprite != defaultFrontSprite)
        {
            backgroundRenderer.color = Color.white;
            valueText.gameObject.SetActive(!hideText); // Text an/aus
        }
        else
        {
            // FALLBACK: Alte Logik (Einfärben + Text), falls du noch nicht alle Bilder hast
            valueText.gameObject.SetActive(true);
            ApplyFallbackColors(data);
        }

        // Text setzen (für Fallback)
        SetTextContent(data);
    }

    public void ShowCardBack()
    {
        if (cardBackSprite != null)
        {
            backgroundRenderer.sprite = cardBackSprite;
            backgroundRenderer.color = Color.white;
            valueText.gameObject.SetActive(false);
        }
        else
        {
            backgroundRenderer.color = Color.black;
            valueText.text = "";
        }
    }

    // --- Hilfsmethoden für die alte Logik (Fallback) ---
    void ApplyFallbackColors(CardData data)
    {
        switch (data.color)
        {
            case CardColor.Red: backgroundRenderer.color = new Color(1f, 0.5f, 0.5f); break;
            case CardColor.Blue: backgroundRenderer.color = new Color(0.5f, 0.5f, 1f); break;
            case CardColor.Green: backgroundRenderer.color = new Color(0.5f, 1f, 0.5f); break;
            case CardColor.Yellow: backgroundRenderer.color = new Color(1f, 1f, 0.5f); break;
            case CardColor.Special: backgroundRenderer.color = Color.darkGray; break;
            default: backgroundRenderer.color = Color.white; break;
        }
    }

    void SetTextContent(CardData data)
    {
        if (data.value == CardValue.Wizard)
        {
            valueText.text = "Z";
            valueText.color = Color.yellow;
        }
        else if (data.value == CardValue.Jester)
        {
            valueText.text = "N";
            valueText.color = Color.white;
        }
        else
        {
            int number = (int)data.value; // oder -1, je nach Enum definition
            valueText.text = number.ToString();
            valueText.color = Color.black;
        }
    }
}