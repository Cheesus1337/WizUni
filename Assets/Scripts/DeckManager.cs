using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class DeckManager : NetworkBehaviour
{
    // Diese Liste ist nur für den Server wichtig, um den Stapel zu verwalten
    public List<CardData> currentDeck = new List<CardData>();

    public GameObject cardPrefab; // WICHTIG: Dieses Prefab muss im NetworkManager registriert sein!

    public override void OnNetworkSpawn()
    {
        // Nur der Server darf das Deck generieren und Karten verteilen
        if (IsServer)
        {
            GenerateDeck();
          
        }
    }

    private void Update()
    {
        // Nur der Server/Host darf das Spiel starten
        if (IsServer)
        {
            // Wenn wir "Leertaste" drücken, werden Karten verteilt
            if (Input.GetKeyDown(KeyCode.Space))
            {
                DealCards();
            }
        }


    }

    void GenerateDeck()
    {
        currentDeck.Clear();
        CardColor[] colors = { CardColor.Red, CardColor.Blue, CardColor.Green, CardColor.Yellow };

        foreach (CardColor color in colors)
        {
            for (int i = 1; i < 14; i++)
            {
                CardValue val = (CardValue)(i + 1);
                currentDeck.Add(new CardData(color, val));
            }
        }

        // Wizards hinzufügen
        for (int i = 0; i < 4; i++)
        {
            currentDeck.Add(new CardData(CardColor.Special, CardValue.Wizard));
        }

        // Narren hinzufügen
        for (int i = 0; i < 4; i++)
        {
            currentDeck.Add(new CardData(CardColor.Special, CardValue.Jester));
        }

        ShuffleDeck();
        Debug.Log("Deck generiert mit " + currentDeck.Count + " Karten.");
    }

    void DealCards()
    {
        PlayerHand[] players = FindObjectsByType<PlayerHand>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            Debug.LogError("Keine Spieler gefunden! (Warten Sie ggf., bis Spieler verbunden sind, bevor Sie DealCards aufrufen)");
            return;
        }

        Debug.Log("Verteile Karten an " + players.Length + " Spieler.");

        int cardsPerPlayer = 5;

        foreach (PlayerHand player in players)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (currentDeck.Count > 0)
                {
                    CardData cardToDeal = currentDeck[0];
                    currentDeck.RemoveAt(0);

                    // 1. Daten logisch hinzufügen (für Spielregeln)
                    // HINWEIS: 'player.handCards' muss eine NetworkList sein, damit es synchronisiert wird
                    player.handCards.Add(cardToDeal);

                    // 2. OPTION 1: Echtes Netzwerk-Objekt spawnen (Visualisierung)
                    // Ich habe die Berechnung leicht angepasst, damit die Karten nicht alle exakt aufeinander liegen
                //    Vector3 spawnPos = player.transform.position + new Vector3(i * 0.5f, 2f, 0f);
                 //   SpawnCardVisual(cardToDeal, spawnPos);
                }
            }
        }
    }

    void ShuffleDeck()
    {
        for (int i = currentDeck.Count - 1; i > 0; i--)
        {
            int k = Random.Range(0, i + 1);
            CardData temp = currentDeck[i];
            currentDeck[i] = currentDeck[k];
            currentDeck[k] = temp;
        }
        Debug.Log("Deck wurde gemischt!");
    }
}