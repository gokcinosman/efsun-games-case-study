using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewFactoryConfig", menuName = "MiniFarm/Factory Config")]
public class FactoryConfig : ScriptableObject
{
    public string factoryName;
    public GameObject factoryPrefab;
    public Recipe recipe;
    public bool requiresInput = true;
    public int capacity = 5;
    public int removeAmount = 1;
    public int addAmount = 1;
}