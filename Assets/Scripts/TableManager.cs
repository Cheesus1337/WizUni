using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TableManager : NetworkBehaviour
{
    // Singleton-Pattern: Damit wir von überall einfach "TableManager.Instance" aufrufen können
    public static TableManager Instance;

    // Die Liste der Karten, die gerade auf dem Tisch liegen
    public NetworkList<CardData> playedCards;

    // Referenz auf das visuelle Prefab (das dumme ohne NetworkObject)
    public GameObject cardPrefabVisual;

    public GameObject tableVisual; // Das Tisch-Objekt (für spätere Erweiterungen)

    // Lokale Liste der gespawnten GameObjects (zum Löschen)
    private List<GameObject> spawnedVisuals = new List<GameObject>();

    private void Awake()
    {
        // Singleton setzen
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Liste initialisieren
        playedCards = new NetworkList<CardData>();

        // SICHERHEITSHALBER: Tisch am Anfang verstecken, 
              if (tableVisual != null)
        {
            tableVisual.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {

        if (tableVisual != null)
        {
            tableVisual.SetActive(true);
        }
        // Bei Änderungen: Visuals aktualisieren
        playedCards.OnListChanged += OnTableCardsChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (tableVisual != null)
        {
            tableVisual.SetActive(false);
        }
        playedCards.OnListChanged -= OnTableCardsChanged;
    }

    private void OnTableCardsChanged(NetworkListEvent<CardData> changeEvent)
    {
        UpdateTableVisuals();
    }

    // Diese Methode wird vom Server aufgerufen (aus PlayerHand)
    public void AddCard(CardData card)
    {
        if (IsServer)
        {
            playedCards.Add(card);
        }
    }

    // Wenn der Stich vorbei ist: Tisch leeren
    public void ClearTable()
    {
        if (IsServer)
        {
            playedCards.Clear();
        }
    }

    void UpdateTableVisuals()
    {
        // 1. Alte Karten löschen
        foreach (GameObject obj in spawnedVisuals) Destroy(obj);
        spawnedVisuals.Clear();

        // 2. Neue Karten anzeigen (Zentriert)
        float xOffset = -0.5f * (playedCards.Count - 1); // Simples Zentrieren

        foreach (CardData cardData in playedCards)
        {
            // Position: Exakt in der Mitte (0,0), aber Z = -2 (näher an der Kamera als die Spieler)
            // Dadurch überdecken Tischkarten im Zweifel die Spieler, nicht umgekehrt.
            Vector3 spawnPos = new Vector3(xOffset, 0f, -2f);

            // WICHTIG: Quaternion.identity statt Euler(45, ...). 
            // Keine Neigung = Text ist gut lesbar!
            GameObject newCard = Instantiate(cardPrefabVisual, spawnPos, Quaternion.identity);

            // Daten setzen
            var display = newCard.GetComponent<CardDisplay>();
            if (display != null)
            {
                display.SetCardData(cardData);

                // Optional: Tischkarten etwas kleiner machen, damit es übersichtlich bleibt
                newCard.transform.localScale = Vector3.one * 0.8f;
            }

            spawnedVisuals.Add(newCard);
            xOffset += 1.0f; // Nächste Karte daneben
        }
    }
}