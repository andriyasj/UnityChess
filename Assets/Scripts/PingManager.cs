using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class PingManager : NetworkBehaviour
{
    // Dictionary to store ping data for each client
    private Dictionary<ulong, float> clientPingStartTimes = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> clientPingValues = new Dictionary<ulong, float>();

    // UI Text element to display ping (optional)
    [SerializeField] private TMPro.TextMeshProUGUI pingText;

    // How often to measure ping
    [SerializeField] private float pingInterval = 1.0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server listens for ping requests from clients
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
        {
            // Start sending pings if we're a client
            StartCoroutine(PingRoutine());
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Initialize ping data for new client
        clientPingValues[clientId] = 0;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Clean up when client disconnects
        if (clientPingStartTimes.ContainsKey(clientId))
            clientPingStartTimes.Remove(clientId);

        if (clientPingValues.ContainsKey(clientId))
            clientPingValues.Remove(clientId);
    }

    private IEnumerator PingRoutine()
    {
        while (true)
        {
            // Wait for the specified interval
            yield return new WaitForSeconds(pingInterval);

            // Send ping to server
            SendPingServerRpc(NetworkManager.Singleton.LocalClientId, Time.realtimeSinceStartup);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendPingServerRpc(ulong clientId, float clientTime)
    {
        // Server immediately responds with a pong
        ReceivePongClientRpc(clientTime, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });
    }

    [ClientRpc]
    private void ReceivePongClientRpc(float sentTime, ClientRpcParams clientRpcParams = default)
    {
        // Calculate round trip time
        float currentTime = Time.realtimeSinceStartup;
        float pingMs = (currentTime - sentTime) * 1000f;

        // Update UI if available
        if (pingText != null)
        {
            pingText.text = $"Ping: {pingMs:F1} ms";
        }

        // Store the ping value for this client
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        clientPingValues[localClientId] = pingMs;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (IsClient)
        {
            StopAllCoroutines();
        }
    }

    // Method to get ping for a specific client (can be called by other components)
    public float GetClientPing(ulong clientId)
    {
        if (clientPingValues.TryGetValue(clientId, out float ping))
            return ping;
        return 0;
    }
}
