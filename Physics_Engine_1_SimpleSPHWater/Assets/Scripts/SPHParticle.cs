using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fluid
{

    public struct Particle
    {
        public float mass;
        public Vector3 position;
        public Vector4 colorGradient;
        public Vector3 velocity;
        //public int onSurface;
        public float density;

        //public Vector3 midVelocity;
        //public Vector3 prevVelocity;
        public float pressure;
        public Vector3 forces;
        
        

    }
}

