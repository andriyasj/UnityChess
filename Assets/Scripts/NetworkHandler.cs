using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkHandler : NetworkBehaviourSingleton<NetworkHandler>
{
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        AnalyticsManager.Instance.HostStartLogging(ShopManager.Instance.userId);
    }
    
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        AnalyticsManager.Instance.ClientStartLogging(ShopManager.Instance.userId);
    }
    
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
}
