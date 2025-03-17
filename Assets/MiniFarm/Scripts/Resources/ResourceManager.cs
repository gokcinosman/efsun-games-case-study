using UniRx;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ResourceManager : MonoBehaviour
{
    private Dictionary<string, int> resources = new Dictionary<string, int>();
    public IObservable<Dictionary<string, int>> OnResourceChanged => resourceSubject;
    private Subject<Dictionary<string, int>> resourceSubject = new Subject<Dictionary<string, int>>();
    public bool HasEnough(string resource, int amount)
    {
        return resources.ContainsKey(resource) && resources[resource] >= amount;
    }
    public void Add(string resource, int amount)
    {
        if (!resources.ContainsKey(resource))
        {
            resources[resource] = 0;
        }
        resources[resource] += amount;
        resourceSubject.OnNext(new Dictionary<string, int>(resources));
    }
    public void Consume(string resource, int amount)
    {
        if (!HasEnough(resource, amount)) return;
        resources[resource] -= amount;
        resourceSubject.OnNext(new Dictionary<string, int>(resources));
    }
    public int GetResourceAmount(string resource)
    {
        if (resources.ContainsKey(resource))
        {
            return resources[resource];
        }
        return 0;
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
