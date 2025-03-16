using UnityEngine;
using Zenject;
public class FlourFactory : BaseFactory
{
    private ResourceManager resourceManager;
    [Inject]
    public void Construct(ResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }
    public void AddProductionOrder()
    {
        if (currentStock >= capacity) return;
        if (!resourceManager.HasEnough("Wheat", 1)) return;
        resourceManager.Consume("Wheat", 1);
        StartProduction();
    }
}
