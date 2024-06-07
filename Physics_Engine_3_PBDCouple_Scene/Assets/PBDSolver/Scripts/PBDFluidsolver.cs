using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

// Start is called before the first frame update
public class PBDFluidSolver : IDisposable
    {
        private const int THREADS = 128;
        private const int READ = 0;
        private const int WRITE = 1;
        
        public int Groups { get; private set; }

        public BoundaryParticles BoundaryParticles { get; private set; }

        public FluidParticles FluidParticles { get; private set; }
        public GridHash Hash { get; private set; }
        
        public int SolverIterations;

        public int  ConstraintInterations;
            
        private ComputeShader m_shader;

        public SmoothingKernel smoothingKernel { get; private set; }
        public Vector3 Gravity;
         public float KernelRadius { get; private set; }


    public PBDFluidSolver(FluidParticles fluidBody, BoundaryParticles solidBody,Bounds bounds)
    {
        SolverIterations = 2;
        ConstraintInterations = 2;

        FluidParticles = fluidBody;
        BoundaryParticles = solidBody;
        //CellSize correlatewith fluidRadius
        float cellSize = fluidBody.Radius * 4.0f;
        int total = fluidBody.NumParticles + solidBody.NumParticles;
        Hash = new GridHash(bounds, total, cellSize);
        int numParticles = fluidBody.NumParticles;
       
        smoothingKernel = new SmoothingKernel(cellSize);


        if(BoundaryParticles.isCouplewithFluid == true)
        {
            Groups = total / THREADS;
            if (total % THREADS != 0) Groups++;
        }
        else
        {
           Groups = numParticles / THREADS;
        if (numParticles % THREADS != 0) Groups++;
           
        }
        m_shader = Resources.Load("PBDsolver") as ComputeShader;


    }


  

    public void StepPhysics(float dt)
    {
        if (dt <= 0.0) return;
        if (SolverIterations <= 0 || ConstraintInterations <= 0) return;

        dt /= SolverIterations;
        m_shader.SetFloat("deltaTime", dt);
        m_shader.SetInt("NumParticles", FluidParticles.NumParticles);
        m_shader.SetVector("gravity", Gravity);
        m_shader.SetFloat("damping", FluidParticles.Damping);
        m_shader.SetFloat("density0", FluidParticles.Density0);
        m_shader.SetFloat("viscosity", FluidParticles.Viscosity);
        m_shader.SetFloat("fluidmass", FluidParticles.ParticleMass);
        m_shader.SetFloat("KernelRadius", smoothingKernel.Radius);
        m_shader.SetFloat("KernelRadius2", smoothingKernel.Radius2);
        m_shader.SetFloat("POLY6", smoothingKernel.POLY6);
        m_shader.SetFloat("SPIKY_GRAD", smoothingKernel.SPIKY_GRAD);
        m_shader.SetFloat("VISC_LAP", smoothingKernel.VISC_LAP);
        m_shader.SetFloat("solidPressure", BoundaryParticles.solidPressure);
        m_shader.SetFloat("solidmass", BoundaryParticles.solidmass);
        m_shader.SetFloat("invSolidDensity",1/BoundaryParticles.invDensity);

        m_shader.SetFloat("HashScale", Hash.InvCellSize);
        m_shader.SetVector("HashSize", Hash.Bounds.size);
        m_shader.SetVector("HashTranslate", Hash.Bounds.min);
        m_shader.SetFloat("invDensity0", FluidParticles.InvDensity0);

        //Debug.Log("FluidParticleNumber = " + FluidParticles.NumParticles);

        for (int i = 0; i < SolverIterations; i++)
        {
           PredictPositions(dt);
           Hash.Process(FluidParticles.predicted, BoundaryParticles.positions);
           SolveConstraint();
           UpdateVelocities(dt);
           SolveViscosity();
           UpdatePositions();


        }


      
    }


   private void PredictPositions(float dt)
    {
        int kernel = m_shader.FindKernel("PredictPositions");
        m_shader.SetBuffer(kernel, "Positions", FluidParticles.positions);
        m_shader.SetBuffer(kernel, "Predicted", FluidParticles.predicted);
        m_shader.SetBuffer(kernel, "Velocities", FluidParticles.velocities);
        
        m_shader.Dispatch(kernel, Groups, 1, 1);




    }
    private void SolveConstraint()
    {
        int computeKernel = m_shader.FindKernel("ComputeDensity");
        int solveKernel = m_shader.FindKernel("SolveConstraint");

        m_shader.SetBuffer(computeKernel, "Densities", FluidParticles.densities);
        //m_shader.SetBuffer(computeKernel, "Pressures", FluidParticles.pressures);
        m_shader.SetBuffer(computeKernel, "Boundary", BoundaryParticles.positions);
        m_shader.SetBuffer(computeKernel, "IndexMap", Hash.IndexMap);
        m_shader.SetBuffer(computeKernel, "Table", Hash.Table);
        m_shader.SetBuffer(computeKernel, "Lambda", FluidParticles.lambda);
        
        //m_shader.SetBuffer(solveKernel, "Pressures", FluidParticles.pressures);
        m_shader.SetBuffer(solveKernel, "Boundary", BoundaryParticles.positions);
        m_shader.SetBuffer(solveKernel, "IndexMap", Hash.IndexMap);
        m_shader.SetBuffer(solveKernel, "Table", Hash.Table);
        m_shader.SetBuffer(solveKernel, "Lambda", FluidParticles.lambda);
        
        for (int i = 0;i<ConstraintInterations;i++)
        {
            m_shader.SetBuffer(computeKernel, "Predicted", FluidParticles.predicted);
             m_shader.Dispatch(computeKernel, Groups, 1, 1);
            m_shader.SetBuffer(solveKernel,"Predicted",FluidParticles.predicted);
            m_shader.Dispatch(solveKernel, Groups, 1, 1);

        }





    }

    private void UpdateVelocities(float dt)
    {
        int kernel = m_shader.FindKernel("UpdateVelocities");
        m_shader.SetBuffer(kernel, "Positions", FluidParticles.positions);
        m_shader.SetBuffer(kernel, "Predicted", FluidParticles.predicted);
        m_shader.SetBuffer(kernel, "Velocities", FluidParticles.velocities);
        //debug
       /// m_shader.SetBuffer(kernel, "IndexMap", Hash.IndexMap);
       // m_shader.SetBuffer(kernel, "Pressures", FluidParticles.pressures);

        //debug
        m_shader.Dispatch(kernel, Groups, 1, 1);


    }

   

    private void SolveViscosity()
    {
        int kernel = m_shader.FindKernel("SolveViscosity");
        m_shader.SetBuffer(kernel, "Densities", FluidParticles.densities);
        m_shader.SetBuffer(kernel, "Boundary", BoundaryParticles.positions);
        m_shader.SetBuffer(kernel, "IndexMap", Hash.IndexMap);
        m_shader.SetBuffer(kernel, "Table", Hash.Table);

        m_shader.SetBuffer(kernel, "Predicted", FluidParticles.predicted);
        m_shader.SetBuffer(kernel, "Velocities", FluidParticles.velocities);
        m_shader.Dispatch(kernel, Groups, 1, 1);






    }

    public void Dispose()
    {
        Hash.Dispose();
    }


    Vector4[] m_corners = new Vector4[8];
    private void UpdatePositions()
    {
        int kernel = m_shader.FindKernel("UpdatePositions");
        m_shader.SetBuffer(kernel, "Positions", FluidParticles.positions);
        m_shader.SetBuffer(kernel, "Predicted", FluidParticles.predicted);


        m_shader.Dispatch(kernel, Groups, 1, 1);
    }
    private static IList<int> m_cube = new int[]
       {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
       };

    private void GetCorners(Bounds b)
    {
        m_corners[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
        m_corners[1] = new Vector4(b.min.x, b.min.y, b.max.z, 1);
        m_corners[2] = new Vector4(b.max.x, b.min.y, b.max.z, 1);
        m_corners[3] = new Vector4(b.max.x, b.min.y, b.min.z, 1);

        m_corners[4] = new Vector4(b.min.x, b.max.y, b.min.z, 1);
        m_corners[5] = new Vector4(b.min.x, b.max.y, b.max.z, 1);
        m_corners[6] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
        m_corners[7] = new Vector4(b.max.x, b.max.y, b.min.z, 1);
    }
    public void DrawBounds(Camera cam, Color col, Bounds bounds)
    {
        GetCorners(bounds);
        DrawLines.LineMode = LINE_MODE.LINES;
        DrawLines.Draw(cam, m_corners, col, Matrix4x4.identity, m_cube);
    }
    private void Swap(ComputeBuffer[] buffers)
    {
        ComputeBuffer tmp = buffers[0];
        buffers[0] = buffers[1];
        buffers[1] = tmp;
    }

}