using System;
using System.Collections.Generic;
using UnityEngine;

public class BitonicSort : IDisposable
{
    // Start is called before the first frame update
    private const int THREADS = 128;
    private const int BITONIC_BLOCK_SIZE = 512;
    private const int TRANSPOSE_BLOCK_SIZE = 16;

    public const int MAX_ELEMENTS = BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE;
    public const int MIN_ELEMENTS = BITONIC_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE;

    private const int MATRIX_WIDTH = BITONIC_BLOCK_SIZE;

    public int NumElements { get; private set; }
    private ComputeBuffer m_buffer1, m_buffer2;
    private ComputeShader m_shader;
    int m_bitonicKernel, m_transposeKernel;
    int m_fillKernel, m_copyKernel;

    public BitonicSort(int count)
    {
        NumElements = FindNumElements(count);
        m_buffer1 = new ComputeBuffer(NumElements, 2 * sizeof(int));
        m_buffer2 = new ComputeBuffer(NumElements, 2 * sizeof(int));

        m_shader = Resources.Load("BitonicSort") as ComputeShader;
        m_bitonicKernel = m_shader.FindKernel("BitonicSort");
        m_transposeKernel = m_shader.FindKernel("MatrixTranpose");
        m_fillKernel = m_shader.FindKernel("Fill");
        m_copyKernel = m_shader.FindKernel("Copy");

    }
    // Update is called once per frame
   public void Dispose()
    {
        m_buffer1.Release();
        m_buffer2.Release();
    }


    public void Sort(ComputeBuffer input)
    {
        int count = input.count;
        if (count < MIN_ELEMENTS)
            throw new ArgumentException("count < MIN_ELEMENTS");
        if (count > MAX_ELEMENTS)
            throw new ArgumentException("count > NumElements");



        m_shader.SetInt("Width", count);
        m_shader.SetBuffer(m_fillKernel, "Input", input);
        m_shader.SetBuffer(m_fillKernel, "Data", m_buffer1);
        m_shader.Dispatch(m_fillKernel, NumElements / THREADS, 1, 1);

        int MATRIX_HEIGHT = NumElements / BITONIC_BLOCK_SIZE;

        m_shader.SetInt("Width", MATRIX_HEIGHT);
        m_shader.SetInt("Height",MATRIX_WIDTH);
        m_shader.SetBuffer(m_bitonicKernel,"Data", m_buffer1);



        for(int level = 2;level <=BITONIC_BLOCK_SIZE;level = level * 2)
        {
            m_shader.SetInt("Level", level);
            m_shader.SetInt("LevelMask", level);
            m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);
        }
        for(int level = (BITONIC_BLOCK_SIZE*2);level<=NumElements;level = level * 2)
        {
            // Transpose the data from buffer 1 into buffer 2
            m_shader.SetInt("Level", level / BITONIC_BLOCK_SIZE);
            m_shader.SetInt("LevelMask", (level & ~NumElements) / BITONIC_BLOCK_SIZE);
            m_shader.SetInt("Width", MATRIX_WIDTH);
            m_shader.SetInt("Height", MATRIX_HEIGHT);
            m_shader.SetBuffer(m_transposeKernel, "Input", m_buffer1);
            m_shader.SetBuffer(m_transposeKernel, "Data", m_buffer2);
            m_shader.Dispatch(m_transposeKernel, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, 1);

            // Sort the transposed column data
            m_shader.SetBuffer(m_bitonicKernel, "Data", m_buffer2);
            m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            m_shader.SetInt("Level", BITONIC_BLOCK_SIZE);
            m_shader.SetInt("LevelMask", level);
            m_shader.SetInt("Width", MATRIX_WIDTH);
            m_shader.SetInt("Height",MATRIX_HEIGHT);
            m_shader.SetBuffer(m_transposeKernel, "Input", m_buffer2);
            m_shader.SetBuffer(m_transposeKernel, "Data", m_buffer1);
            m_shader.Dispatch(m_transposeKernel, MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, 1);

            //Sort the row data
            m_shader.SetBuffer(m_bitonicKernel, "Data", m_buffer1);
            m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);



        }

        m_shader.SetInt("Width", count);
        m_shader.SetBuffer(m_copyKernel, "Input", m_buffer1);
        m_shader.SetBuffer(m_copyKernel, "Data", input);
        m_shader.Dispatch(m_copyKernel, NumElements / THREADS, 1, 1);



    }

    private int FindNumElements(int count)
    {
        if (count < MIN_ELEMENTS)
            throw new ArgumentException("Data != MIN_ELEMENTS.Need to decrease Bitonic size.");
        if (count < MAX_ELEMENTS)
            throw new ArgumentException("Data != MAX_ELEMENTS.Need to increase Bitonic size.");
        int NumElements;
        int level = TRANSPOSE_BLOCK_SIZE;
        do
        {
            NumElements = BITONIC_BLOCK_SIZE * level;
            level *= 2;
        }
        while (NumElements < count);
        return NumElements;
    }



}

