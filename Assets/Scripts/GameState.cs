using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityChess;
using UnityEngine;

public struct GameState : INetworkSerializable
{
    public Side SideToMove;
    public bool GameEnded;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SideToMove);
        serializer.SerializeValue(ref GameEnded);
    }
}
