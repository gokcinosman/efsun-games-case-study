using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;
public class BaseFactory : MonoBehaviour
{
    [SerializeField] protected int capacity;
    [SerializeField] protected Recipe recipe;
    protected bool isProducing = false;
    protected int currentStock = 0;
    public int CurrentStock => currentStock;
    public FactoryConfig config;
    private IDisposable productionCoroutine; // UniRX timer
    public IObservable<int> OnStockChanged => stockSubject;
    private Subject<int> stockSubject = new Subject<int>();
    [Inject] protected ResourceManager resourceManager;
    [SerializeField] private ResourceUI resourceUI;
    [SerializeField] private ProductionUI productionUI;
    public ProductionUI ProductionUI => productionUI;
    protected virtual void Start()
    {
        if (config != null)
        {
            capacity = config.capacity;
            recipe = config.recipe;
        }
        stockSubject.OnNext(currentStock);
        if (resourceUI != null && productionUI != null)
        {
            resourceUI.Initialize(this);
            productionUI.Initialize(this);
        }
        if (recipe != null && config != null && !config.requiresInput && currentStock < capacity)
        {
            StartProduction();
        }
    }
    public void AddProductionOrder()
    {
        if (currentStock >= capacity) return;
        if (!config.requiresInput)
        {
            StartProduction();
            return;
        }
        foreach (var requirement in recipe.requirements)
        {
            bool hasEnough = resourceManager.HasEnough(requirement.resourceName, requirement.amount);
            Debug.Log($"Gereksinim: {requirement.resourceName}, Miktar: {requirement.amount}, Yeterli mi: {hasEnough}");
            if (!hasEnough)
                return;
        }
        foreach (var requirement in recipe.requirements)
        {
            resourceManager.Consume(requirement.resourceName, requirement.amount);
            Debug.Log($"{requirement.amount} adet {requirement.resourceName} tüketildi");
        }
        StartProduction();
    }
    public void RemoveProductionOrder()
    {
        if (isProducing)
        {
            isProducing = false;
            productionCoroutine?.Dispose();
            Debug.Log("Üretim durduruldu");
        }
        if (config != null && recipe != null)
        {
            foreach (var requirement in recipe.requirements)
            {
                resourceManager.Add(requirement.resourceName, requirement.amount);
                Debug.Log($"{requirement.amount} adet {requirement.resourceName} geri verildi");
            }
        }
    }
    public void StartProduction()
    {
        if (currentStock >= capacity)
        {
            isProducing = false;
            return;
        }
        isProducing = true;
        productionCoroutine = Observable.Timer(TimeSpan.FromSeconds(recipe.productionTime)).Repeat().Subscribe(_ =>
        {
            ProduceItem();
        });
    }
    private void ProduceItem()
    {
        if (currentStock >= capacity)
        {
            isProducing = false;
            productionCoroutine?.Dispose();
            return;
        }
        currentStock += recipe.outputAmount;
        stockSubject.OnNext(currentStock);
        if (currentStock >= capacity)
        {
            isProducing = false;
            productionCoroutine?.Dispose();
        }
    }
    public bool IsProducing()
    {
        return isProducing;
    }
    public int CollectItems()
    {
        int collected = currentStock;
        currentStock = 0;
        stockSubject.OnNext(currentStock);
        return collected;
    }
    protected virtual void OnDestroy()
    {
        isProducing = false;
        productionCoroutine?.Dispose(); // UniRx timer dispose
    }
    public void SetCapacity(int newCapacity)
    {
        capacity = newCapacity;
    }
    public void SetRecipe(Recipe newRecipe)
    {
        recipe = newRecipe;
    }
}
