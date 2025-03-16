using UnityEngine;
using Zenject;
public class BreadFactory : BaseFactory
{
    [Inject] private ResourceManager resourceManager;
    public void AddProductionOrder()
    {
        if (currentStock >= capacity) return;
        if (!resourceManager.HasEnough("Flour", 2)) return;
        resourceManager.Consume("Flour", 2);
        StartProduction();
    }
}
