using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;
public class BaseFactory : MonoBehaviour
{
    [SerializeField] protected int capacity;
    [SerializeField] protected Recipe recipe;
    protected bool isProducing = false;
    protected int currentStock = 0;
    private IDisposable productionCoroutine; // UniRX timer
    public IObservable<int> OnStockChanged => stockSubject;
    private Subject<int> stockSubject = new Subject<int>();
    [Inject] protected ResourceManager resourceManager;
    protected virtual void Start()
    {
        stockSubject.OnNext(currentStock);
    }
    public void AddProductionOrder()
    {
        if (currentStock >= capacity) return;
        if (!recipe.requiresInput)
        {
            StartProduction();
            return;
        }
        foreach (var requirement in recipe.requirements)
        {
            if (!resourceManager.HasEnough(requirement.resourceName, requirement.amount))
                return;
        }
        foreach (var requirement in recipe.requirements)
        {
            resourceManager.Consume(requirement.resourceName, requirement.amount);
        }
        StartProduction();
    }
    public void StartProduction()
    {
        if (currentStock >= capacity) return;
        isProducing = true;
        productionCoroutine = Observable.Timer(TimeSpan.FromSeconds(recipe.productionTime)).Repeat().Subscribe(_ =>
        {
            ProduceItem();
        });
    }
    private void ProduceItem()
    {
        if (currentStock < capacity)
        {
            currentStock += recipe.outputAmount;
            stockSubject.OnNext(currentStock);
        }
        else
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
}
