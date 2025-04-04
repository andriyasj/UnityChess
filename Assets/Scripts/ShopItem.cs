using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItem : MonoBehaviour
{
    public int cost;
    public string imageUrl;
    public Image itemImage;
    public Button purchaseButton;
    public TMPro.TextMeshProUGUI costText;

    private ShopManager shopManager;

    private void Start()
    {
        shopManager = GetComponentInParent<ShopManager>();

        if (purchaseButton == null)
        {
            purchaseButton = GetComponent<Button>();
        }

        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        }
    }

    public void SetCost(int objCost)
    {
        cost = objCost;

        if (costText != null)
        {
            costText.text = cost.ToString() + " Points";
        }
    }

    public void OnPurchaseButtonClicked()
    {
        if (shopManager != null)
        {
            shopManager.AttemptPurchase(cost, imageUrl, OnPurchaseComplete);
        }
        else
        {
            Debug.LogError("ShopManager reference not set on ShopItem");
        }
    }

    private void OnPurchaseComplete(bool success)
    {
        if (success)
        {
            Debug.Log("Purchase successful: " + imageUrl);
            // You could add visual feedback here, like a success animation
        }
        else
        {
            Debug.Log("Purchase failed: Not enough points or other error");
            // You could add visual feedback here, like shaking the button
        }
    }
}
