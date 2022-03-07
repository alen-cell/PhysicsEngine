using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Runtime.InteropServices;
using System;

public class PBDFluidSolver : MonoBehaviour
{
    [SerializeField]
    [Header("Particle properties")]
    public float Supportradius = 2f;
    public Mesh particleMesh;
    Transform pointPrefab;
    public float particleRenderSize = 1f;



    public Material material;
 
    public float mass = 4f;
    public float gasConstant = 2000f;
    public float restDensity = 9f;
   
    //粘性
    public float viscosityCoefficient = 2.5f;
    public float[] gravity = { 0.0f, -9.81f, 0.0f };
    //阻尼
    [SerializeField, Range(-0.99f, 0)]
    public float damping = -0.37f;
    public float dt = 0.01f;
    
    [Header("BoundaryAttributes")]
    [SerializeField]
    ComputeShader BoundaryShader;
    public float solidPressure;
    public int doubleInteractive;
    public int numberOfBoundaryParticles = 100;
    public Material BoundaryMaterial;
    public float BoundaryParticleradius = 2f;


    public Bounds bounds;
    private float volume;
    private float radius2;
    private float radius3;
    private float radius4;
    private float radius5;
 
    private int currParticleNum { get { return _particles.Length; } }



    [Header("Simulation space properties")]
    public int numberOfParticles = 2000;
    public int dimensions = 100;
    public int maximumParticlesPerCell = 100;

    [Header("Debug information")]
    [Tooltip("Tracks how many neighbours each particleIndex has in" + nameof(_neighbourList))]

    private int[] _neighbourTracker;
    private Particle[] _particles;

    private int[] _neighbourList;
    private uint[] _hashGrid;
    private uint[] _hashGridTracker;
    private int[] _test;


    private ComputeBuffer _particlesBuffer;
    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _neighbourListBuffer;
    private ComputeBuffer _neighbourTrackerBuffer;
    private ComputeBuffer _hashGridBuffer;
    private ComputeBuffer _hashGridTrackerBuffer;
  
    //private ComputeBuffer _testBuffer;
    SolidParticlesManager BoundaryManager = new SolidParticlesManager();

    private static readonly int SizeProperty = Shader.PropertyToID("_size");
    private static readonly int ParticlesBufferProperty = Shader.PropertyToID("_particlesBuffer");
   
    public ComputeShader computeShaderSPH;

    private GridHash hash;
    private int clearHashGridKernel;
    private int recalculateHashGridKernel;
    private int buildNeighbourListKernel;
    private int computeDensityPressureKernel;
    private int computeForcesKernel;
    private int integrateKernel;
    private int DispatchKernel;

    [Tooltip("The absolute accumulated simulation steps")]
    public int elapsedSimulationSteps;
    private object properties;

    [StructLayout(LayoutKind.Sequential, Size = 28)]

    private struct Particle
    {
        public Vector3 position;
        public Vector4 colorGradient;
        public Vector3 velocity;
        public Vector3 forces;
        public float density;
        public float mass;
        public float pressure;

        public static int stride = sizeof(float) * 16;
    }



    private void Awake()
    {

        // mass2 = mass * mass;
        //mass2 = 16;
        InitParameters();
        RespawnParticles();
        CreateHashGrid();
        FindKernels();
        InitComputeShaderParameter();
        InitBoundaryParticleBuffer();
        InitComputeBuffers();

    }

    #region Initilisation
    private void RespawnParticles()
    {

        _particles = new Particle[numberOfParticles];
        _test = new int[numberOfParticles];

        int particlesPerDimension = Mathf.CeilToInt(Mathf.Pow(numberOfParticles, 1f / 3f));
        volume = Mathf.Pow(2 * Supportradius, 3);
        int counter = 0;
        while (counter < numberOfParticles / 2)
        {
            for (int x = 0; x < particlesPerDimension; x++)
                for (int y = 0; y < particlesPerDimension; y++)
                    for (int z = 0; z < particlesPerDimension; z++)
                    {//ToDo 开放参数初始化
                        Vector3 startPos = new Vector3(dimensions - 1, dimensions - 1, dimensions - 1)
                              - new Vector3(x / 2f, y / 2f, z / 2f) - new Vector3(Random.Range(0, 0.01f), Random.Range(0f, 0.01f), Random.Range(0f, 0.01f));


                        _particles[counter] = new Particle
                        {
                            position = startPos,
                            colorGradient = Color.white,
                            velocity = Vector3.zero,
                            forces = Vector3.zero,
                            density = -10f,
                            mass = 4f,
                            pressure = 0.0f,

                        };

                        if (++counter == numberOfParticles)
                        {
                            return;
                        }
                    }
        
        }

     

    }

    private void CreateHashGrid()
    {
        bounds = new Bounds(new Vector3(dimensions/2,dimensions/2,dimensions/2), new Vector3(dimensions, dimensions, dimensions));
        hash = new GridHash(bounds, numberOfParticles + numberOfBoundaryParticles, Supportradius);
        
    }


  
    private void FindKernels()
    {
        clearHashGridKernel = computeShaderSPH.FindKernel("ClearHashGrid");
        recalculateHashGridKernel = computeShaderSPH.FindKernel("RecalculateHashGrid");
        buildNeighbourListKernel = computeShaderSPH.FindKernel("BuildNeighbourList");
        computeDensityPressureKernel = computeShaderSPH.FindKernel("ComputeDensityPressure");
        computeForcesKernel = computeShaderSPH.FindKernel("ComputeForces");
        integrateKernel = computeShaderSPH.FindKernel("Integrate");


    }



    // Start is called before the first frame update
    private void InitComputeShaderParameter()
    {
        computeShaderSPH.SetFloat("CellSize", Supportradius * 2);
        computeShaderSPH.SetInt("Dimensions", dimensions);
        computeShaderSPH.SetInt("maximumParticlesPerCell", maximumParticlesPerCell);
        computeShaderSPH.SetFloat("radius", Supportradius);
        computeShaderSPH.SetFloat("radius2", radius2);
        computeShaderSPH.SetFloat("radius3", radius3);
        computeShaderSPH.SetFloat("radius4", radius4);
        computeShaderSPH.SetFloat("radius5", radius4);

        computeShaderSPH.SetFloat("gasConstant", gasConstant);
        computeShaderSPH.SetFloat("restDensity", restDensity);
        computeShaderSPH.SetFloat("viscosityCoefficient", viscosityCoefficient);
        computeShaderSPH.SetFloat("damping", damping);
        computeShaderSPH.SetFloat("dt", dt);
        computeShaderSPH.SetFloats("gravity", gravity);
        computeShaderSPH.SetFloats("epsilon", Mathf.Epsilon);
        computeShaderSPH.SetFloat("pi", Mathf.PI);
        computeShaderSPH.SetInt("doubleInteractive", doubleInteractive);
        computeShaderSPH.SetFloat("solidpressure", solidPressure);
        computeShaderSPH.SetInt("numberofBoundaryParticles",numberOfBoundaryParticles);

    }
    void InitBoundaryParticleBuffer() {

        //碰撞Boundary粒子设置（SetData）
        
        BoundaryManager.NumParticles = numberOfBoundaryParticles;
       
        BoundaryManager.radius = BoundaryParticleradius;
       
        BoundaryManager.InitBoundaryParticles(BoundaryManager.CreateCylinder(), Matrix4x4.identity);
     
        BoundaryManager.ConfigBoundary(BoundaryShader);
        //Debug.Log(BoundaryManager.SolidParticlesList[100].position);
        
        computeShaderSPH.SetBuffer(clearHashGridKernel, "_boundaryParticles", BoundaryManager.SolidParticleBuffer);
        
        computeShaderSPH.SetBuffer(recalculateHashGridKernel, "_boundaryParticles", BoundaryManager.SolidParticleBuffer);
        
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_boundaryParticles", BoundaryManager.SolidParticleBuffer);
        
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_boundaryParticles", BoundaryManager.SolidParticleBuffer);
      


    }

    void InitComputeBuffers()
    {
        uint[] args = {
        particleMesh.GetIndexCount(0),
        (uint)numberOfParticles,
        particleMesh.GetIndexStart(0),
        particleMesh.GetBaseVertex(0),
        0
        };
   
        _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(args);

        int totalParticles = numberOfBoundaryParticles + numberOfParticles;
        _particlesBuffer = new ComputeBuffer(numberOfParticles, Particle.stride);
        _particlesBuffer.SetData(_particles);


        _neighbourList = new int[numberOfParticles * maximumParticlesPerCell * 8];
        _neighbourTracker = new int[numberOfParticles];


        _hashGrid = new uint[dimensions * dimensions * dimensions * maximumParticlesPerCell];
        _hashGridTracker = new uint[dimensions * dimensions * dimensions];

        _neighbourListBuffer = new ComputeBuffer(numberOfParticles * maximumParticlesPerCell * 8, sizeof(int));
        _neighbourListBuffer.SetData(_neighbourList);
        _neighbourTrackerBuffer = new ComputeBuffer(numberOfParticles, sizeof(int));
        _neighbourTrackerBuffer.SetData(_neighbourTracker);

        _hashGridBuffer = new ComputeBuffer(dimensions * dimensions * dimensions * maximumParticlesPerCell, sizeof(uint));
        _hashGridBuffer.SetData(_hashGrid);
        _hashGridTrackerBuffer = new ComputeBuffer(dimensions * dimensions * dimensions, sizeof(uint));


     
        computeShaderSPH.SetBuffer(clearHashGridKernel, "_hashGridTracker", _hashGridTrackerBuffer);
        

        computeShaderSPH.SetBuffer(recalculateHashGridKernel, "_particles", _particlesBuffer);
        computeShaderSPH.SetBuffer(recalculateHashGridKernel, "_hashGrid", _hashGridBuffer);
        computeShaderSPH.SetBuffer(recalculateHashGridKernel, "_hashGridTracker", _hashGridTrackerBuffer);
        //
      
       

        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_particles", _particlesBuffer);
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_hashGrid", _hashGridBuffer);
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_hashGridTracker", _hashGridTrackerBuffer);
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_neighbourList", _neighbourListBuffer);
        computeShaderSPH.SetBuffer(buildNeighbourListKernel, "_neighbourTracker", _neighbourTrackerBuffer);
        
     

        computeShaderSPH.SetBuffer(computeDensityPressureKernel, "_neighbourTracker", _neighbourTrackerBuffer);
        computeShaderSPH.SetBuffer(computeDensityPressureKernel, "_neighbourList", _neighbourListBuffer);
        computeShaderSPH.SetBuffer(computeDensityPressureKernel, "_particles", _particlesBuffer);


        computeShaderSPH.SetBuffer(computeForcesKernel, "_neighbourTracker", _neighbourTrackerBuffer);
        computeShaderSPH.SetBuffer(computeForcesKernel, "_neighbourList", _neighbourListBuffer);
        computeShaderSPH.SetBuffer(computeForcesKernel, "_particles", _particlesBuffer);

        computeShaderSPH.SetBuffer(integrateKernel, "_particles", _particlesBuffer);


    }

    #endregion
    // Update is called once per frame
    void Update()
    {


        computeShaderSPH.Dispatch(clearHashGridKernel, dimensions * dimensions * dimensions / 100, 1, 1);
        computeShaderSPH.Dispatch(recalculateHashGridKernel, numberOfParticles / 100, 1, 1);
        computeShaderSPH.Dispatch(buildNeighbourListKernel, numberOfParticles / 100, 1, 1);
        computeShaderSPH.Dispatch(computeDensityPressureKernel, numberOfParticles / 100, 1, 1);
        computeShaderSPH.Dispatch(computeForcesKernel, numberOfParticles / 100, 1, 1);
        computeShaderSPH.Dispatch(integrateKernel, numberOfParticles / 100, 1, 1);

        //Debug.Log(_argsBuffer);
        //if (material != null && particleMesh != null)
        //{
        //    Particle[] data = new Particle[numberOfParticles];
        //    _particlesBuffer.GetData(data);


        //    for (int i = 0; i < Mathf.CeilToInt(numberOfParticles / 10); i++)
        //    {
             

        //        Debug.Log("density = " + data[i].density);
        //        // Debug.Log("mass = " + data[i].mass);
        //        Debug.Log("pressure = " + data[i].pressure);

        //        Debug.Log("forces = " + data[i].forces);
        //        //  Debug.Log("velocity = " + data[i].velocity);
        //        //Debug.Log("position = " + data[i].position);

        //    }

        //}





            material.SetFloat(SizeProperty, particleRenderSize);
            material.SetBuffer(ParticlesBufferProperty, _particlesBuffer);
            //material.SetBuffer(VelocityField, _velocitiesBuffer);
            BoundaryManager.Draw(Camera.main, particleMesh, BoundaryMaterial, 0);
            Graphics.DrawMeshInstancedIndirect(particleMesh, 0, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), _argsBuffer);
        elapsedSimulationSteps++;


        //}
    }



    private void InitParameters()
    {
        if (particleMesh == null||material == null){
            throw new ArgumentException("particleMesh and material should be initialized");
           
        }

        else if(numberOfBoundaryParticles ==0 ||numberOfParticles ==0)
        {
            throw new ArgumentException("Your particles buffer is zero!");
           
        }

        else
        {
            Debug.Log("No Exception");

        }
        radius2 = Supportradius * Supportradius;
        radius3 = Supportradius * radius2;
        radius4 = radius2 * radius2;
        radius5 = radius4 * Supportradius;

    }

    private void OnDestroy()
    {
       ReleaseBuffers();
    }

    private void ReleaseBuffers()
    {
        _particlesBuffer.Dispose();
        _argsBuffer.Dispose();
        _neighbourListBuffer.Dispose();
        _neighbourTrackerBuffer.Dispose();
        _hashGridBuffer.Dispose();
        _hashGridTrackerBuffer.Dispose();
        hash.Dispose();
        BoundaryManager.Dispose();
    }


    private void OnRenderObject()
    {
        hash.DrawGrid(Camera.current, Color.green);
        DrawLines.Draw(Camera.current, new Vector4(0, 0, 0, 0), new Vector4(0, 100, 0, 0), Color.yellow, Matrix4x4.identity);
    }
}