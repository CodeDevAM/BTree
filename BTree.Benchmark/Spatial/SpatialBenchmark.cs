using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BTree.Test.BTree.Spatial;
using System;
using System.Linq;

namespace BTree.Benchmark.Spatial;

[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class SpatialBenchmark
{
    [Params(100_000)]
    public int N;

    [Params(Pattern.Random, Pattern.Const)]
    public Pattern Pattern;

    [Params(false, true)]
    public bool ZOrder;

    public Point[] Points { get; set; }

    public BPlusTree<Point, bool> Tree { get; set; }

    public Random Random { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random = new(7);

        Points = Enumerable.Range(0, N).Select(i =>
        {
            double x = Pattern == Pattern.Const ? 0.0 : Random.NextDouble() <= 0.5 ? Random.NextDouble() : -Random.NextDouble();
            double y = Random.NextDouble() <= 0.5 ? Random.NextDouble() : -Random.NextDouble();
            Point point = new(x, y, ZOrder);
            return point;
        }).ToArray();

        Tree = new();

        foreach (Point point in Points)
        {
            Tree.InsertOrUpdate(point, false);
        }
    }

    [Benchmark]
    public void General()
    {
        int index = Random.Next(Points.Length);
        Point point = Points[index];
        Tree.Get(point, out _);
        Tree.Remove(point, out _);
        Tree.InsertOrUpdate(point, false);
    }

    [Benchmark]
    public void Get()
    {
        int index = Random.Next(Points.Length);
        Point point = Points[index];
        Tree.Get(point, out _);
    }

    [Benchmark]
    public void QuerySmall()
    {
        int count = 0;
        Point lowerLimit = new(-0.05, -0.05, ZOrder);
        Point upperLimit = new(0.05, 0.05, ZOrder);
        Tree.DoForEach((point, _) =>
        {
            if (point.X >= lowerLimit.X && point.X <= upperLimit.X && point.Y >= lowerLimit.Y && point.Y <= upperLimit.Y)
            {
                count++;
            }
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void QuerySmallHorizontalSlice()
    {
        int count = 0;
        Point lowerLimit = new(-1.0, -0.05, ZOrder);
        Point upperLimit = new(1.0, 0.05, ZOrder);
        Tree.DoForEach((point, _) =>
        {
            if (point.X >= lowerLimit.X && point.X <= upperLimit.X && point.Y >= lowerLimit.Y && point.Y <= upperLimit.Y)
            {
                count++;
            }
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void QuerySmallVerticalSlice()
    {
        int count = 0;
        Point lowerLimit = new(-0.05, -1.0, ZOrder);
        Point upperLimit = new(0.05, 1.0, ZOrder);
        Tree.DoForEach((point, _) =>
        {
            if (point.X >= lowerLimit.X && point.X <= upperLimit.X && point.Y >= lowerLimit.Y && point.Y <= upperLimit.Y)
            {
                count++;
            }
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void QueryLarge()
    {
        int count = 0;
        Point lowerLimit = new(-0.25, -0.25, ZOrder);
        Point upperLimit = new(0.25, 0.25, ZOrder);
        Tree.DoForEach((point, _) =>
        {
            if (point.X >= lowerLimit.X && point.X <= upperLimit.X && point.Y >= lowerLimit.Y && point.Y <= upperLimit.Y)
            {
                count++;
            }
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void ManualQuerySmall()
    {
        int count = 0;
        Point lowerLimit = new(-0.05, -0.05, ZOrder);
        Point upperLimit = new(0.05, 0.05, ZOrder);
        foreach (Point point in Points)
        {
            if (point.X >= lowerLimit.X && point.X <= upperLimit.X && point.Y >= lowerLimit.Y && point.Y <= upperLimit.Y)
            {
                count++;
            }
        }
    }

    [Benchmark]
    public void ManualQuerySmall2()
    {
        int count = 0;
        Point lowerLimit = new(-0.05, -0.05, ZOrder);
        Point upperLimit = new(0.05, 0.05, ZOrder);
        foreach (Point point in Points)
        {
            if (point.CompareTo(lowerLimit) >= 0 && point.CompareTo(upperLimit) <= 0)
            {
                count++;
            }
        }
    }

}
