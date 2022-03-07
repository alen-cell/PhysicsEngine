using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SolidParticlesManager : IDisposable
{
    
   // private int resolution = 128;
    public int NumParticles { get; set; }
    public Bounds Bounds;
    private int Scale = 10;
   // public Transform transform;
    public float radius = 1f;

   // public float density;
    
    public RenderTexture DistanceField;
    
    ComputeShader BoundaryShader;
    //private ComputeBuffer positions;
    public ComputeBuffer SolidParticleBuffer;
    public ComputeBuffer Positions;
    public ComputeBuffer m_argsBuffer;

    public SolidParticle[] SolidParticlesList;
    
    public bool isCouplewithFluid;
    public struct SolidParticle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 forces;
        public float density;
        public float mass;
        public float pressure;
        //public bool doubleInteractive;  
        public static int stride = sizeof(float) * 12;
    }


    // Start is called before the first frame updat

    public  Vector3[] CreateSphere()
    {
        int resolution = Mathf.CeilToInt(Mathf.Sqrt(NumParticles));
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        //平移
        Vector3 Translate = new Vector3(25, 0, 25);
        Vector3[] positions = new Vector3[NumParticles];
        for (int i = 0, x = 0, z = 0; i < positions.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;

            }
            float u = (x + 0.5f) * step - 1f;
            float r = 0.9f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 4f * v));
            float s = r * Mathf.Cos(0.5f * Mathf.PI * v);
           
            
            positions[i].x = 10 * s *Mathf.Sin(Mathf.PI * u);
            positions[i].y = 10*r*Mathf.Sin(Mathf.PI *0.5f* v);
            positions[i].z = 10 * s *Mathf.Cos(Mathf.PI * u);
            positions[i] += Translate;
        }
        
        return positions;
    }
    public Vector3[] CreateCylinder()
    {
        int resolution = Mathf.CeilToInt(Mathf.Sqrt(NumParticles));
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        radius *= Scale;
        Vector3[] positions = new Vector3[NumParticles];
        Vector3 Translate = new Vector3(25, 12.5f, 25);
        for (int i = 0, x = 0, z = 0; i < positions.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;

            }
            float u = (x + 0.5f) * step - 1f;
            float r = Mathf.Cos(0.5f * Mathf.PI * v);
            //float r = 0.9f + 0.1f * Mathf.Sin(Mathf.PI * (6f * u + 4f * v));
            float s = r * Mathf.Cos(0.5f * Mathf.PI * v);

            positions[i] = Translate;
            positions[i].x += Scale * Mathf.Sin(Mathf.PI * u);
            positions[i].y += Scale * Mathf.Sin(Mathf.PI * 0.5f * v);
            positions[i].z += Scale * Mathf.Cos(Mathf.PI * u) ;

        }

        return positions;
    }



    public void InitBoundaryParticles(Vector3[]position, Matrix4x4 RTS)
    {

        SolidParticlesList = new SolidParticle[NumParticles] ;
        Vector4[] positions = new Vector4[NumParticles];

        float inf = float.PositiveInfinity;
        Vector3 min = new Vector3(inf, inf, inf);
        Vector3 max = new Vector3(-inf, -inf, -inf);
         


        for (int i = 0; i < NumParticles; i++)
        {

            Vector4 pos = RTS * position[i];
            if (pos.x < min.x) min.x = pos.x;
            if (pos.y < min.y) min.y = pos.y;
            if (pos.z < min.z) min.z = pos.z;

            if (pos.x > max.x) max.x = pos.x;
            if (pos.y > max.y) max.y = pos.y;
            if (pos.z > max.z) max.z = pos.z;
            SolidParticlesList[i].position = position[i];
            SolidParticlesList[i].velocity = Vector3.zero;
            SolidParticlesList[i].forces = Vector3.zero;
            SolidParticlesList[i].density = 0.0f;
            SolidParticlesList[i].pressure = 0.0f;
            positions[i] = position[i];



        }

        min.x -= radius;
        min.y -= radius;
        min.z -= radius;

        max.x += radius;
        max.y += radius;
        max.z += radius;

        Bounds = new Bounds();
        Bounds.SetMinMax(min, max);
        //
        SolidParticleBuffer = new ComputeBuffer(NumParticles, SolidParticle.stride);
        SolidParticleBuffer.SetData(SolidParticlesList);
        //
        Positions = new ComputeBuffer(NumParticles, 4 * sizeof(float));
        Positions.SetData(positions);

    }

    public void ConfigBoundary(ComputeShader computeShader)
    {
        
       
        int hashBounary = computeShader.FindKernel("hashBoundary");
        computeShader.SetBuffer(hashBounary, "_boundaryParticles", SolidParticleBuffer);



    }
    public void Draw(Camera cam,Mesh mesh,Material material,int layer)
    {
        if (m_argsBuffer == null)
            CreateArgBuffer(mesh.GetIndexCount(0));
        //单独的Buffer
        material.SetBuffer("positions", Positions);


        material.SetBuffer("_boundaryParticles", SolidParticleBuffer);
        material.SetColor("color", Color.cyan);
        material.SetFloat("radius", radius);
        ShadowCastingMode castShadow = ShadowCastingMode.On;
        bool receiveShadow = true;
        Graphics.DrawMeshInstancedIndirect(mesh, 0,  material, Bounds, m_argsBuffer, 0,  null, castShadow,  receiveShadow, 0);
    }




    private void CreateArgBuffer(uint indexCount)
    {
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = indexCount;
        args[1] = (uint)NumParticles;
        m_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);
    }


   
   


    public void Dispose()
    {
        if(Positions!=null)
        {
            Positions.Release();
            Positions = null;
        }
        m_argsBuffer.Release();
        SolidParticleBuffer.Release();
    }

}
    
