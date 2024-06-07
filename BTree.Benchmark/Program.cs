using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BTree;
using System;
using System.Linq;

Summary summary = BenchmarkRunner.Run<BTreeBenchmark>();

namespace BTree
{
    public class BTreeBenchmark
    {
        [Params(1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000)]
        public int N;

        public int[] Items { get; set; }

        public BTree<int> Tree { get; set; }

        private Random _Random { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Items = Enumerable.Range(0, N).ToArray();
            _Random = new();

            Tree = new();

            foreach (int t in Items)
            {
                Tree.InsertOrUpdate(t);
            }
        }

        [Benchmark]
        public void BTree()
        {
            int index = _Random.Next(Items.Length);
            int item = Items[index];
            Tree.Get(item, out int _);
            Tree.Remove(item, out int _);
            Tree.InsertOrUpdate(item);
        }

    }
}
