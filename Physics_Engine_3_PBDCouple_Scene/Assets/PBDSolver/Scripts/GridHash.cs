using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class GridHash : IDisposable
{

    private const int THREDAS = 128;
    private const int READ = 0;
    private const int WRITE = 1;

    public int TotalParticles { get; private set; }
    public Bounds Bounds;
    public float CellSize { get; private set; }
    public float InvCellSize { get; private set; }
    public int Groups { get; private set; }

    public ComputeBuffer IndexMap { get; private set; }
    public ComputeBuffer Table { get; private set; }

   // public ComputeBuffer DebugBuffer { get; private set; }
    public BitonicSort m_sort;
    public PrefixRadixSort m_Radixsort;

    private ComputeShader m_shader;
    private int m_hashKernel, m_clearKernel, m_mapKernel;

    bool BitonicSortisEnabled = true;

    public GridHash(Bounds bounds, int numParticles, float cellSize)
    {
        TotalParticles = numParticles;
        CellSize = cellSize;
        InvCellSize = 1 / cellSize;
        Groups = TotalParticles / THREDAS;
        if (TotalParticles % THREDAS != 0) Groups++;

        Vector3 min, max;
        min = bounds.min;
        max.x = min.x + (float)Math.Ceiling(bounds.size.x / CellSize);
        max.y = min.y + (float)Math.Ceiling(bounds.size.y / CellSize);
        max.z = min.z + (float)Math.Ceiling(bounds.size.z / CellSize);

        Bounds = new Bounds();
        Bounds.SetMinMax(min, max);


        int width = (int)Bounds.size.x;
        int height = (int)Bounds.size.y;
        int depth = (int)Bounds.size.z;

        int size = width * height * depth;

        IndexMap = new ComputeBuffer(TotalParticles, 2 * sizeof(int));
       
        Table = new ComputeBuffer(size, 2 * sizeof(int));
      //  DebugBuffer = new ComputeBuffer(size, 4 * sizeof(float));
       
        if(BitonicSortisEnabled == true)
        {
            m_sort = new BitonicSort(TotalParticles);

        }
        else
        {
            m_Radixsort = new PrefixRadixSort(TotalParticles);
            Debug.Log("TotalParticle ==" + TotalParticles);
        }


        m_shader = Resources.Load("GridHash") as ComputeShader;
        m_hashKernel = m_shader.FindKernel("HashParticles");
        m_clearKernel = m_shader.FindKernel("ClearTable");
        m_mapKernel = m_shader.FindKernel("MapTable");

    }
    public void Dispose()
    {
        if (BitonicSortisEnabled == true)
        {
            m_sort.Dispose();
        }
        else
        {
            m_Radixsort.Dispose();
        }
      
        //m_sort.Dispose();
        if (IndexMap != null)
        {
            IndexMap.Release();
            IndexMap = null;
        }
        if (Table != null)
        {
            Table.Release();
            Table = null;
        }
      // DebugBuffer.Release();
    }

    public void Process(ComputeBuffer particles)
    {
        if (particles.count != TotalParticles)
            throw new ArgumentException("particles.Length != TotalParticles");
        m_shader.SetInt("NumParticles", TotalParticles);
        m_shader.SetInt("TotalParticles", TotalParticles);
        m_shader.SetFloat("HashScale", InvCellSize);
        m_shader.SetVector("HashSize", Bounds.size);
        m_shader.SetBuffer(m_hashKernel, "Particles", particles);
        m_shader.SetBuffer(m_hashKernel, "Boundary", particles);
        m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);
        m_shader.Dispatch(m_hashKernel, Groups, 1, 1);
        if (BitonicSortisEnabled == true)
        {
            MapTable();
        }
        else
        {
       
            RadixMapTable();
        };
           
       
    }

    public void Process(ComputeBuffer particles, ComputeBuffer boundary)
    {
        int numParticles = particles.count;
        int numBoundary = boundary.count;
        if (numParticles + numBoundary != TotalParticles)
            throw new ArgumentException("numParticles + Boundary ！=TotalParticles");


        m_shader.SetInt("NumParticles", numParticles);
        m_shader.SetInt("TotalPatrticles", TotalParticles);
        m_shader.SetFloat("HashScale", InvCellSize);
        m_shader.SetVector("HashSize", Bounds.size);
        m_shader.SetVector("HashTranslate", Bounds.min);

        m_shader.SetBuffer(m_hashKernel, "Particles", particles);
        m_shader.SetBuffer(m_hashKernel, "Boundary", boundary);
        m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);


       // m_shader.SetBuffer(m_hashKernel, "Debug", DebugBuffer);

        m_shader.Dispatch(m_hashKernel, Groups, 1, 1);

        if (BitonicSortisEnabled == true)
        {
            MapTable();
        }
        else
        {
            RadixMapTable();
        };
    }


    private void RadixMapTable()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start(); //  开始监视代码运行时间
        m_Radixsort.Init(IndexMap);
        m_Radixsort.Sort();
        stopwatch.Stop(); //  停止监视

        //  获取当前实例测量得出的总时间
        System.TimeSpan timespan = stopwatch.Elapsed;
        double hours = timespan.TotalHours; // 总小时
        double minutes = timespan.TotalMinutes;  // 总分钟
        double seconds = timespan.TotalSeconds;  //  总秒数
        double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

        //打印代码执行时间
        Debug.Log("Testime ===================>>>>>>>>>>>>>>>>>>>>>>>>" + milliseconds);

        m_shader.SetInt("TotalParticles", TotalParticles);
        m_shader.SetBuffer(m_clearKernel, "Table", Table);

        m_shader.Dispatch(m_clearKernel, Groups, 1, 1);


        m_shader.SetBuffer(m_mapKernel, "IndexMap", IndexMap);
        m_shader.SetBuffer(m_mapKernel, "Table", Table);

        // m_shader.SetBuffer(m_mapKernel, "Debug", DebugBuffer);
        m_shader.Dispatch(m_mapKernel, Groups, 1, 1);
    }

    private void MapTable()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

         stopwatch.Start(); //  开始监视代码运行时间  
                           //
        m_sort.Sort(IndexMap);
         stopwatch.Stop(); //  停止监视

        //  获取当前实例测量得出的总时间
        System.TimeSpan timespan = stopwatch.Elapsed;
           double hours = timespan.TotalHours; // 总小时
            double minutes = timespan.TotalMinutes;  // 总分钟
            double seconds = timespan.TotalSeconds;  //  总秒数
         double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

        //打印代码执行时间
         //  Debug.Log("Testime ===================>>>>>>>>>>>>>>>>>>>>>>>>" + milliseconds);
        //Debug：SortMap index为0
   

        m_shader.SetInt("TotalParticles", TotalParticles);
        m_shader.SetBuffer(m_clearKernel, "Table", Table);

        m_shader.Dispatch(m_clearKernel, Groups, 1, 1);


        m_shader.SetBuffer(m_mapKernel, "IndexMap", IndexMap);
        m_shader.SetBuffer(m_mapKernel, "Table", Table);

      //  m_shader.SetBuffer(m_mapKernel, "Debug", DebugBuffer);

        m_shader.Dispatch(m_mapKernel, Groups, 1, 1);     

    }
    public void DrawGrid(Camera cam, Color col)
    {
        float width = Bounds.size.x;
        float height = Bounds.size.y;
        float depth = Bounds.size.z;


      


        DrawLines.LineMode = LINE_MODE.LINES;

        for (float y = 0; y <= height; y++)
        {

            for (float x = 0; x < width; x++)
            {

                Vector3 a = Bounds.min + new Vector3(x, y, 0) * CellSize;
                Vector3 b = Bounds.min + new Vector3(x, y, depth) * CellSize;

                DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
              
            }

            for (float z = 0; z <= depth; z++)
            {
                Vector3 a = Bounds.min + new Vector3(0, y, z) * CellSize;
                Vector3 b = Bounds.min + new Vector3(width, y, z) * CellSize;
                DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);

            }

        }

        for (float z = 0; z <= depth; z++)
        {
            for (float x = 0; x <= width; x++)
            {
                Vector3 a = Bounds.min + new Vector3(x, 0, z) * CellSize;
                Vector3 b = Bounds.min + new Vector3(x, height, z) * CellSize;

                DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
            }
        }


    }



}