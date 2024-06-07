using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadixSort : IDisposable
{
    



   // public int NumberOfParticles;
    public int NumberfGrid;
    public int NumElements;
  

    public int MaxDitLengthIndex;
    public int MaxDitLengthGrid;

    public int SORT_RADIUS = 256;

    //Data stucture
    private const int THREADS = 256;
    public int Groups { get; private set; }

    public static ComputeBuffer m_bufferIn, m_resultbuffer,m_tempBuffer;
    private ComputeBuffer m_globalcounting;
    private ComputeBuffer m_BlockScanSum;
    private ComputeBuffer m_HistogramGlobal;
    public static ComputeBuffer DebugBuffer;
    int m_sortKernel,m_fillKernel,m_mergeKernel;

    int Groupsbin;
    private ComputeShader m_shader;
    public RadixSort(int count, int GridNumber)
    {

        Groups = count / THREADS;
        if (count % THREADS != 0) Groups++;
        NumElements = count;
        NumberfGrid = GridNumber;
        m_bufferIn = new ComputeBuffer(NumElements, 2 * sizeof(int));
        m_resultbuffer = new ComputeBuffer(NumElements, 2 * sizeof(int));
        m_globalcounting = new ComputeBuffer(THREADS, sizeof(int));
        m_BlockScanSum = new ComputeBuffer(Groups, sizeof(int));
        m_tempBuffer = new ComputeBuffer(NumElements, 2*sizeof(int));



        m_shader = Resources.Load("ParalledRadixSort") as ComputeShader;

        //kernel
        m_fillKernel = m_shader.FindKernel("Fill");
        m_sortKernel = m_shader.FindKernel("Sort");
        m_mergeKernel = m_shader.FindKernel("Merge");
      
       



       
        //Temp
       // SORT_RADIUS = 10;
        DebugBuffer = new ComputeBuffer(NumElements, 2 * sizeof(float));
        MaxDitLengthIndex = FindMaxDigitNumber(NumElements, MaxDitLengthIndex, SORT_RADIUS);
        MaxDitLengthGrid = FindMaxDigitNumber(GridNumber, MaxDitLengthGrid, SORT_RADIUS);
        Debug.Log("MaxDitLengthIndex===" + MaxDitLengthIndex);
        Debug.Log("NumElements ==" + NumElements);
        Debug.Log("count===" + count);

    }

    public void Dispose()
    {
        m_BlockScanSum.Release();
        m_bufferIn.Release();
        m_resultbuffer.Release();
        m_globalcounting.Release();
        m_tempBuffer.Release();
    }

    //合并格子和NumParticles位数：indexMap.x*indexMap ,indexMap.y
    public void InitData(ComputeBuffer input)
    {
       // NumberOfParticles = input.count;
        if (NumElements == 0)
            throw new ArgumentException("NumElements = 0!");



        Debug.Log("NumElements ==" + input.count);


        //Vector4 index = new Vector4(GridNumber, NumberOfParticles);

        //FindOptimalRadius(NumberOfParticles);
        //init the number

        // m_shader.SetVector("NumElement", index);



        m_shader.SetInt("MaxDitLengthGrid", MaxDitLengthGrid);
        m_shader.SetInt("MaxDitLengthIndex", MaxDitLengthIndex);
        m_shader.SetInt("Groups", Groups);
        m_shader.SetInt("NumElement", NumElements);
       // m_shader.SetBuffer(m_fillKernel, "Debug", DebugBuffer);
        m_shader.SetBuffer(m_fillKernel, "Data", m_resultbuffer);
        m_shader.SetBuffer(m_fillKernel, "Input", input);
      
      //  m_shader.Dispatch(m_fillKernel, NumElements/THREADS, 1, 1);


        //LocalSort
        //m_shader.SetInt("sortRadius", SORT_RADIUS);
        m_shader.SetBuffer(m_sortKernel,"Input", input);
        m_shader.SetBuffer(m_sortKernel, "Data", m_resultbuffer);
        m_shader.SetBuffer(m_sortKernel, "Debug", DebugBuffer);
   
      
       m_shader.Dispatch(m_sortKernel, Groups, 1, 1);





        m_shader.SetBuffer(m_mergeKernel, "Data", m_resultbuffer);
        m_shader.SetBuffer(m_mergeKernel, "Debug", DebugBuffer);
        m_shader.SetBuffer(m_mergeKernel, "Temp", m_tempBuffer);

       m_shader.SetBuffer(m_mergeKernel, "Input", input);

        m_shader.Dispatch(m_mergeKernel, Groups, 1, 1);





    }

    public void FindOptimalRadius(int MaxNumber)
    {
        int b = 0;
        //首先，找到b的位数，最大被排序的数值的位数99999 为5位
        FindMaxDigitNumber(NumElements, b,10);

        Debug.Log("b 的位数为==" + b);
        Debug.Log("maxNumber ==" + MaxNumber);

        //find The Most effient RADIUS;
        int r = 0;    
        int k = 0;
        float lgN = (Mathf.Log10(MaxNumber));
        if (b < lgN)
        {
            r = b;
        }

        else
        {
            r = Mathf.CeilToInt(lgN);
        }

        k = (int)Mathf.Pow(10,  r);
        SORT_RADIUS = k;
        Debug.Log("SORT_RADIUS ==" + SORT_RADIUS);
        //d轮 =（b/r）计算MaxDitLengthGrid和Max
        FindMaxDigitNumber(MaxNumber, MaxDitLengthIndex, SORT_RADIUS);



    }



    //findTheMax位数给定max和位制、10，20等等
    public int FindMaxDigitNumber(int max,int digitCount,int radius)
    {
        if (max == 0) return 0;
        

        while (max != 0)
        {
            digitCount++;
            max /= radius;
        }
        return digitCount;

    }




    public void Sort(ComputeBuffer m_bufferIn)
    {

    }

}
