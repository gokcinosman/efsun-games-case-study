using UniRx;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using Zenject;
public class ResourceManager : MonoBehaviour
{
    private Dictionary<string, int> resources = new Dictionary<string, int>();
    public IObservable<Dictionary<string, int>> OnResourceChanged => resourceSubject;
    private Subject<Dictionary<string, int>> resourceSubject = new Subject<Dictionary<string, int>>();
    [Header("Kaynak Animasyon Ayarları")]
    [SerializeField] private float animationSpeed = 0.02f;
    [SerializeField] private int incrementStep = 1;
    private Dictionary<string, bool> animatingResources = new Dictionary<string, bool>();
    public bool HasEnough(string resource, int amount)
    {
        return resources.ContainsKey(resource) && resources[resource] >= amount;
    }
    public async void Add(string resource, int amount)
    {
        if (!resources.ContainsKey(resource))
        {
            resources[resource] = 0;
        }
        if (animatingResources.TryGetValue(resource, out bool isAnimating) && isAnimating)
        {
            resources[resource] += amount;
            return;
        }
        await AnimateResourceIncrease(resource, amount);
    }
    private async UniTask AnimateResourceIncrease(string resource, int targetAmount)
    {
        animatingResources[resource] = true;
        int originalValue = resources[resource];
        int currentValue = originalValue;
        int targetValue = originalValue + targetAmount;
        try
        {
            while (currentValue < targetValue)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(animationSpeed));
                int increment = Math.Min(incrementStep, targetValue - currentValue);
                currentValue += increment;
                resources[resource] = currentValue;
                resourceSubject.OnNext(new Dictionary<string, int>(resources));
            }
        }
        finally
        {
            animatingResources[resource] = false;
            resources[resource] = targetValue;
            resourceSubject.OnNext(new Dictionary<string, int>(resources));
        }
    }
    public void Consume(string resource, int amount)
    {
        if (!HasEnough(resource, amount)) return;
        resources[resource] -= amount;
        resourceSubject.OnNext(new Dictionary<string, int>(resources));
    }
    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(resources);
    }
    public void SetResource(string resource, int amount)
    {
        resources[resource] = amount;
        resourceSubject.OnNext(new Dictionary<string, int>(resources));
    }
}
