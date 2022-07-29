using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;

public class FluidParticles : IDisposable
{

    public int NumParticles { get; private set; }
    
    public Bounds Bounds;
    public float Density0 { get; set; }
    public float InvDensity0 { get { return (float)(1.0f / Density0); } }
    public float Viscosity { get; set; }
    public float Damping { get; set; }
    public float Radius { get; private set; }
    public float Diameter { get { return Radius * 2.0f; } }

    public float ParticleVolume { get; private set; }

    public float ParticleMass { get; set; }

    
   // public ComputeBuffer pressures { get; private set; } 
    public ComputeBuffer lambda { get; private set; }
    public ComputeBuffer densities { get; private set; }
    public ComputeBuffer positions { get; private set; }
    public ComputeBuffer predicted { get; private set; }
    public ComputeBuffer velocities { get; private set; }

   

    private ComputeBuffer m_argsBuffer;

    public FluidParticles(ParticleSource source,float radius,float density,Matrix4x4 RTS)
    {
        NumParticles = source.NumParticles;
        Density0 = density;
        Viscosity = 0.002f;
        Damping = 0.0f;
      
        Radius = radius;
        ParticleVolume = (4.0f / 3.0f) * Mathf.PI * Mathf.Pow(radius, 3);
        ParticleMass = ParticleVolume * Density0;

        densities = new ComputeBuffer(NumParticles, sizeof(float));
       // pressures = new ComputeBuffer(NumParticles, sizeof(float));
        lambda = new ComputeBuffer(NumParticles, sizeof(float));
        CreateParticles(ref source, RTS);

    }

    
    
    public void CreateParticles(ref ParticleSource source, Matrix4x4 RTS)
    {
         //Vector3[] postest =  source.CreateSphere();
        Vector4[] position = new Vector4[NumParticles];
        Vector4[] predicted = new Vector4[NumParticles];
        Vector4[] velocities = new Vector4[NumParticles];

        float inf = float.PositiveInfinity;
        Vector3 min = new Vector3(inf, inf, inf);
        Vector3 max = new Vector3(-inf, -inf, -inf);

        for (int i = 0; i < NumParticles; i++)
        {
            Vector4 pos = RTS * source.Positions[i];
            predicted[i] = pos;
            position[i] = pos;

            if (pos.x < min.x) min.x = pos.x - Radius;
            if (pos.y < min.y) min.y = pos.y - Radius;
            if (pos.z < min.z) min.z = pos.z - Radius;

            if (pos.x > max.x) max.x = pos.x + Radius;
            if (pos.y > max.y) max.y = pos.y + Radius;
            if (pos.z > max.z) max.z = pos.z + Radius;

        }
        Bounds = new Bounds();
        Bounds.SetMinMax(min, max);

        positions = new ComputeBuffer(NumParticles, 4 * sizeof(float));
        positions.SetData(position);
        
       
        this.predicted = new ComputeBuffer(NumParticles, 4*sizeof(float));
        this.predicted.SetData(predicted);
      

      
        this.velocities = new ComputeBuffer(NumParticles, 4 * sizeof(float));
        this.velocities.SetData(velocities);
       




    }
  
    public static void Release(IList<ComputeBuffer> buffers)
    {
        if (buffers == null) return;

        int count = buffers.Count;
        for (int i = 0; i < count; i++)
        {
            if (buffers[i] == null) continue;
            buffers[i].Release();
            buffers[i] = null;
        }
    }
    public void Dispose()
    {
        if (positions != null)
        {
            positions.Release();
            positions = null;
        }
        if (densities != null)
        {
            densities.Release();
            densities = null;
        }
       // if (pressures != null)

        //{
           // pressures.Release();
          //  pressures = null;
       // }
        predicted.Release();
        velocities.Release();
        CBUtility.Release(ref m_argsBuffer);
        lambda.Release();
    }
   
    public void Draw(Camera cam,Mesh mesh,Material material,int layer)
    {
     

        if (m_argsBuffer == null)
            CreateArgBuffer(mesh.GetIndexCount(0));

        material.SetBuffer("_positionsBuffer", this.positions);
        material.SetColor("color", Color.cyan);
        
        material.SetFloat("radius", Radius);
        ShadowCastingMode castShadow = ShadowCastingMode.On;
        bool receiveShadow = true;

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, Bounds, m_argsBuffer, 0, null, castShadow, receiveShadow, layer, cam);

    }
      private void CreateArgBuffer(uint indexCount)
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = indexCount;
        args[1] = (uint)NumParticles;
        m_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);
    }

}
