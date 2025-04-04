using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

public class AnalyticsManager : MonoBehaviourSingleton<AnalyticsManager>
{
    string sessionId;

    private void Awake()
    {
        sessionId = Guid.NewGuid().ToString();
    }

    private void Start()
    {
        InitializeAnalytics();
    }

    private async void InitializeAnalytics()
    {
        await UnityServices.InitializeAsync();
        if (AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.StartDataCollection();
        }
    }

    public void HostStartLogging(string id)
    {
        CustomEvent hostStartedEvent = new("HostStarted")
        {
            {"mySessionID", sessionId},
            {"mySessionTimestamp", DateTime.UtcNow.ToString("o")},
            {"myUserID", id}
        };
        AnalyticsService.Instance.RecordEvent(hostStartedEvent);
    }

    public void ClientStartLogging(string id)
    {
        CustomEvent clientStartedEvent = new("ClientStarted")
        {
            {"mySessionID", sessionId},
            {"mySessionTimestamp", DateTime.UtcNow.ToString("o")},
            {"myUserID", id}
        };
        AnalyticsService.Instance.RecordEvent(clientStartedEvent);
    }

    public void GameStartedLogging()
    {
        CustomEvent gameStartedEvent = new("GameStarted")
        {
            {"mySessionID", sessionId},
            {"mySessionTimestamp", DateTime.UtcNow.ToString("o")}
        };
        AnalyticsService.Instance.RecordEvent(gameStartedEvent);
    }

    public void GameEndedLogging()
    {
        CustomEvent gameEndedEvent = new("GameEnded")
        {
            {"mySessionID", sessionId},
            {"mySessionTimestamp", DateTime.UtcNow.ToString("o")}
        };
        AnalyticsService.Instance.RecordEvent(gameEndedEvent);
    }

    public void BuyItemLogging(string itemUrl, string userId)
    {
        Debug.Log("test");
        CustomEvent buyItemEvent = new("BuyItem")
        {
            {"mySessionID", sessionId},
            {"mySessionTimestamp", DateTime.UtcNow.ToString("o")},
            {"myUserID", userId},
            {"myImageURL", itemUrl}
        };
        AnalyticsService.Instance.RecordEvent(buyItemEvent);
    }
}
