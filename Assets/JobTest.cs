using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

public class JobTest : MonoBehaviour
{
    [SerializeField] int a = 0;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Restart();
            Job1 j = new Job1(a, new AA());
            var jh = j.Schedule();
            jh.Complete();
            stopwatch.Stop();
            UnityEngine.Debug.Log($"花费了 {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}

struct Job1 : IJob
{

    private int rayCount;
    AA tran;

    public Job1(int rayCount, AA tran)
    {
        this.rayCount = rayCount;
        this.tran = tran;
    }

    public void Execute()
    {
        int zz = 2;
        for (int i = 0; i < rayCount; i++)
        {
            Math.Pow(2, i);
        }
    }
}
public class Job2 : IJobParallelFor
{

    public void Execute(int index)
    {

    }
}
public struct AA
{

}