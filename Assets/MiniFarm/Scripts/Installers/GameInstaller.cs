using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ResourceManager>()
                 .FromComponentInHierarchy()
                 .AsSingle()
                 .NonLazy();
        Container.Bind<SaveManager>()
       .FromComponentInHierarchy()
       .AsSingle()
       .NonLazy();
        Container.Bind<FactoryManager>()
       .FromComponentInHierarchy()
       .AsSingle()
       .NonLazy();
    }
}
