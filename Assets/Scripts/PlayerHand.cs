using System;
using System.Collections.Generic;
using System.Linq; // WICHTIG: Für das Sortieren der Liste (OrderBy/Sort)
using Unity.Netcode;
using UnityEngine;

public class PlayerHand : NetworkBehaviour
{
    public NetworkList<CardData> handCards;

    // Das VISUELLE Prefab (ohne NetworkObject!)
    public GameObject cardPrefab;

    // Liste der aktuell angezeigten Karten-Objekte
    private List<GameObject> spawnedCards = new List<GameObject>();

    // Einstellungen für den Tisch-Radius (Ellipse)
    [Header("Tisch Einstellungen")]
    public float tableRadiusX = 8.0f; // Standard erhöht (war 6.0)
    public float tableRadiusY = 5.0f; // Standard erhöht (war 3.5)

    private void Awake()
    {
        // Initialisierung der NetworkList
        handCards = new NetworkList<CardData>();
    }

    public override void OnNetworkSpawn()
    {
        // 1. Abonnieren für Änderungen an der Hand
        handCards.OnListChanged += OnHandChanged;

        // 2. WICHTIG: Wenn JEMAND joint (auch ich selbst), Sitzordnung neu berechnen
        // Wir hören auf den NetworkManager, um mitzubekommen, wenn sich die Spielerzahl ändert
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerCountChanged;
        }

        // 3. Initial einmal berechnen (damit ich sofort richtig sitze)
        UpdateTableLayout();

        // 4. Farben setzen
        if (IsOwner) GetComponent<Renderer>().material.color = Color.green;
        else GetComponent<Renderer>().material.color = Color.red;

        // Late-Joiner Check
        if (handCards.Count > 0)
        {
            UpdateHandVisuals();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Sauber abmelden
        handCards.OnListChanged -= OnHandChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerCountChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerCountChanged;
        }
    }

    // Wird aufgerufen, wenn ein Spieler joint oder leaved
    private void OnPlayerCountChanged(ulong clientId)
    {
        // Layout neu berechnen, da sich die Anzahl der Spieler geändert hat
        UpdateTableLayout();
    }

    // --- NEUE LOGIK: Dynamische Sitzordnung ---
    public void UpdateTableLayout()
    {
        // 1. Alle Spieler-Objekte in der Szene finden
        List<PlayerHand> allPlayers = FindObjectsByType<PlayerHand>(FindObjectsSortMode.None).ToList();

        // 2. Sortieren nach ID, damit die Reihenfolge bei allen Clients gleich ist
        allPlayers.Sort((p1, p2) => p1.OwnerClientId.CompareTo(p2.OwnerClientId));

        // 3. Meinen eigenen Index in dieser Liste finden (wer bin "ICH"?)
        ulong myId = NetworkManager.Singleton.LocalClientId;
        int myIndex = allPlayers.FindIndex(p => p.OwnerClientId == myId);

        // Falls wir noch nicht fertig geladen sind, abbrechen
        if (myIndex == -1) return;

        int totalPlayers = allPlayers.Count;

        // 4. Positionen berechnen für ALLE Spieler (aus meiner Sicht)
        for (int i = 0; i < totalPlayers; i++)
        {
            PlayerHand player = allPlayers[i];

            // RELATIVE POSITION:
            // Wir "drehen" den Tisch so, dass 'myIndex' immer auf Position 0 landet.
            // Formel: (SpielerIndex - MeinIndex + Anzahl) % Anzahl
            int relativeSeat = (i - myIndex + totalPlayers) % totalPlayers;

            // Sitz 0 ist Unten (-90 Grad)
            // Wir verteilen die anderen im Kreis (360 / Anzahl)
            float angleDeg = -90f - (relativeSeat * (360f / totalPlayers));

            // Umrechnung in Bogenmaß
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Position auf einer Ellipse
            float x = Mathf.Cos(angleRad) * tableRadiusX;
            float y = Mathf.Sin(angleRad) * tableRadiusY;

            // Position zuweisen
            player.transform.position = new Vector3(x, y, 0);

            // Da sich die Position geändert hat, müssen wir eventuell die Karten neu ausrichten
            // (z.B. damit sie nicht aus dem Bild ragen, wenn der Spieler oben ist)
            player.UpdateHandVisuals();
        }
    }

    private void OnHandChanged(NetworkListEvent<CardData> changeEvent)
    {
        UpdateHandVisuals();
    }

    // Sichtbarkeit auf public geändert, damit UpdateTableLayout darauf zugreifen kann
    public void UpdateHandVisuals()
    {
        // 1. Aufräumen
        foreach (GameObject card in spawnedCards)
        {
            if (card != null) Destroy(card);
        }
        spawnedCards.Clear();

        // 2. Setup: Unterschiedliches Spacing für Mich vs. Gegner
        // Wenn ich es bin: 2.3f (breit). Wenn Gegner: 1.0f (überlappend).
        float spacing = IsOwner ? 2.3f : 1.0f;

        float totalWidth = 0f;
        if (handCards.Count > 1)
        {
            totalWidth = (handCards.Count - 1) * spacing;
        }

        float xOffset = -(totalWidth / 2f);

        // 3. Karten neu generieren
        for (int i = 0; i < handCards.Count; i++)
        {
            CardData data = handCards[i];

            // Dynamischer Y-Offset (wie gehabt)
            float yOffset = (transform.position.y < 0) ? 2f : -2f;

            // --- NEU: Z-Offset für saubere Überlappung ---
            // Wir ziehen jede Karte minimal näher zur Kamera (-0.1f * i).
            // Dadurch liegt Karte 2 garantiert optisch VOR Karte 1.
            float zOffset = -0.1f * i;

            // Position relativ zum Spieler berechnen + Z-Offset
            Vector3 spawnPos = transform.position + new Vector3(xOffset, yOffset, zOffset);

            GameObject newCard = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

            // Logik für Anzeige & Index
            var displayScript = newCard.GetComponent<CardDisplay>();

            if (displayScript != null)
            {
                displayScript.handIndex = i;

                if (IsOwner)
                {
                    displayScript.SetCardData(data);
                }
                else
                {
                    displayScript.ShowCardBack();
                }
            }

            spawnedCards.Add(newCard);
            xOffset += spacing;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                CardDisplay card = hit.collider.GetComponent<CardDisplay>();
                if (card != null)
                {
                    // Debug.Log("Karte angeklickt: " + card.GetComponent<CardDisplay>().valueText.text);
                    PlayCardServerRpc(card.handIndex);
                }
            }
        }
        // NUR ZUM TESTEN (später wieder löschen, spart Performance):
        if (IsServer) UpdateTableLayout();
    }

    [ServerRpc]
    void PlayCardServerRpc(int index)
    {
        if (index < 0 || index >= handCards.Count) return;

        CardData playedCard = handCards[index];
        // Debug.Log($"Server: Spieler {OwnerClientId} spielt Karte {playedCard.color} {playedCard.value}");

        handCards.RemoveAt(index);

        if (TableManager.Instance != null)
        {
            TableManager.Instance.AddCard(playedCard);
        }
        else
        {
            Debug.LogError("TableManager nicht gefunden! Hast du ihn in die Szene gelegt?");
        }
    }
}