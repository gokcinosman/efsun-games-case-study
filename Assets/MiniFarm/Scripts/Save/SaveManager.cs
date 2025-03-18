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
    public FactoryData(string name, int stock)
    {
        factoryName = name;
        currentStock = stock;
    }
}
[Serializable]
public class GameData
{   // hammaddelerin miktarı lazım -bitti-
    // hammaddelerin isimleri lazım -bitti-
    //   --------factory data--------
    // factory id lazım -bitti-
    // production queue lazım
    // stock lazım -bitti-
    // isproducing değişkeni lazım
    // production progressin son anı lazım
    // buraya değil ama genel bir production progress hesaplaması lazım
    // production progress hesaplaması için factorynin üretim süresi lazım
    // remaning time gibi bir şey yani
    public List<ResourceData> resources = new List<ResourceData>();
    public List<FactoryData> factories = new List<FactoryData>();
}
public class SaveManager : MonoBehaviour
{
    [Inject]
    ResourceManager resourceManager;
    [Inject]
    FactoryManager factoryManager;
    private GameData gameData = new GameData();
    private string saveFilePath;
    private void Awake()
    {
        saveFilePath = Application.persistentDataPath + "/save.json";
        // Fabrikalar oluşmadan önce veriyi yükleyelim
        if (System.IO.File.Exists(saveFilePath))
        {
            string json = System.IO.File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            Debug.Log("Kayıt yok");
            gameData = new GameData();
        }
    }
    private void Start()
    {
        // Fabrikalar oluşturulduktan sonra kaynak yükleyelim
        if (resourceManager != null)
        {
            foreach (var resource in gameData.resources)
            {
                resourceManager.SetResource(resource.resourceName, resource.amount);
            }
        }
        Debug.Log(gameData.factories.Count + " fabrika verisi ");
    }
    public void SaveGame()
    {
        Debug.Log("Oyun kaydediliyor");
        SaveResourceData();
        SaveFactoryData();
        foreach (var factory in gameData.factories)
        {
            Debug.Log($"Kaydedilen fabrika verisi {factory.factoryName}, Stok {factory.currentStock}");
        }
        string json = JsonUtility.ToJson(gameData, true);
        System.IO.File.WriteAllText(saveFilePath, json);
        Debug.Log("Oyun kaydedildi " + saveFilePath);
    }
    public void SaveResourceData()
    {
        if (resourceManager != null)
        {
            gameData.resources.Clear();
            Dictionary<string, int> resourceDict = resourceManager.GetAllResources();
            foreach (var resource in resourceDict)
            {
                gameData.resources.Add(new ResourceData(resource.Key, resource.Value));
            }
        }
    }
    public void SaveFactoryData()
    {
        if (factoryManager != null)
        {
            gameData.factories.Clear();
            foreach (var factory in factoryManager.activeFactories)
            {
                gameData.factories.Add(new FactoryData(factory.config.factoryName, factory.CurrentStock));
            }
        }
    }
    public void LoadGame()
    {
        if (System.IO.File.Exists(saveFilePath))
        {
            string json = System.IO.File.ReadAllText(saveFilePath);
            gameData = JsonUtility.FromJson<GameData>(json);
            if (resourceManager != null)
            {
                foreach (var resource in gameData.resources)
                {
                    resourceManager.SetResource(resource.resourceName, resource.amount);
                }
            }
            Debug.Log("Oyun yüklendi " + gameData.factories.Count + " fabrika verisi");
        }
        else
        {
            Debug.Log("Kayıt dosyası yok");
            gameData = new GameData();
        }
    }
    public List<FactoryData> GetSavedFactoryData()
    {
        return gameData.factories;
    }
    private void OnApplicationQuit()
    {
        SaveGame();
        Debug.Log("Uygulama kapanırken oyun kaydedildi");
    }
}
