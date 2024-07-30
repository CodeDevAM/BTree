using BenchmarkDotNet.Attributes;
using BTree.Test.BTree.Spatial;
using System;
using System.Linq;

namespace BTree.Benchmark;

public class BTreeBenchmark
{
    [Params(1_000, 1_000_000)]
    public int N;

    [Params(Pattern.Random, Pattern.Const)]
    public Pattern Pattern;

    public int[] Items { get; set; }

    public BTree<int> Tree { get; set; }

    public Random Random { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random = new();

        Items = Enumerable.Range(0, N).Select(i => Random.NextDouble() <= 0.5 ? Random.Next() : -Random.Next()).ToArray();

        Tree = new();

        foreach (int item in Items)
        {
            Tree.InsertOrUpdate(item);
        }
    }

    [Benchmark]
    public void General()
    {
        int index = Random.Next(Items.Length);
        int item = Items[index];
        Tree.Get(item, out _);
        Tree.Remove(item, out _);
        Tree.InsertOrUpdate(item);
    }

    [Benchmark]
    public void GetNearest()
    {
        int index = Random.Next(Items.Length);
        int item = Items[index];
        Tree.GetNearest(item);
    }

    [Benchmark]
    public void QuerySmall()
    {
        int count = 0;
        int lowerLimit = (int)(-0.05 * Int32.MaxValue);
        int upperLimit = (int)(0.05 * Int32.MaxValue);
        Tree.DoForEach<int>(item =>
        {
            count++;
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void QueryLarge()
    {
        int count = 0;
        int lowerLimit = (int)(-0.25 * Int32.MaxValue);
        int upperLimit = (int)(0.25 * Int32.MaxValue);
        Tree.DoForEach<int>(item =>
        {
            count++;
            return false;
        }, lowerLimit, upperLimit, true);
    }

    [Benchmark]
    public void ManualQuerySmall()
    {
        int count = 0;
        int lowerLimit = (int)(-0.05 * Int32.MaxValue);
        int upperLimit = (int)(0.05 * Int32.MaxValue);
        foreach (int item in Items)
        {
            if (item >= lowerLimit && item <= upperLimit)
            {
                count++;
            }
        }
    }
}