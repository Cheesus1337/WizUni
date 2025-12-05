using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class DeckManager : NetworkBehaviour
{
    // Diese Liste ist nur f�r den Server wichtig, um den Stapel zu verwalten
    public List<CardData> currentDeck = new List<CardData>();

    public GameObject cardPrefab; // WICHTIG: Dieses Prefab muss im NetworkManager registriert sein!
    
    // Cache for player hands to avoid repeated FindObjectsByType calls
    private PlayerHand[] cachedPlayers;

    public override void OnNetworkSpawn()
    {
        // Nur der Server darf das Deck generieren und Karten verteilen
        if (IsServer)
        {
            GenerateDeck();
            
            // Subscribe to network events to invalidate cache when players join/leave
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Invalidate cache when a new player joins
        cachedPlayers = null;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Invalidate cache when a player leaves
        cachedPlayers = null;
    }

    private bool IsPlayerCacheValid()
    {
        // Check if all cached players are still valid (not null or destroyed)
        foreach (PlayerHand player in cachedPlayers)
        {
            if (player == null)
            {
                return false;
            }
        }
        return true;
    }

    private void Update()
    {
        // Nur der Server/Host darf das Spiel starten
        if (IsServer)
        {
            // Wenn wir "Leertaste" dr�cken, werden Karten verteilt
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

        // Wizards hinzuf�gen
        for (int i = 0; i < 4; i++)
        {
            currentDeck.Add(new CardData(CardColor.Special, CardValue.Wizard));
        }

        // Narren hinzuf�gen
        for (int i = 0; i < 4; i++)
        {
            currentDeck.Add(new CardData(CardColor.Special, CardValue.Jester));
        }

        ShuffleDeck();
        Debug.Log("Deck generiert mit " + currentDeck.Count + " Karten.");
    }

    void DealCards()
    {
        // Use cached players or find them if not cached, or if cache contains destroyed objects
        if (cachedPlayers == null || !IsPlayerCacheValid())
        {
            cachedPlayers = FindObjectsByType<PlayerHand>(FindObjectsSortMode.None);
        }

        if (cachedPlayers.Length == 0)
        {
            Debug.LogError("Keine Spieler gefunden! (Warten Sie ggf., bis Spieler verbunden sind, bevor Sie DealCards aufrufen)");
            return;
        }

        Debug.Log("Verteile Karten an " + cachedPlayers.Length + " Spieler.");

        int cardsPerPlayer = 5;

        foreach (PlayerHand player in cachedPlayers)
        {
            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (currentDeck.Count > 0)
                {
                    // Remove from end for O(1) instead of O(n)
                    int lastIndex = currentDeck.Count - 1;
                    CardData cardToDeal = currentDeck[lastIndex];
                    currentDeck.RemoveAt(lastIndex);

                    // 1. Daten logisch hinzuf�gen (f�r Spielregeln)
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

    // Diese Funktion spawnt das echte GameObject im Netzwerk
    void SpawnCardVisual(CardData data, Vector3 pos)
    {
        // A) Instanziieren (passiert erst nur auf dem Server)
        GameObject newCard = Instantiate(cardPrefab, pos, Quaternion.identity);

        // B) Das NetworkObject holen
        NetworkObject netObj = newCard.GetComponent<NetworkObject>();

        // C) WICHTIG: Spawn aufrufen! Damit wissen alle Clients: "Hier ist ein neues Objekt!"
        netObj.Spawn();

        // D) Daten setzen
        // Achtung: Das funktioniert nur, wenn "CardDisplay" auch ein NetworkBehaviour ist 
        // und netCardData eine NetworkVariable ist.
        if (newCard.TryGetComponent(out CardDisplay display))
        {
            display.netCardData.Value = data;
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