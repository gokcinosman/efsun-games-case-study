using System;
using System.Collections.Generic;
[Serializable]
public class Recipe
{
    public string outputResourceName;
    public int outputAmount = 1;
    public float productionTime = 5f;
    public bool requiresInput = true;
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
}
[Serializable]
public class ResourceRequirement
{
    public string resourceName;
    public int amount;
}