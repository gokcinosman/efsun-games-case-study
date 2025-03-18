using System.Collections.Generic;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
public class FactoryManager : MonoBehaviour
{
    #region Bağımlılıklar ve Serileştirilen Değişkenler
    [Inject] private DiContainer container;
    [Inject] private SaveManager saveManager;
    [SerializeField] private Transform factoryStartPoint;
    [SerializeField] private List<FactoryConfig> availableFactories = new List<FactoryConfig>();
    [Header("Grid Layout Settings")]
    [SerializeField] private float spacingX = 2f;
    [SerializeField] private float spacingZ = 2f;
    [SerializeField] private int maxColumnsPerRow = 3;
    #endregion
    #region Genel Özellikler
    public List<BaseFactory> activeFactories = new List<BaseFactory>();
    public ReactiveCollection<BaseFactory> FactoriesCollection { get; private set; } = new ReactiveCollection<BaseFactory>();
    #endregion
    #region Unity Yaşam Döngüsü
    private void Start()
    {
        InitializeFactories();
        LoadFactoryDataAsync().Forget();
    }
    #endregion
    #region Fabrika Başlatma ve Yükleme
    private void InitializeFactories()
    {
        if (factoryStartPoint == null)
        {
            Debug.LogError("Fabrika başlangıç noktası ayarlanmamış!");
            return;
        }
        List<string> factoryNames = GetInitialFactoryNames();
        activeFactories = CreateFactoriesInGrid(factoryNames, factoryStartPoint.position);
        // ReactiveCollection'a fabrikaları ekle
        FactoriesCollection.Clear();
        foreach (var factory in activeFactories)
        {
            FactoriesCollection.Add(factory);
        }
    }
    private List<string> GetInitialFactoryNames()
    {
        List<string> factoryNames = new List<string>();
        List<FactoryData> savedFactories = saveManager.GetSavedFactoryData();
        if (savedFactories != null && savedFactories.Count > 0)
        {
            foreach (var factoryData in savedFactories)
            {
                factoryNames.Add(factoryData.factoryName);
            }
        }
        else
        {
            // Varsayılan fabrikaları kullan
            foreach (var factory in GetAvailableFactories())
            {
                factoryNames.Add(factory.factoryName);
            }
        }
        return factoryNames;
    }
    private async UniTaskVoid LoadFactoryDataAsync()
    {
        // Fabrikaların oluşturulmasına zaman tanımak için kısa bekleme
        await UniTask.Delay(100);
        List<FactoryData> savedFactories = saveManager.GetSavedFactoryData();
        if (savedFactories == null || savedFactories.Count == 0)
        {
            return;
        }
        foreach (var savedFactory in savedFactories)
        {
            ApplySavedDataToFactory(savedFactory);
        }
    }
    private void ApplySavedDataToFactory(FactoryData savedFactory)
    {
        BaseFactory factoryToUpdate = GetFactoryByName(savedFactory.factoryName);
        if (factoryToUpdate == null)
        {
            return;
        }
        // Fabrika verilerini güncelle
        factoryToUpdate.SetStockDirectly(savedFactory.currentStock);
        factoryToUpdate.SetProductionQueue(savedFactory.productionQueue);
        if (savedFactory.isProducing && savedFactory.currentStock < factoryToUpdate.config.capacity)
        {
            // Üretim devam ediyor ise kalan süreyi ayarla ve başlat
            factoryToUpdate.SetRemainingProductionTime(savedFactory.remainingProductionTime);
            factoryToUpdate.StartProduction();
        }
    }
    #endregion
    #region Fabrika Oluşturma
    public List<BaseFactory> CreateFactoriesInGrid(List<string> factoryNames, Vector3 startPosition)
    {
        List<BaseFactory> createdFactories = new List<BaseFactory>();
        for (int i = 0; i < factoryNames.Count; i++)
        {
            int row = i / maxColumnsPerRow;
            int column = i % maxColumnsPerRow;
            Vector3 position = new Vector3(
                startPosition.x + column * spacingX,
                startPosition.y,
                startPosition.z - row * spacingZ
            );
            BaseFactory factory = CreateFactory(factoryNames[i], position);
            if (factory != null)
            {
                createdFactories.Add(factory);
            }
        }
        return createdFactories;
    }
    public BaseFactory CreateFactory(string factoryName, Vector3 position)
    {
        FactoryConfig config = availableFactories.Find(f => f.factoryName == factoryName);
        if (config == null) return null;
        GameObject factoryObject = container.InstantiatePrefab(
            config.factoryPrefab,
            position,
            Quaternion.identity,
            null
        );
        BaseFactory factory = factoryObject.GetComponent<BaseFactory>();
        factory.config = config;
        return factory;
    }
    #endregion
    #region Yardımcı Metodlar
    public BaseFactory GetFactoryByName(string factoryName)
    {
        return activeFactories.Find(f => f.config.factoryName == factoryName);
    }
    public List<FactoryConfig> GetAvailableFactories()
    {
        return availableFactories;
    }
    #endregion
}