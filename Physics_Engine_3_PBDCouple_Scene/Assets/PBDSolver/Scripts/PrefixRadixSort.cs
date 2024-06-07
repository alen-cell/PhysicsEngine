using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//indexMap-------------------------------Particles index Array
//_bufferLocalBucket -----------------------Global  Local Sort memory
//

public class PrefixRadixSort: IDisposable
{
    public ComputeShader _shaderRadixSort;
    private ComputeBuffer _IndexMapbuffer;
    private ComputeBuffer _bufferGlobalBucket;
    private ComputeBuffer _bufferOrdered;
    private ComputeBuffer _bufferSortedParticles;
    private ComputeBuffer _bufferLocalBucket;
    private ComputeBuffer _bufferGroupKeyOffset;
    private ComputeBuffer _bufferLocalBinMarkers;
    public ComputeBuffer DebugBuffer;
    public const int bucketSize = 16;
    public const int bucketBitNum = 4;
    public const int sortSectionNum = 64;

    private int _particleNum;
    private int _groupNum;
    private int _roundNum;

    private int _kernelCheckOrder;
    private int _kernelLocalCount;
    private int _kernelPrecedeScan;
    private int _kernelLocalSort;
    private int _kernelComputeGroupKeyOffset;
    private int _kernelGlobalReorder;

    private int[] _bufferOrderInit;
    private int[] _bufferGlobalBucketInit;
    private int[] _bufferLocalBucketInit;
    private int[] _bufferGroupKeyOffsetInit;
    private int[] _bufferLocalBinMarkersInit;


    public PrefixRadixSort(int particleNum)
    {
        _shaderRadixSort = Resources.Load("GPURadixSort") as ComputeShader;


        _kernelCheckOrder = _shaderRadixSort.FindKernel("CheckOrder");
        _kernelLocalCount = _shaderRadixSort.FindKernel("LocalCount");
        _kernelLocalSort = _shaderRadixSort.FindKernel("LocalSort");
        _kernelPrecedeScan = _shaderRadixSort.FindKernel("PrecedeScan");
        _kernelComputeGroupKeyOffset = _shaderRadixSort.FindKernel("ComputeGroupKeyOffset");
        _kernelGlobalReorder = _shaderRadixSort.FindKernel("GlobalReorder");

        _particleNum = particleNum;
        _groupNum = Mathf.CeilToInt((float)_particleNum / (float)sortSectionNum);
        for (_roundNum = 1; _roundNum < 7; ++_roundNum)
        {
            if ((1 << (4 * _roundNum)) > _particleNum)
                break;
        }

        _bufferOrderInit = new int[1] { 1 };
        _bufferGlobalBucketInit = new int[bucketSize];
        _bufferLocalBucketInit = new int[bucketSize * _groupNum];
        _bufferGroupKeyOffsetInit = new int[bucketSize * _groupNum];
        _bufferLocalBinMarkersInit = new int[bucketSize * sortSectionNum * _groupNum];
        //indexMap HERE
        _bufferSortedParticles = new ComputeBuffer(_particleNum, 2 * sizeof(int));

        _bufferOrdered = new ComputeBuffer(1, sizeof(int));
        _bufferLocalBucket = new ComputeBuffer(bucketSize * _groupNum, sizeof(int));
        _bufferGlobalBucket = new ComputeBuffer(bucketSize, sizeof(int));
        _bufferGroupKeyOffset = new ComputeBuffer(bucketSize * _groupNum, sizeof(int));
        _bufferLocalBinMarkers = new ComputeBuffer(bucketSize * sortSectionNum * _groupNum, sizeof(int));
        DebugBuffer = new ComputeBuffer(_particleNum, 2 * sizeof(int));
    }

    public void Init(ComputeBuffer input)
    {

        _bufferOrdered.SetData(_bufferOrderInit);
        _IndexMapbuffer = input;
        _bufferGlobalBucket.SetData(_bufferGlobalBucketInit);
        _bufferLocalBucket.SetData(_bufferLocalBucketInit);
        _bufferGroupKeyOffset.SetData(_bufferGroupKeyOffsetInit);
        _bufferLocalBinMarkers.SetData(_bufferLocalBinMarkersInit);
    }


    public bool CheckOrder()
    {
        int[] ordered = new int[1] { 1 };
        _bufferOrdered.SetData(ordered);
        _shaderRadixSort.SetBuffer(_kernelCheckOrder, "_Ordered", _bufferOrdered);
        _shaderRadixSort.SetBuffer(_kernelCheckOrder, "_IndexMap", _IndexMapbuffer);
        _shaderRadixSort.Dispatch(_kernelCheckOrder, _groupNum, 1, 1);
        _bufferOrdered.GetData(ordered);
        return ordered[0] == 1;

    }
    public void SortRound(int round)
    {
        _bufferGlobalBucket.SetData(new int[bucketSize]);
        _bufferLocalBucket.SetData(new int[bucketSize * _groupNum]);
        _bufferGroupKeyOffset.SetData(new int[bucketSize * _groupNum]);

        _shaderRadixSort.SetInt("_SortSectionNum", _groupNum);
        _shaderRadixSort.SetInt("_ParticleNum", _particleNum);
        _shaderRadixSort.SetInt("_RotationRound", round);
        LocalCountandScan();
        GlobalOffset();
        GlobalReorder();

    }
    public void LocalCountandScan()
    {
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_Ordered", _bufferOrdered);
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_IndexMap", _IndexMapbuffer);
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_GlobalPrefixSum", _bufferGlobalBucket);
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_LocalPrefixSum", _bufferLocalBucket);
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_GroupKeyOffset", _bufferGroupKeyOffset);
        _shaderRadixSort.SetBuffer(_kernelLocalCount, "_LocalBinMarkers", _bufferLocalBinMarkers);
        _shaderRadixSort.Dispatch(_kernelLocalCount, _groupNum, 1, 1);

        _shaderRadixSort.SetBuffer(_kernelPrecedeScan, "_LocalBinMarkers", _bufferLocalBinMarkers);
        _shaderRadixSort.Dispatch(_kernelPrecedeScan, _groupNum, 1, 1);
    }


    public void GlobalOffset()
    {
        //LocalSort
        _shaderRadixSort.SetBuffer(_kernelLocalSort, "_IndexMap", _IndexMapbuffer);
        _shaderRadixSort.SetBuffer(_kernelLocalSort, "_LocalPrefixSum", _bufferLocalBucket);
        _shaderRadixSort.SetBuffer(_kernelLocalSort, "_LocalBinMarkers", _bufferLocalBinMarkers);
        _shaderRadixSort.SetBuffer(_kernelLocalSort, "Result", _bufferSortedParticles);
        _shaderRadixSort.Dispatch(_kernelLocalSort, _groupNum, 1, 1);
        //ComputeGlocalOffset
        _shaderRadixSort.SetBuffer(_kernelComputeGroupKeyOffset, "_GroupKeyOffset", _bufferGroupKeyOffset);
        _shaderRadixSort.Dispatch(_kernelComputeGroupKeyOffset, 1, 1, 1);


    }
    public void GlobalReorder()
    {
        _shaderRadixSort.SetBuffer(_kernelGlobalReorder, "_IndexMap", _bufferSortedParticles);
        _shaderRadixSort.SetBuffer(_kernelGlobalReorder, "Result", _IndexMapbuffer);
        _shaderRadixSort.SetBuffer(_kernelGlobalReorder, "_GlobalPrefixSum", _bufferGlobalBucket);
        _shaderRadixSort.SetBuffer(_kernelGlobalReorder, "_LocalPrefixSum", _bufferLocalBucket);
        _shaderRadixSort.SetBuffer(_kernelGlobalReorder, "_GroupKeyOffset", _bufferGroupKeyOffset);

        _shaderRadixSort.Dispatch(_kernelGlobalReorder, _groupNum, 1, 1);

    }

    public void Sort()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start(); //  开始监视代码运行时间  
                           //
        for (int i = 0; i < _roundNum; ++i)
        {
            //if (CheckOrder())
            //    break;
            SortRound(i);
        }
        stopwatch.Stop(); //  停止监视
                          //  获取当前实例测量得出的总时间
        System.TimeSpan timespan = stopwatch.Elapsed;
        double hours = timespan.TotalHours; // 总小时
        double minutes = timespan.TotalMinutes;  // 总分钟
        double seconds = timespan.TotalSeconds;  //  总秒数
        double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

        //打印代码执行时间
        Debug.Log("Testime ===================>>>>>>>>>>>>>>>>>>>>>>>>" + milliseconds);
    }
    public void Dispose()
    {
        CBUtility.Release(ref _bufferGlobalBucket);
        CBUtility.Release(ref _bufferLocalBucket);
        CBUtility.Release(ref _bufferGroupKeyOffset);
        CBUtility.Release(ref _bufferOrdered);
        CBUtility.Release(ref _bufferLocalBinMarkers);
        CBUtility.Release(ref _bufferSortedParticles);


    }


}