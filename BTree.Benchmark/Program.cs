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
        [Params(100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000)]
        public int N;

        private int[] _Items { get; set; }

        private BTree<int> _BTree { get; set; }

        private BPlusTree<int, int> _BPlusTree { get; set; }

        private Random _Random { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _Items = Enumerable.Range(0, N).ToArray();
            _Random = new();

            _BTree = new();
            _BPlusTree = new();

            foreach (int t in _Items)
            {
                _BTree.InsertOrUpdate(t);
            }
        }

        [Benchmark]
        public void BTree()
        {
            int index = _Random.Next(_Items.Length);
            int item = _Items[index];
            _BTree.Get(item, out int _);
            _BTree.Remove(item, out int _);
            _BTree.InsertOrUpdate(item);
        }

        [Benchmark]
        public void BPlusTree()
        {
            int index = _Random.Next(_Items.Length);
            int item = _Items[index];
            _BPlusTree.Get(item, out int _);
            _BPlusTree.Remove(item, out int _);
            _BPlusTree.InsertOrUpdate(item, item);
        }

    }
}
