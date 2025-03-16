using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ResourceManager>()
                 .FromComponentInHierarchy() // Sahnedeki instance'ı kullan
                 .AsSingle()
                 .NonLazy();
    }
}
