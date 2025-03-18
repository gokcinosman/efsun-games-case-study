using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Zenject.ReflectionBaking.Mono.Cecil;
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
        Container.Bind<ResourceAnimation>()
       .FromComponentInHierarchy()
       .AsSingle()
       .NonLazy();
        Container.Bind<CurrencyUI>()
      .FromComponentInHierarchy()
      .AsSingle()
      .NonLazy();
    }
}
