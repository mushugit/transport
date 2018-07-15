﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepotObjectRender : MonoBehaviour, IUnityObjectRenderer
{
    public Construction NestedConstruction { get; set; }

    void Start()
    {
        HColor.GetInitialColor(NestedConstruction, this);
    }

}
