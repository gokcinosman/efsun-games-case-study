using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class HayFactory : BaseFactory
{
    protected override void Start()
    {
        base.Start();
        StartProduction();
    }
}
