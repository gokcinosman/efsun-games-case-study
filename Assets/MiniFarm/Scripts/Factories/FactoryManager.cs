using System.Collections.Generic;
using UnityEngine;
using Zenject;
public class FactoryManager : MonoBehaviour
{
    [SerializeField] private Transform factoryStartPoint;
    [SerializeField] private List<FactoryConfig> availableFactories = new List<FactoryConfig>();
    [Inject] private DiContainer container;
    [Header("Grid Layout Settings")]
    [SerializeField] private float spacingX = 2f;
    [SerializeField] private float spacingZ = 2f;
    [SerializeField] private int maxColumnsPerRow = 3;
    private void Start()
    {
        InitializeFactories();
    }
    private void InitializeFactories()
    {
        if (factoryStartPoint == null)
        {
            Debug.LogError("Fabrika başlangıç noktası ayarlanmamış!");
            return;
        }
        List<string> factoryNames = new List<string>();
        foreach (var factory in GetAvailableFactories())
        {
            factoryNames.Add(factory.factoryName);
        }
        List<BaseFactory> createdFactories = CreateFactoriesInGrid(
            factoryNames,
            factoryStartPoint.position
        );
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