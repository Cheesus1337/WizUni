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
        float spacing = 2.3f; // Dein eingestellter Abstand

        // Berechnung der Gesamtbreite für die Zentrierung
        // Bei 0 oder 1 Karten verhindern wir negative Werte/Fehler
        float totalWidth = 0f;
        if (handCards.Count > 1)
        {
            totalWidth = (handCards.Count - 1) * spacing;
        }

        // Wir starten bei der Hälfte der Breite nach links verschoben
        float xOffset = -(totalWidth / 2f);

        // 3. Karten neu generieren
        // WICHTIG: Wir nutzen jetzt eine for-Schleife statt foreach!
        // Das gibt uns den Index 'i' (0, 1, 2...), den wir für die Logik brauchen.
        for (int i = 0; i < handCards.Count; i++)
        {
            CardData data = handCards[i]; // Daten für die aktuelle Karte holen

            // Position relativ zum Spieler berechnen
            Vector3 spawnPos = transform.position + new Vector3(xOffset, 2f, 0);

            // Instanziieren (Lokal)
            GameObject newCard = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

            // Debugging (Index + 1 für die Anzeige, damit es bei "Karte 1" losgeht)
            Debug.Log($"Karte {i + 1}: Farbe={data.color}, Wert={data.value}");

            // --- HIER IST DIE WICHTIGE ÄNDERUNG (TEIL A) ---
            // Wir holen uns das Skript der Karte...
            var displayScript = newCard.GetComponent<CardDisplay>();

            if (displayScript != null)
            {
                // ... und sagen der Karte, an welcher Stelle sie liegt.
                // Das brauchen wir gleich für den Klick (ServerRpc)!
                displayScript.handIndex = i;

                // Sichtbarkeits-Logik
                if (IsOwner)
                {
                    // Eigene Karten: Daten anzeigen
                    displayScript.SetCardData(data);
                }
                else
                {
                    // Gegner-Karten: Rückseite
                    displayScript.ShowCardBack();
                }
            }
            // ------------------------------------------------

            spawnedCards.Add(newCard);

            // Abstände für die nächste Karte addieren
            xOffset += spacing;
        }
    }

    private void Update()
    {
        // Nur wenn ich der Besitzer bin, darf ich klicken
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0)) // Linksklick
        {
            // Raycast von der Mausposition in die Welt (für 2D/3D)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Für 3D Collider (nutzt du 2D oder 3D Collider auf den Karten?)
            if (Physics.Raycast(ray, out hit))
            {
                // Prüfen, ob wir eine Karte getroffen haben
                CardDisplay card = hit.collider.GetComponent<CardDisplay>();
                if (card != null)
                {
                    Debug.Log("Karte angeklickt: " + card.GetComponent<CardDisplay>().valueText.text);
                    PlayCardServerRpc(card.handIndex);
                    // Hier kommt später die Logik zum Ausspielen hin
                }
            }

            // Falls du 2D Collider nutzt (BoxCollider2D), brauchst du diesen Code stattdessen:
            /*
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit2D.collider != null)
            {
                 CardDisplay card = hit2D.collider.GetComponent<CardDisplay>();
                 if (card != null) Debug.Log("Karte angeklickt!");
            }
            */
        }
    }

    [ServerRpc]
    void PlayCardServerRpc(int index)
    {
        // Sicherheits-Check: Ist der Index gültig?
        if (index < 0 || index >= handCards.Count) return;

        // Logik: Karte holen
        CardData playedCard = handCards[index];
        Debug.Log($"Server: Spieler {OwnerClientId} spielt Karte {playedCard.color} {playedCard.value}");

        // 1. Karte aus der Hand entfernen
        // Da 'handCards' eine NetworkList ist, wird das Löschen AUTOMATISCH
        // an alle Clients gesendet! Die Hand wird bei allen neu gezeichnet.
        handCards.RemoveAt(index);

        // 2. TODO: Karte auf den Tisch legen (Machen wir im nächsten Schritt)
        // DeckManager.Instance.AddPlayedCard(playedCard);
    }

}