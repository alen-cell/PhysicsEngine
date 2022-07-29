
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;
public enum SIMULATION_QUALITY { LOW, MEDIUM, HIGH }

public class FluidDemo1 : MonoBehaviour

{
    //public bool debugData = true;
    public enum SortLevel1 {
        low,
        medieum,
        high

    }
    private const float timeStep = 1.0f / 60.0f;


   // [HeaderAttribute("Fluid Attributes")]
   
    //[Range(0, 40000)]
    //public int numberOfFluidParticles = 2000;
  //  [Range(0,0.1f)]
   // public float viscosity = 0.02f;
   // public float damping = 0.02f;
   // public Bounds bounds;

    public Vector3 gravity = new Vector3(0f, -9.8f, 0);


    [HeaderAttribute("Init Variables")]
    public Material m_fluidParticleMat;
    public Material m_boundaryParticleMat;
    public int Dimension = 100;
    //public Transform transform;
    public Material m_volumeMat;
    private Matrix4x4 TRS;

   


    public bool m_drawLines = true;

    public bool m_drawGrid = false;

    public bool m_drawFluidParticles = true;

    public bool m_drawBoundaryParticles = true;

    public bool m_drawFluidVolume = true;
   

    public SIMULATION_QUALITY m_simulationQuality = SIMULATION_QUALITY.MEDIUM;


    public bool m_run = true;
    public Mesh m_sphereMesh;

    private FluidParticles m_fluid;
    private BoundaryParticles m_boundary;

    private PBDFluidSolver m_solver;

    private RenderVolume m_volume;
    Bounds m_fluidBound, m_outerGridBound, m_rigidBound,m_innerSource,m_outerSource;

    private bool wasError;
    //  private RenderVolume

    void Start()
    {



        float radius = 0;
     
        float fluidDensity = 1000f;
        float boundDensity = 1000f;
        switch (m_simulationQuality)
        {
            case SIMULATION_QUALITY.LOW:
                 radius = 0.1f;
                break;
            case SIMULATION_QUALITY.MEDIUM:
                 radius = 0.08f;
                break;
            case SIMULATION_QUALITY.HIGH:
                 radius = 0.06f;
                break;
        }
     //   radius = 0.2f;
        float fluidRadius = radius;
        float boundaryRadius = radius;
        Debug.Log(radius + "radius ===========");
        try
        {
            m_outerGridBound = new Bounds(new Vector3(Dimension / 2, Dimension / 2, Dimension / 2), new Vector3(Dimension, Dimension, Dimension));
            CreateBoundary(boundaryRadius, boundDensity);
            CreateFluid(fluidRadius,fluidDensity, m_fluidBound, Matrix4x4.identity);
            m_solver = new PBDFluidSolver(m_fluid, m_boundary, m_outerSource);
            m_volume = new RenderVolume(m_boundary.Bounds, radius);
            m_volume.CreateMesh(m_volumeMat);
            m_solver.Gravity = gravity;


        }
        catch
        {

            wasError = true;
            throw;
        }





    }
    private void CreateFluid(float fluidRadius,float density, Bounds Bounds, Matrix4x4 TRS)
    {

        
        //particleSourceINIT
        // Bounds bounds = new Bounds(new Vector3(40, 40, 40), new Vector3(50, 50, 35));
        Bounds bounds = new Bounds();
        Vector3 min = new Vector3(-8, 0, -1);
        Vector3 max = new Vector3(-4, 8, 2);


        min.x += fluidRadius;
        min.y += fluidRadius;
        min.z += fluidRadius;

        max.x -= fluidRadius;
        max.y -= fluidRadius;
        max.z -= fluidRadius;

        bounds.SetMinMax(min, max);
        ParticleSource source = new ParticleSource(bounds, fluidRadius *2* 0.9f);

        //Create a fluid from source
        m_fluid = new FluidParticles(source, fluidRadius, density, TRS);
        Debug.Log("Fluid Particles = " + source.NumParticles);


    }
    private void CreateBoundary(float radius,float density)
    {


        //Demomini
        Bounds innerBounds = new Bounds();
        Vector3 min = new Vector3(-8, 0, -2);
        Vector3 max = new Vector3(8, 10, 2);
        innerBounds.SetMinMax(min, max);

        //Make the boundary 1 particle thick.
        //The multiple by 1.2 adds a little of extra
        //thickness in case the radius does not evenly
        //divide into the bounds size. You might have
        //particles missing from one side of the source
        //bounds other wise.
         
        float thickness = 1;
        float diameter = radius * 2;
        min.x -= diameter * thickness * 1.2f;
        min.y -= diameter * thickness * 1.2f;
        min.z -= diameter * thickness * 1.2f;

        max.x += diameter * thickness * 1.2f;
        max.y += diameter * thickness * 1.2f;
        max.z += diameter * thickness * 1.2f;

        Bounds outerBounds = new Bounds();
        outerBounds.SetMinMax(min, max);



        
        ParticleSource source = new ParticleSource(diameter, outerBounds, innerBounds);
        Debug.Log("Boundary Particles = " + source.NumParticles);

        m_boundary = new BoundaryParticles(source, radius, density, Matrix4x4.identity);

        m_innerSource = innerBounds;
        m_outerSource = outerBounds;

    }
    //CREATE ANOTHER BOUDNS
   
        // Update is called once per frame
        void Update()
    {
        if (wasError) return;
        if (m_run)
        {
            Profiler.BeginSample("== Physics gc ==");
            m_solver.StepPhysics(timeStep);
            m_volume.FillVolume(m_fluid, m_solver.Hash, m_solver.smoothingKernel);
            Profiler.EndSample();

        }

        if (m_drawFluidParticles)
            m_fluid.Draw(Camera.current, m_sphereMesh, m_fluidParticleMat, 0);
        if (m_drawBoundaryParticles)
            m_boundary.Draw(Camera.main, m_sphereMesh, m_boundaryParticleMat, 0);


     

        //int total = m_fluid.NumParticles;
     //   int total = m_fluid.NumParticles;
      //  float[] dataPd = new float[total];
      //  m_fluid.pressures.GetData(dataPd);
       // for (int i = 0; i < Mathf.CeilToInt(total); i++)
       // {

        //    Debug.Log("SolidPressure = " + dataPd[i]);
          //   Debug.Log("gridIndex =" + dataPd[i].x + ", ParticelId = " + dataPd[i].y);
        //    // Debug.Log("gridIndex =" + dataP[i].z+ "ParticelId = " + dataP[i].w);

       // }



    }


    private void OnDestroy()
    {
        m_boundary.Dispose();
        m_fluid.Dispose();
        m_solver.Dispose();

    }


    private void OnRenderObject()
    {
        Camera camera = Camera.main;
        if (camera != Camera.main) return;

        if (m_drawLines)
        {
            m_solver.DrawBounds(camera, Color.green, m_boundary.Bounds);
            m_solver.DrawBounds(camera, Color.blue, m_fluid.Bounds);


        }

        if (m_drawGrid)
        {
            m_solver.Hash.DrawGrid(camera, Color.yellow);
        }


    }




}
