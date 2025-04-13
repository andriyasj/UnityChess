using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityChess;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class ShopManager : MonoBehaviourSingleton<ShopManager>
{
    public string userId;

    private FirebaseFirestore db;
    private ListenerRegistration purchaseListener;

    [SerializeField] private GameObject shop;
    [SerializeField] private Image profileIcon;
    [SerializeField] private Image oppProfileIcon;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private List<GameObject> shopItems = new List<GameObject>();

    private int currentPoints = 0;
    private List<string> icons = new List<string>();
    private readonly string localProfileImageFolder = "ProfileImages";

    private void Awake()
    {
        InitialiseFirebase();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(userId))
        {
            userId = "0";
        }

        GetUserPoints(userId);
        GetCurrentPFP();
        GetOppPFP();
        LoadShopIcons();
        StartPurchaseListener();
    }

    private void InitialiseFirebase()
    {
        FirebaseFirestore.DefaultInstance.Settings.PersistenceEnabled = false;
        db = FirebaseFirestore.DefaultInstance;
    }

    public void ToggleShop()
    {
        if (shop.GetComponent<Canvas>().enabled)
        {
            HideShop();
        }
        else
        {
            ShowShop();
        }
    }

    private void ShowShop()
    {
        shop.GetComponent<Canvas>().enabled = true;
    }

    private void HideShop()
    {
        shop.GetComponent<Canvas>().enabled = false;
    }

    private void LoadShopIcons()
    {
        db.Collection("profileicons").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;

                for (int i = 0; i < snapshot.Count && i < shopItems.Count; i++)
                {
                    var documentsList = snapshot.Documents.ToList();
                    DocumentSnapshot document = documentsList[i];
                    Dictionary<string, object> data = document.ToDictionary();

                    if (data.TryGetValue("cost", out object costObj) &&
                        data.TryGetValue("imageURL", out object imageUrlObj))
                    {
                        int cost = System.Convert.ToInt32(costObj);
                        string imageUrl = imageUrlObj.ToString();

                        ShopItem item = shopItems[i].GetComponent<ShopItem>();
                        if (item != null)
                        {
                            item.SetCost(cost);
                            item.imageUrl = imageUrl;

                            StartCoroutine(DownloadIcon(imageUrl, item.itemImage));
                        }
                    }
                }
            }
        });
    }

    private void GetOppPFP()
    {
        DocumentReference userRef;

        if (userId == "0")
        {
            userRef = db.Collection("users").Document("1");
        }
        else
        {
            userRef = db.Collection("users").Document("0");
        }

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> userData = snapshot.ToDictionary();
                    if (userData.TryGetValue("profileImage", out object imageUrlObj))
                    {
                        string imageUrl = imageUrlObj.ToString();

                        StartCoroutine(DownloadIcon(imageUrl, oppProfileIcon));
                    }
                }
            }
        });
    }

    private IEnumerator DownloadIcon(string imageUrl, Image targetImage)
    {
        if (targetImage == null)
        {
            Debug.LogError("Target image is null for URL: " + imageUrl);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                DownloadHandlerTexture downloadHandler = request.downloadHandler as DownloadHandlerTexture;
                if (downloadHandler != null && downloadHandler.texture != null)
                {
                    Texture2D texture = downloadHandler.texture;
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    targetImage.sprite = sprite;
                }
                else
                {
                    Debug.LogError("Failed to get texture from download handler for URL: " + imageUrl);
                }
            }
            else
            {
                Debug.LogError("Failed to download image: " + request.error + " for URL: " + imageUrl);
            }
        }
    }

    private void GetUserPoints(string userID)
    {
        if (string.IsNullOrEmpty(userID)) return;

        DocumentReference userRef = db.Collection("users").Document(userID);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> userData = snapshot.ToDictionary();
                    if (userData.TryGetValue("points", out object pointsObj))
                    {
                        currentPoints = System.Convert.ToInt32(pointsObj);
                        UpdatePointsText();
                    }
                }
            }
        });
    }

    private void UpdatePointsText()
    {
        if (pointsText != null)
        {
            pointsText.text = "Points: " + currentPoints.ToString();
        }
    }

    private void GetCurrentPFP()
    {
        string localPath = GetLocalPFPPath();
        if (File.Exists(localPath))
        {
            StartCoroutine(LoadLocalPFP(localPath));
            return;
        }

        if (string.IsNullOrEmpty(userId)) return;

        DocumentReference userRef = db.Collection("users").Document(userId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    Dictionary<string, object> userData = snapshot.ToDictionary();
                    if (userData.TryGetValue("profileImage", out object imageUrlObj))
                    {
                        string imageUrl = imageUrlObj.ToString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            StartCoroutine(DownloadPFP(imageUrl));
                        }
                    }
                }
            }
        });
    }

    private IEnumerator LoadLocalPFP(string path)
    {
        if (File.Exists(path))
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            profileIcon.sprite = sprite;
        }
        yield return null;
    }

    private string GetLocalPFPPath()
    {
        string directory = Path.Combine(Application.persistentDataPath, localProfileImageFolder);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        if (userId == "0") return Path.Combine(directory, "profile0.jpg");
        else return Path.Combine(directory, "profile1.jpg");
    }

    private IEnumerator DownloadPFP(string imageUrl)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                profileIcon.sprite = sprite;

                byte[] imageData = texture.EncodeToJPG();
                File.WriteAllBytes(GetLocalPFPPath(), imageData);
            }
        }
    }

    private void StartPurchaseListener()
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        ListenerRegistration purchaseListener = db.Collection("shopevents").Document("event")
            .Listen(snapshot => {
                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    if (data.TryGetValue("imageURL", out object imageUrlObj) &&
                        data.TryGetValue("user", out object userObj))
                    {
                        string imageUrl = imageUrlObj.ToString();
                        string purchasedByUser = userObj.ToString();

                        Debug.Log($"User {purchasedByUser} purchased item with image URL: {imageUrl}");

                        if (purchasedByUser != userId)
                        {
                            Debug.Log($"Another user ({purchasedByUser}) made a purchase!");
                            GetOppPFP();
                        }
                    }
                }
            });

        this.purchaseListener = purchaseListener;
    }

    public bool AttemptPurchase(int cost, string imageUrl, System.Action<bool> onComplete = null)
    {
        if (currentPoints >= cost)
        {
            // Deduct points
            int newPoints = currentPoints - cost;

            // Update Firestore
            DocumentReference userRef = db.Collection("users").Document(userId);
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "points", newPoints }
            };

            userRef.UpdateAsync(updates).ContinueWithOnMainThread(task => {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    // Update local points
                    currentPoints = newPoints;
                    UpdatePointsText();

                    // Apply the new profile image
                    ApplyPFP(imageUrl);

                    // Record the purchase event
                    SaveShopEvent(imageUrl, userId);

                    onComplete?.Invoke(true);
                }
                else
                {
                    onComplete?.Invoke(false);
                }
            });

            return true;
        }

        onComplete?.Invoke(false);
        return false;
    }

    private void ApplyPFP(string imageUrl)
    {
        // Save the image URL to Firestore
        SavePFPInFirestore(imageUrl);

        // Download and apply the image
        StartCoroutine(DownloadPFP(imageUrl));
    }

    private void SavePFPInFirestore(string imageUrl)
    {
        if (string.IsNullOrEmpty(userId)) return;

        DocumentReference userRef = db.Collection("users").Document(userId);
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "profileImage", imageUrl }
        };

        userRef.UpdateAsync(updates);
    }

    private void SaveShopEvent(string imageUrl, string purchasedByUserId)
    {
        DocumentReference eventRef = db.Collection("shopevents").Document("event");
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "imageURL", imageUrl },
            { "user", purchasedByUserId }
        };

        eventRef.SetAsync(eventData);
        AnalyticsManager.Instance.BuyItemLogging(imageUrl, purchasedByUserId);
    }

    private void OnDestroy()
    {
        if (purchaseListener != null)
        {
            purchaseListener.Stop();
        }
    }

    public void OnSaveButtonClicked()
    {
        string gameID = $"chess_game_{DateTime.Now.Ticks}";
        GameManager.Instance.SaveGameStateServerRpc(gameID);
        UIManager.Instance.UpdateGameStringInputField(gameID);
    }
        

    public async Task<bool> SaveGameState(string gameId, string serializedGame, Side currentTurn)
    {
        try
        {
            DocumentReference gameRef = db.Collection("games").Document(gameId);
            Dictionary<string, object> gameData = new Dictionary<string, object>
            {
                { "serializedGame", serializedGame },
                { "currentTurn", currentTurn.ToString() },
                { "lastUpdated", Timestamp.GetCurrentTimestamp() }
            };

            await gameRef.SetAsync(gameData);
            Debug.Log("Game saved successfully to Firestore");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}");
            return false;
        }
    }

    public async Task<(string serializedGame, Side currentTurn)> LoadGameState(string gameId)
    {
        try
        {
            DocumentReference gameRef = db.Collection("games").Document(gameId);
            DocumentSnapshot snapshot = await gameRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Dictionary<string, object> gameData = snapshot.ToDictionary();

                // Debug logs for inspection
                Debug.Log($"Document data: {string.Join(", ", gameData.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");

                if (gameData.TryGetValue("serializedGame", out object serializedGameObj) &&
                    gameData.TryGetValue("currentTurn", out object currentTurnObj))
                {
                    string serializedGame = serializedGameObj.ToString();
                    Side currentTurn = (Side)Enum.Parse(typeof(Side), currentTurnObj.ToString());

                    Debug.Log($"Loaded serializedGame: {serializedGame}, currentTurn: {currentTurn}");
                    return (serializedGame, currentTurn);
                }
                else
                {
                    Debug.LogError("Missing required fields in Firestore document.");
                    return (null, Side.White);
                }
            }
            else
            {
                Debug.LogWarning($"Document with ID {gameId} does not exist.");
                return (null, Side.White);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading game: {e.Message}");
            return (null, Side.White);
        }
    }
}
