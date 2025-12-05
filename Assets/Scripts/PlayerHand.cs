using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random; // Eindeutigkeit f�r Random

public class PlayerHand : NetworkBehaviour
{
    public NetworkList<CardData> handCards;

    // Das VISUELLE Prefab (ohne NetworkObject!)
    public GameObject cardPrefab;

    // Liste der aktuell angezeigten Karten-Objekte
    private List<GameObject> spawnedCards = new List<GameObject>();
    
    // Cache for CardDisplay components to avoid GetComponent calls in update loops
    private List<CardDisplay> spawnedCardDisplays = new List<CardDisplay>();
    
    // Error message constant
    private const string MISSING_DISPLAY_ERROR = "CardPrefab is missing CardDisplay component! Cannot display cards. Please fix the prefab.";

    private void Awake()
    {
        // Initialisierung der NetworkList
        handCards = new NetworkList<CardData>();
    }

    public override void OnNetworkSpawn()
    {
        // --- �NDERUNG START ---

        // 1. Positionen: Nur der Server darf entscheiden, wer wo steht!
        // Wir nutzen NetworkTransform, damit das Ergebnis an alle gesendet wird.
        if (IsServer)
        {
            // OwnerClientId 0 ist immer der Host.
            // OwnerClientId 1, 2, etc. sind die Clients.

            if (OwnerClientId == 0)
            {
                // Host steht links
                transform.position = new Vector3(-5f, 0f, 0f);
            }
            else
            {
                // Client(s) stehen rechts (wir schieben jeden weiteren Client etwas weiter)
                float xPos = 5f + (OwnerClientId * 2.0f);
                transform.position = new Vector3(xPos, 0f, 0f);
            }
        }

        // 2. Farben (Lokal f�r die Optik)
        if (IsOwner) GetComponent<Renderer>().material.color = Color.green; // Ich bin gr�n
        else GetComponent<Renderer>().material.color = Color.red;       // Gegner sind rot

        // --- �NDERUNG ENDE ---

        // Abonnieren f�r �nderungen
        handCards.OnListChanged += OnHandChanged;

        // Late-Joiner Check
        if (handCards.Count > 0)
        {
            UpdateHandVisuals();
        }
    }

    public override void OnNetworkDespawn()
    {
        handCards.OnListChanged -= OnHandChanged;
    }

    private void OnHandChanged(NetworkListEvent<CardData> changeEvent)
    {
        UpdateHandVisuals();
    }

    void UpdateHandVisuals()
    {
        float spacing = 2.3f;
        float totalWidth = (handCards.Count - 1) * spacing;
        float xOffset = -(totalWidth / 2f);

        // Optimization: Reuse existing cards when possible, only create/destroy the difference
        int targetCount = handCards.Count;

        // Remove excess cards if we have too many
        while (spawnedCards.Count > targetCount)
        {
            int lastIndex = spawnedCards.Count - 1;
            GameObject cardToRemove = spawnedCards[lastIndex];
            spawnedCards.RemoveAt(lastIndex);
            spawnedCardDisplays.RemoveAt(lastIndex);
            if (cardToRemove != null) Destroy(cardToRemove);
        }

        // Add new cards if we need more
        while (spawnedCards.Count < targetCount)
        {
            GameObject newCard = Instantiate(cardPrefab, transform.position, Quaternion.identity);
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display == null)
            {
                Debug.LogError(MISSING_DISPLAY_ERROR);
                Destroy(newCard);
                return; // Fail fast - cannot display cards without proper prefab
            }
            spawnedCards.Add(newCard);
            spawnedCardDisplays.Add(display);
        }

        // Update all cards with current data and positions
        for (int i = 0; i < handCards.Count; i++)
        {
            CardData data = handCards[i];
            GameObject card = spawnedCards[i];
            CardDisplay displayScript = spawnedCardDisplays[i];

            // Position relativ zum Spieler berechnen
            Vector3 targetPos = transform.position + new Vector3(xOffset, 2f, 0);
            card.transform.position = targetPos;

            // Sichtbarkeits-Logik
            if (IsOwner)
            {
                // Eigene Karten: Daten anzeigen
                displayScript.SetCardData(data);
            }
            else
            {
                // Gegner-Karten: R�ckseite
                displayScript.ShowCardBack();
            }

            xOffset += spacing;
        }
    }
}