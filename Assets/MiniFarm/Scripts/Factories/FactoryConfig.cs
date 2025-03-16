using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewFactoryConfig", menuName = "MiniFarm/Factory Config")]
public class FactoryConfig : ScriptableObject
{
    public string factoryName;
    public Sprite factoryIcon;
    public GameObject factoryPrefab;
    public Recipe recipe;
    public int capacity = 5;
}