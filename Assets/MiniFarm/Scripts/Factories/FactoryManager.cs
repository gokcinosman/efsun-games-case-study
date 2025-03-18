using System.Collections.Generic;
using UniRx;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
public class FactoryManager : MonoBehaviour
{
    [Inject] private DiContainer container;
    [Inject] private SaveManager saveManager;
    [SerializeField] private Transform factoryStartPoint;
    [SerializeField] private List<FactoryConfig> availableFactories = new List<FactoryConfig>();
    [Header("Grid Layout Settings")]
    [SerializeField] private float spacingX = 2f;
    [SerializeField] private float spacingZ = 2f;
    [SerializeField] private int maxColumnsPerRow = 3;
    public List<BaseFactory> activeFactories = new List<BaseFactory>();
    // ReactiveProperty - fabrikaların güncel durumunu izlemek için
    public ReactiveCollection<BaseFactory> FactoriesCollection { get; private set; } = new ReactiveCollection<BaseFactory>();
    private void Start()
    {
        InitializeFactories();
        LoadFactoryStocksAsync().Forget();
    }
    private async UniTaskVoid LoadFactoryStocksAsync()
    {
        await UniTask.DelayFrame(1);
        List<FactoryData> savedFactories = saveManager.GetSavedFactoryData();
        if (savedFactories != null && savedFactories.Count > 0)
        {
            foreach (var savedFactory in savedFactories)
            {
                var factoryToUpdate = activeFactories.Find(f => f.config.factoryName == savedFactory.factoryName);
                if (factoryToUpdate != null)
                {
                    Debug.Log($"Fabrika stokunu yüklüyorum: {factoryToUpdate.config.factoryName}, Stok: {savedFactory.currentStock}");
                    SetFactoryStock(factoryToUpdate, savedFactory.currentStock);
                }
            }
            foreach (var factory in activeFactories)
            {
                Debug.Log($"Fabrika son durumu: {factory.config.factoryName}, Stok: {factory.CurrentStock}");
            }
        }
    }
    private void InitializeFactories()
    {
        if (factoryStartPoint == null)
        {
            Debug.LogError("Fabrika başlangıç noktası ayarlanmamış!");
            return;
        }
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
        // Fabrikaları oluştur
        activeFactories = CreateFactoriesInGrid(
            factoryNames,
            factoryStartPoint.position
        );
        // ReactiveCollection'a ekle
        FactoriesCollection.Clear();
        foreach (var factory in activeFactories)
        {
            FactoriesCollection.Add(factory);
        }
    }
    private void SetFactoryStock(BaseFactory factory, int stockAmount)
    {
        Debug.Log($"Fabrika stoku ayarlanıyor: {factory.config.factoryName}, Stok: {stockAmount}");
        factory.SetStockDirectly(stockAmount);
    }
    public BaseFactory CreateFactory(string factoryName, Vector3 position)
    {
        FactoryConfig config = availableFactories.Find(f => f.factoryName == factoryName);
        if (config == null) return null;
        GameObject factoryObject = container.InstantiatePrefab(config.factoryPrefab, position, Quaternion.identity, null);
        BaseFactory factory = factoryObject.GetComponent<BaseFactory>();
        factory.config = config;
        return factory;
    }
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
    public List<FactoryConfig> GetAvailableFactories()
    {
        return availableFactories;
    }
}