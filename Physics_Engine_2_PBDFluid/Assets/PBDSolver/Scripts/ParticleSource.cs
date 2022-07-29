using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSource 
{
    // Start is called before the first frame update

    public int NumParticles { get { return Positions.Count; } }
    public Bounds Bounds { get; private set; }
    public List<Bounds> Exclusion { get; private set; }
    //public int numberOfParticles;
    //public int dimensions;
    public float Spacing { get; private set; }

    public float HalfSpacing { get { return Spacing * 0.5f; } }
    public IList<Vector3> Positions { get; protected set; }
    //public ParticleSource(Bounds Bounds,float spacing,int numberOfParticles,int dimemsions)
    //{

    //    InitParticleSourceStyle1(Bounds, spacing);
    //    InitPositions3(Bounds,numberOfParticles, dimemsions);

    //}

    public ParticleSource(float spacing, Bounds bounds) 
    {
        Bounds = bounds;
        Exclusion = new List<Bounds>();
        CreateParticles(spacing);
    }
    public ParticleSource(Bounds bounds, float spacing)
    {
        // InitParticleSourceStyle1(Bounds, spacing);
        Bounds = bounds;
        Exclusion = new List<Bounds>();
        CreateParticles(spacing);

    }

    public ParticleSource(float spacing,Bounds bounds,Bounds exclusion)
    {
        Bounds = bounds;
        Exclusion = new List<Bounds>();
        Exclusion.Add(exclusion);
        CreateParticles(spacing);


    }
    private void CreateParticles(float spacing)
    {   
        float HalfSpacing = spacing * 0.5f;
        int numX = (int)((Bounds.size.x + HalfSpacing) / spacing);
        int numY = (int)((Bounds.size.y + HalfSpacing) / spacing);
        int numZ = (int)((Bounds.size.z + HalfSpacing) / spacing);

        Positions = new List<Vector3>();

        for(int z = 0; z < numZ; z++)
        {
            for (int y = 0; y < numY; y++)
            {

                for(int x = 0; x < numX; x++)
                {
                    Vector3 pos = new Vector3();
                    pos.x = spacing * x + Bounds.min.x + HalfSpacing;
                    pos.y = spacing * y + Bounds.min.y + HalfSpacing;
                    pos.z = spacing * z + Bounds.min.z + HalfSpacing;

                    bool exclude = false;
                    for(int i = 0;i<Exclusion.Count; i++)
                    {
                        if (Exclusion[i].Contains(pos))
                        {
                            exclude = true;
                            break;
                        }
                    }
                    if (!exclude)
                        Positions.Add(pos);

                }

            }

        }





    }

    public IList<Vector3> InitParticleSourceStyle1(Bounds Bounds, float spacing)
    {
        float HalfSpacing = Spacing * 0.5f;
        int numX = (int)((Bounds.size.x + HalfSpacing) / spacing);
        int numY = (int)((Bounds.size.y + HalfSpacing) / spacing);
        int numZ = (int)((Bounds.size.z + HalfSpacing) / spacing);

        Positions = new List<Vector3>();
        for (int z = 0; z < numZ; z++)
        {
            for (int y = 0; y < numY; y++)
            {
                for (int x = 0; x < numX; x++)
                {
                    Vector3 pos = new Vector3();
                    pos.x = spacing * x + Bounds.min.x + HalfSpacing;
                    pos.y = spacing * y + Bounds.min.y + HalfSpacing;
                    pos.z = spacing * z + Bounds.min.z + HalfSpacing;

                    Positions.Add(pos);

                }


            }


        }
        return Positions;


    }
    public IList<Vector3> InitParticleSourceStyle2(Bounds Bounds, float spacing,int resolution)
    {
        //int numX = (int)((Bounds.size.x + spacing * 0.5) / spacing);
        //int numY = (int)((Bounds.size.y + spacing * 0.5) / spacing);
        //int numZ = (int)((Bounds.size.z + spacing * 0.5) / spacing);



        int numX = resolution;
        int numY = resolution;
        int numZ = resolution;



           Positions = new List<Vector3>();
        for (int z = 0; z < numZ; z++)
        {
            for (int y = 0; y < numY; y++)
            {
                for (int x = 0; x < numX; x++)
                {
                    Vector3 pos = new Vector3();
                    pos.x = spacing * x + Bounds.min.x + spacing * 0.5f;
                    pos.y = spacing * y + Bounds.min.y + spacing * 0.5f;
                    pos.z = spacing * z + Bounds.min.z + spacing * 0.5f;

                    Positions.Add(pos);

                }


            }


        }
        return Positions;


    }

    public void InitPositions3(Bounds bounds,int numberOfParticles,int dimensions)
    {



        Positions = new List<Vector3>(numberOfParticles);
       // Vector3[] positions = new Vector3[numberOfParticles];
           // _test = new int[numberOfParticles];

            int particlesPerDimension = Mathf.CeilToInt(Mathf.Pow(numberOfParticles, 1f / 3f));
            //volume = Mathf.Pow(2 * Supportradius, 3);
            int counter = 0;
            while (counter < numberOfParticles/2)
            {
                 for (int x = 0; x < particlesPerDimension; x++)
                    for (int y = 0; y < particlesPerDimension; y++)
                        for (int z = 0; z < particlesPerDimension; z++)
                              {//ToDo 开放参数初始化
                            Vector3 startPos = new Vector3(dimensions - 1, dimensions - 1, dimensions - 1)
                                  - new Vector3(x / 2f, y / 2f, z / 2f) - new Vector3(Random.Range(0, 0.01f), Random.Range(0f, 0.01f), Random.Range(0f, 0.01f));

                        Positions.Add(startPos);
                        
                        if (++counter == numberOfParticles)
                        {
                            return;
                        }
                    }


        }

       
    }
    public Vector3[] CreateSphere()
    {
        int resolution = Mathf.CeilToInt(Mathf.Sqrt(NumParticles));
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        //平移
        Vector3 Translate = new Vector3(25, 0, 25);
        // Vector3 Translate = new Vector3(0, 0, 0);
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


            positions[i].x = 10 * s * Mathf.Sin(Mathf.PI * u);
            positions[i].y = 10 * r * Mathf.Sin(Mathf.PI * 0.5f * v);
            positions[i].z = 10 * s * Mathf.Cos(Mathf.PI * u);
            positions[i] += Translate;
        }

        return positions;
    }

 

}

