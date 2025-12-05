using UnityEngine;
using Unity.Netcode;
using System;

[System.Serializable]
public struct CardData : INetworkSerializable, IEquatable<CardData>
{
    public CardColor color;
    public CardValue value;

    public CardData(CardColor c, CardValue v)
    {
        color = c;
        value = v;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref value);
    }

    public bool Equals(CardData other)
    {
        return color == other.color && value == other.value;
    }

}



    public enum CardColor
    {
        None,
        Red,
        Blue,
        Green,
        Yellow,
        Special
    }

    public enum CardValue

    {
        None,
        Jester,
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Eleven,
        Twelve,
        Thirteen,
        Wizard
    }