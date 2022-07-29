using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadixTest : MonoBehaviour
{

    private const int THREADS = 128;

    public int sortRadius;

    public Vector2Int[] TestNumberIn = new Vector2Int[100];
 
    Random random = new Random();

    public int MaxDitLengthIndex;

    public int MaxDitLengthGrid;

    public int NumElements { get; private set; }

    // Start is called before the first frame update
    void Start()

    {
        //    TestNumberIn[0] = new Vector2Int(199, 32);
        //    TestNumberIn[1] = new Vector2Int(324, 9999);
        //    TestNumberIn[2] = new Vector2Int(76, 980);
        //    TestNumberIn[3] = new Vector2Int(54, 1);
        //    TestNumberIn[4] = new Vector2Int(10, 32);
        //    TestNumberIn[5] = new Vector2Int(8, 43);
        //    TestNumberIn[6] = new Vector2Int(87, 654);
        //    TestNumberIn[7] = new Vector2Int(543, 0);
        //    TestNumberIn[8] = new Vector2Int(67, 90);
        //    TestNumberIn[9] = new Vector2Int(654, 873);

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start(); //  开始监视代码运行时间





        for (int i = 0; i < TestNumberIn.Length; i++)
        {
            TestNumberIn[i] = new Vector2Int(Mathf.CeilToInt(Random.Range(0, 100)), Mathf.CeilToInt(Random.Range(0, 1000)));

        }

        FindMax();
        for (int i = 1; i >= 0; i--)
        {
            GetRadix(i);
        }



        stopwatch.Stop(); //  停止监视

        //  获取当前实例测量得出的总时间
        System.TimeSpan timespan = stopwatch.Elapsed;
        //   double hours = timespan.TotalHours; // 总小时
        //    double minutes = timespan.TotalMinutes;  // 总分钟
        //    double seconds = timespan.TotalSeconds;  //  总秒数
        double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

        //打印代码执行时间
        Debug.Log("Testime ===================>>>>>>>>>>>>>>>>>>>>>>>>"+ milliseconds);
      
      


    }


    public void FindMax()
    {
        if (TestNumberIn == null) return;
        int max = 0;
        //最大粒子数
        for (int i = 0; i < TestNumberIn.Length; i++)
        {
            if (TestNumberIn[i].y > max)
            {
                max = TestNumberIn[i].y;
            }

        }
        Debug.Log("max = "+ max);

        while (max != 0)
        {
            MaxDitLengthIndex++;
            max /= sortRadius;

        }
        Debug.Log("MaxDitLengthIndex = " + MaxDitLengthIndex);
        
        int max2 = 0;
        //最大Grid
        for (int i = 0; i < TestNumberIn.Length; i++)
        {
            if (TestNumberIn[i].x > max2)
            {
                max2 = TestNumberIn[i].x;
            }

        }
        Debug.Log("Max2 = " + max2);

        while (max2 != 0)
        {
            MaxDitLengthGrid++;
            max2 /= sortRadius;
        }
        

        Debug.Log("MaxDitLengthGrid = " + MaxDitLengthGrid);

    }

    public void GetRadix(int round)
    {
       
        int[] counting = new int[sortRadius];
        
        int dev = 1;

        //System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < Mathf.Max(MaxDitLengthGrid, MaxDitLengthIndex); i++)
        {
            Vector2Int[] result = new Vector2Int[TestNumberIn.Length];
            //sb.AppendFormat("Epoch {0}:", i+1);
            //计算基数，放进counting桶里
            for(int j = 0; j < TestNumberIn.Length;j++)
            {
                int radix = TestNumberIn[j][round] / dev % sortRadius;
                counting[radix]++;
            }
            //计算每一个桶的offset计算绝对位置起始值
            for (int j = 1; j < counting.Length; j++)
            {
                counting[j] += counting[j - 1];
            }

           
            //倒序遍历
            for (int j = TestNumberIn.Length-1; j >= 0; j--)
            {
                int radix = TestNumberIn[j][round] / dev % sortRadius;     
                result[--counting[radix]] = TestNumberIn[j];
            
            }

            TestNumberIn = result;

            //for (int j = 0; j < result.Length; j++)
            //{
            //    sb.Append(" " + result[j]);

            //}
            for (int j = 0; j < counting.Length; j++)
            {
                counting[j] = 0;
            }
          //  sb.Append("\n");
            
            dev *= sortRadius;
        }
       // Debug.Log(sb.ToString());
        
    }

   
    public void Copy()
    {
       
    }
    // Update is called once per frame
    void Update()
    {
        



    }
}
