using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothingKernel
{
    public float POLY6 { get; private set; }
    public float SPIKY_GRAD { get; private set; }
    public float VISC_LAP { get; private set; }
    
    public float Radius { get; private set; }

    public float InvRadius { get; private set; }

    public float Radius2 { get; private set; }

    public SmoothingKernel(float radius)
    {
        Radius = radius;
        Radius2 = radius * radius;
        InvRadius = 1.0f / radius;

        float PI = Mathf.PI;

        POLY6 = 315.0f / (65.0f * PI * Mathf.Pow(Radius, 9.0f));
        SPIKY_GRAD = -45.0f / (PI * Mathf.Pow(Radius, 6.0f));
        VISC_LAP = 45.0f / (PI * Mathf.Pow(Radius, 6.0f));

    }



}
