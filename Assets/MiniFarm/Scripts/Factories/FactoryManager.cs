using System.Collections.Generic;
using UnityEngine;
using Zenject;
public class FactoryManager : MonoBehaviour
{
    [SerializeField] private List<FactoryConfig> availableFactories = new List<FactoryConfig>();
    [Inject] private DiContainer container;
    public BaseFactory CreateFactory(string factoryName, Vector3 position)
    {
        FactoryConfig config = availableFactories.Find(f => f.factoryName == factoryName);
        if (config == null) return null;
        GameObject factoryObject = container.InstantiatePrefab(config.factoryPrefab, position, Quaternion.identity, null);
        BaseFactory factory = factoryObject.GetComponent<BaseFactory>();
        return factory;
    }
    public List<FactoryConfig> GetAvailableFactories()
    {
        return availableFactories;
    }
}