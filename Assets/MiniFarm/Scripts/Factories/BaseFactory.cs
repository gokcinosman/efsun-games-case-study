using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
public class BaseFactory : MonoBehaviour
{
    [SerializeField] protected int capacity;
    [SerializeField] protected float productionTime;
    protected int currentStock = 0;
    private IDisposable productionCoroutine; // UniRX timer
    public IObservable<int> OnStockChanged => stockSubject;
    private Subject<int> stockSubject = new Subject<int>();
    protected virtual void Start()
    {
        stockSubject.OnNext(currentStock);
    }
    public void StartProduction()
    {
        if (currentStock >= capacity) return;
        productionCoroutine = Observable.Timer(TimeSpan.FromSeconds(productionTime)).Repeat().Subscribe(_ =>
        {
            ProduceItem();
        });
    }
    private void ProduceItem()
    {
        if (currentStock < capacity)
        {
            currentStock++;
            stockSubject.OnNext(currentStock);
        }
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
        productionCoroutine?.Dispose(); // UniRx timer dispose
    }
}
