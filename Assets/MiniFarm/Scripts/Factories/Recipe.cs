using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class Recipe
{
    public string outputResourceName;
    public Sprite outputResourceIcon;
    public int outputAmount = 1;
    public float productionTime = 5f;
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
}
[Serializable]
public class ResourceRequirement
{
    public string resourceName;
    public Sprite resourceIcon;
    public int amount;
}