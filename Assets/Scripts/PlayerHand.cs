using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random; // Eindeutigkeit für Random

public class PlayerHand : NetworkBehaviour
{
    public NetworkList<CardData> handCards;

    // Das VISUELLE Prefab (ohne NetworkObject!)
    public GameObject cardPrefab;

    // Liste der aktuell angezeigten Karten-Objekte
    private List<GameObject> spawnedCards = new List<GameObject>();

    private void Awake()
    {
        // Initialisierung der NetworkList
        handCards = new NetworkList<CardData>();
    }

    public override void OnNetworkSpawn()
    {
        // --- ÄNDERUNG START ---

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

        // 2. Farben (Lokal für die Optik)
        if (IsOwner) GetComponent<Renderer>().material.color = Color.green; // Ich bin grün
        else GetComponent<Renderer>().material.color = Color.red;       // Gegner sind rot

        // --- ÄNDERUNG ENDE ---

        // Abonnieren für Änderungen
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
        // 1. Aufräumen: Alte Bilder löschen
        foreach (GameObject card in spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        spawnedCards.Clear();

        // 2. Setup für die Schleife
        float spacing = 2.3f;
        // Wir berechnen die Gesamtbreite der Hand
        // (Anzahl Karten - 1) * Abstand
        // Bei 5 Karten und Abstand 1.5 ist die Breite 6.0
        float totalWidth = (handCards.Count - 1) * spacing;


        // Wir starten bei der Hälfte der Breite nach links verschoben
        float xOffset = -(totalWidth / 2f);
        int index = 1;

        // 3. Karten neu generieren
        foreach (CardData data in handCards)
        {
            // Position relativ zum Spieler berechnen
            Vector3 spawnPos = transform.position + new Vector3(xOffset, 2f, 0);

            // Instanziieren (Lokal)
            GameObject newCard = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

            // Debugging
            Debug.Log($"Karte {index}: Farbe={data.color}, Wert={data.value}");

            // Sichtbarkeits-Logik
            if (IsOwner)
            {
                // Eigene Karten: Daten anzeigen
                // Stelle sicher, dass SetCardData keine NullReference wirft!
                var displayScript = newCard.GetComponent<CardDisplay>();
                if (displayScript != null)
                {
                    displayScript.SetCardData(data);
                }
            }
            else
            {
                // Gegner-Karten: Rückseite
                var displayScript = newCard.GetComponent<CardDisplay>();
                if (displayScript != null)
                {
                    displayScript.ShowCardBack();
                }
            }

            spawnedCards.Add(newCard);

            // Abstände für die nächste Karte
            xOffset += spacing;
            index++; // WICHTIG: Zähler erhöhen
        }
    }
}