using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class TestGC : MonoBehaviour
{
    List<int> list;
    // Start is called before the first frame update
    void Start()
    {
        list = new List<int>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Profiler.BeginSample("== test gc ==");
            for(int i = 0; i < 100000; i++)
            {
                list.Clear();
                list.Add(i);
            }
            Profiler.EndSample();
        }
    }
}
