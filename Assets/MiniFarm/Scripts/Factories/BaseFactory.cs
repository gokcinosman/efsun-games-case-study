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
    protected int productionQueue = 0;
    public int ProductionQueue => productionQueue;
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
        if (currentStock >= capacity)
        {
            return;
        }
        if (!config.requiresInput)
        {
            StartProduction();
            return;
        }
        int potentialProduction = productionQueue + currentStock + recipe.outputAmount;
        if (potentialProduction > capacity)
        {
            return;
        }
        foreach (var requirement in recipe.requirements)
        {
            bool hasEnough = resourceManager.HasEnough(requirement.resourceName, requirement.amount);
            if (!hasEnough)
            {
                return;
            }
        }
        foreach (var requirement in recipe.requirements)
        {
            resourceManager.Consume(requirement.resourceName, requirement.amount);
        }
        productionQueue += recipe.outputAmount;
        stockSubject.OnNext(currentStock);
        if (!isProducing)
        {
            StartProduction();
        }
    }
    public void RemoveProductionOrder()
    {
        if (productionQueue >= recipe.outputAmount && config != null && recipe != null)
        {
            productionQueue -= recipe.outputAmount;
            foreach (var requirement in recipe.requirements)
            {
                resourceManager.Add(requirement.resourceName, requirement.amount);
            }
            stockSubject.OnNext(currentStock);
            if (productionQueue <= 0)
            {
                isProducing = false;
                productionCoroutine?.Dispose();
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
        if (productionQueue <= 0 && (config == null || config.requiresInput))
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
        if (productionQueue > 0)
        {
            productionQueue -= recipe.outputAmount;
            currentStock += recipe.outputAmount;
        }
        else if (config != null && !config.requiresInput)
        {
            currentStock += recipe.outputAmount;
        }
        else
        {
            isProducing = false;
            productionCoroutine?.Dispose();
            return;
        }
        stockSubject.OnNext(currentStock);
        if (currentStock >= capacity)
        {
            isProducing = false;
            productionCoroutine?.Dispose();
        }
        else if (productionQueue <= 0 && (config == null || config.requiresInput))
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
        if (collected > 0 && config != null && recipe != null)
        {
            string outputResourceName = recipe.outputResourceName;
            resourceManager.Add(outputResourceName, collected);
        }
        if (config != null && !config.requiresInput && currentStock < capacity && !isProducing)
        {
            StartProduction();
        }
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
