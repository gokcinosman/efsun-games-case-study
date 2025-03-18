using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
[Serializable]
public class ResourceData
{
    public string resourceName;
    public int amount;
    public ResourceData(string name, int value)
    {
        resourceName = name;
        amount = value;
    }
}
[Serializable]
public class FactoryData
{
    public string factoryName;
    public int currentStock;
    public bool isProducing;
    public int productionQueue;
    public float remainingProductionTime;
    public long lastSaveTimeTicks;
    [NonSerialized]
    private DateTime _lastSaveTime;
    public DateTime lastSaveTime
    {
        get
        {
            if (_lastSaveTime == default && lastSaveTimeTicks > 0)
                _lastSaveTime = new DateTime(lastSaveTimeTicks);
            if (_lastSaveTime == default)
                _lastSaveTime = DateTime.Now;
            return _lastSaveTime;
        }
        set
        {
            _lastSaveTime = value;
            lastSaveTimeTicks = value.Ticks;
        }
    }
    public FactoryData(string name, int stock, bool producing, int queue, float remainingTime)
    {
        factoryName = name;
        currentStock = stock;
        isProducing = producing;
        productionQueue = queue;
        remainingProductionTime = remainingTime;
        lastSaveTime = DateTime.Now;
    }
}
[Serializable]
public class GameData
{ // hammaddelerin miktarı lazım -bitti-
    // hammaddelerin isimleri lazım -bitti-
    //   --------factory data--------
    // factory id lazım -bitti-
    // stock lazım -bitti-
    // production queue lazım -eklendi-
    // isproducing değişkeni lazım -eklendi-
    // production progressin son anı lazım -eklendi-
    // buraya değil ama genel bir production progress hesaplaması lazım
    // production progress hesaplaması için factorynin üretim süresi lazım
    // remaning time gibi bir şey yani -eklendi-
    public List<ResourceData> resources = new List<ResourceData>();
    public List<FactoryData> factories = new List<FactoryData>();
    public long lastSaveTimeTicks;  // DateTime yerine long (ticks) kullanılacak, yoksa saat 00000 olarak kaydediliyor.
    [NonSerialized]
    private DateTime _lastSaveTime;
    public DateTime lastSaveTime
    {
        get
        {
            if (_lastSaveTime == default && lastSaveTimeTicks > 0)
                _lastSaveTime = new DateTime(lastSaveTimeTicks);
            if (_lastSaveTime == default)
                _lastSaveTime = DateTime.Now;
            return _lastSaveTime;
        }
        set
        {
            _lastSaveTime = value;
            lastSaveTimeTicks = value.Ticks;
        }
    }
}
public class SaveManager : MonoBehaviour
{
    [Inject] private ResourceManager resourceManager;
    [Inject] private FactoryManager factoryManager;
    private GameData gameData = new GameData();
    private string saveFilePath;
    private void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/save.json";
        LoadGameData();
    }
    private void Start()
    {
        ApplyResourceData();
        if (gameData.lastSaveTime != default(DateTime) && factoryManager != null)
        {
            ProcessOfflineProduction();
        }
    }
    private void LoadGameData()
    {
        if (System.IO.File.Exists(saveFilePath))
        {
            string json = System.IO.File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(json);
            if (gameData.lastSaveTimeTicks <= 0)
            {
                gameData.lastSaveTime = DateTime.Now;
                Debug.LogWarning("Geçersiz kayıt zamanı, şu anki zaman kullanılıyor: " + gameData.lastSaveTime);
            }
            else
            {
                Debug.Log("Kayıt dosyası yüklendi, son kayıt zamanı: " + gameData.lastSaveTime);
            }
        }
        else
        {
            Debug.Log("Kayıt dosyası bulunamadı. Yeni kayıt oluşturuluyor.");
            gameData = new GameData();
            gameData.lastSaveTime = DateTime.Now;
        }
    }
    private void ApplyResourceData()
    {
        if (resourceManager != null)
        {
            foreach (var resource in gameData.resources)
            {
                resourceManager.SetResource(resource.resourceName, resource.amount);
            }
        }
    }
    private void ProcessOfflineProduction()
    {
        if (gameData.lastSaveTime == default(DateTime))
        {
            Debug.LogWarning("Geçerli son kayıt zamanı yok!");
            return;
        }
        DateTime now = DateTime.Now;
        TimeSpan offlineTime = now - gameData.lastSaveTime;
        float offlineSeconds = (float)offlineTime.TotalSeconds;
        if (offlineSeconds < 1.0f)
        {
            return;
        }
        Debug.Log($"Çevrimdışı hesaplama başlatılıyor. Geçen süre: {offlineSeconds} saniye");
        foreach (var factoryData in gameData.factories)
        {
            UpdateFactoryOfflineProduction(factoryData, offlineSeconds);
        }
        gameData.lastSaveTime = now;
    }
    private void UpdateFactoryOfflineProduction(FactoryData factoryData, float offlineSeconds)
    {
        BaseFactory factory = factoryManager.GetFactoryByName(factoryData.factoryName);
        if (factory == null || !factoryData.isProducing)
        {
            return;
        }
        float productionTime = factory.Recipe.productionTime;
        float remainingTime = factoryData.remainingProductionTime;
        // Offline sürede üretilen ürün sayısını hesaplama
        int producedItems = CalculateOfflineProduction(
            offlineSeconds,
            remainingTime,
            productionTime,
            out float newRemainingTime);
        // Kapasite kontrolü
        int maxProduction = factory.config.capacity - factoryData.currentStock;
        producedItems = Mathf.Min(producedItems, maxProduction);
        // Üretim kuyruğu işleme
        int queueToProcess = Mathf.Min(factoryData.productionQueue, producedItems);
        factoryData.productionQueue -= queueToProcess;
        // Nihai üretim miktarını belirleme
        if (factory.config != null && !factory.config.requiresInput)
        {
            producedItems = Mathf.Min(producedItems, maxProduction);
        }
        else
        {
            producedItems = queueToProcess;
        }
        // Fabrika verilerini güncelleme
        factoryData.currentStock += producedItems;
        factoryData.isProducing = (factoryData.currentStock < factory.config.capacity) &&
                                 (factoryData.productionQueue > 0 ||
                                 (factory.config != null && !factory.config.requiresInput));
        factoryData.remainingProductionTime = newRemainingTime;
        // Fabrika nesnesini güncelleme
        UpdateFactoryObject(factory, factoryData);
    }
    private int CalculateOfflineProduction(float elapsedTime, float remainingTime,
                                          float productionTime, out float newRemainingTime)
    {
        int producedItems = 0;
        if (remainingTime > 0)
        {
            if (elapsedTime >= remainingTime)
            {
                producedItems++;
                elapsedTime -= remainingTime;
                producedItems += Mathf.FloorToInt(elapsedTime / productionTime);
                newRemainingTime = productionTime - (elapsedTime % productionTime);
                if (Mathf.Approximately(newRemainingTime, productionTime))
                {
                    newRemainingTime = 0;
                }
            }
            else
            {
                newRemainingTime = remainingTime - elapsedTime;
            }
        }
        else
        {
            producedItems = Mathf.FloorToInt(elapsedTime / productionTime);
            newRemainingTime = productionTime - (elapsedTime % productionTime);
            if (Mathf.Approximately(newRemainingTime, productionTime))
            {
                newRemainingTime = 0;
            }
        }
        return producedItems;
    }
    private void UpdateFactoryObject(BaseFactory factory, FactoryData factoryData)
    {
        factory.SetStockDirectly(factoryData.currentStock);
        factory.SetProductionQueue(factoryData.productionQueue);
        if (factoryData.isProducing)
        {
            factory.SetRemainingProductionTime(factoryData.remainingProductionTime);
            factory.StartProduction();
        }
        else
        {
            factory.SetRemainingProductionTime(0);
        }
    }
    public void SaveGame()
    {
        SaveResourceData();
        SaveFactoryData();
        gameData.lastSaveTime = DateTime.Now;
        string json = JsonUtility.ToJson(gameData, true);
        System.IO.File.WriteAllText(saveFilePath, json);
        Debug.Log("Oyun kaydedildi: " + saveFilePath);
    }
    public void SaveResourceData()
    {
        if (resourceManager == null) return;
        gameData.resources.Clear();
        Dictionary<string, int> resourceDict = resourceManager.GetAllResources();
        foreach (var resource in resourceDict)
        {
            gameData.resources.Add(new ResourceData(resource.Key, resource.Value));
        }
    }
    public void SaveFactoryData()
    {
        if (factoryManager == null) return;
        gameData.factories.Clear();
        foreach (var factory in factoryManager.activeFactories)
        {
            float remainingTime = factory.IsProducing ? factory.GetRemainingProductionTime() : 0f;
            gameData.factories.Add(new FactoryData(
                factory.config.factoryName,
                factory.CurrentStock,
                factory.IsProducing,
                factory.ProductionQueue,
                remainingTime
            ));
        }
    }
    public void LoadGame()
    {
        LoadGameData();
        ApplyResourceData();
        ProcessOfflineProduction();
    }
    public List<FactoryData> GetSavedFactoryData()
    {
        return gameData.factories;
    }
    private void OnApplicationQuit()
    {
        SaveGame();
    }
}