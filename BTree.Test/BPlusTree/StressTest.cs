using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public class StressTest
{
    [Test]
    public async Task InsertAndRemove_LargeDataset_MaintainsIntegrity()
    {
        BPlusTree<int, int> tree = new();
        const int count = 10000;

        // Insert all items
        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify all items exist
        for (int i = 0; i < count; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsTrue();

            tree.Get(i, out var value);
            await Assert.That(value).IsEqualTo(i * 10);
        }

        // Remove half in random order
        int[] indicesToRemove = Enumerable.Range(0, count / 2).ToArray();
        indicesToRemove.Shuffle();

        foreach (int i in indicesToRemove)
        {
            bool removed = tree.Remove(i, out _);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(count / 2);

        // Verify remaining items
        for (int i = count / 2; i < count; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsTrue();
        }

        // Verify removed items are gone
        for (int i = 0; i < count / 2; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsFalse();
        }
    }

    [Test]
    public async Task RandomInsertRemove_MaintainsIntegrity()
    {
        BPlusTree<int, int> tree = new();
        Dictionary<int, int> expectedItems = [];
        Random random = new(42);
        const int operations = 5000;

        for (int op = 0; op < operations; op++)
        {
            if (random.Next(2) == 0 || expectedItems.Count == 0)
            {
                // Insert
                int key = random.Next(1000);
                int value = random.Next(10000);
                tree.InsertOrUpdate(key, value);
                expectedItems[key] = value;
            }
            else
            {
                // Remove
                int key = expectedItems.Keys.First();
                tree.Remove(key, out _);
                expectedItems.Remove(key);
            }
        }

        await Assert.That(tree.Count).IsEqualTo(expectedItems.Count);

        foreach (var kvp in expectedItems)
        {
            bool getResult = tree.Get(kvp.Key, out var value);
            await Assert.That(getResult).IsTrue();
            await Assert.That(value).IsEqualTo(kvp.Value);
        }
    }

    [Test]
    public async Task AlternatingInsertRemove_MaintainsOrder()
    {
        BPlusTree<int, int> tree = new();

        for (int round = 0; round < 100; round++)
        {
            // Insert 100 items
            for (int i = 0; i < 100; i++)
            {
                tree.InsertOrUpdate(round * 100 + i, i);
            }

            // Remove first 50
            for (int i = 0; i < 50; i++)
            {
                tree.Remove(round * 100 + i, out _);
            }
        }

        await Assert.That(tree.Count).IsEqualTo(5000);

        // Verify items are in order
        int lastKey = int.MinValue;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Key).IsGreaterThan(lastKey);
            lastKey = item.Key;
        }
    }

    [Test]
    public async Task RemoveMinRepeatedly_EmptiesTree()
    {
        BPlusTree<int, int> tree = new();
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        int expectedMin = 0;
        while (tree.Count > 0)
        {
            bool result = tree.RemoveMin(out var min);
            await Assert.That(result).IsTrue();
            await Assert.That(min.Key).IsEqualTo(expectedMin);
            await Assert.That(min.Value).IsEqualTo(expectedMin * 10);
            expectedMin++;
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveMaxRepeatedly_EmptiesTree()
    {
        BPlusTree<int, int> tree = new();
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        int expectedMax = count - 1;
        while (tree.Count > 0)
        {
            bool result = tree.RemoveMax(out var max);
            await Assert.That(result).IsTrue();
            await Assert.That(max.Key).IsEqualTo(expectedMax);
            await Assert.That(max.Value).IsEqualTo(expectedMax * 10);
            expectedMax--;
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RepeatedClearAndRefill_Works()
    {
        BPlusTree<int, string> tree = new();

        for (int round = 0; round < 10; round++)
        {
            for (int i = 0; i < 100; i++)
            {
                tree.InsertOrUpdate(i, $"value{i}");
            }

            await Assert.That(tree.Count).IsEqualTo(100);

            tree.Clear();

            await Assert.That(tree.Count).IsEqualTo(0);

            bool getMinResult = tree.GetMin(out _);
            await Assert.That(getMinResult).IsFalse();
        }
    }

    [Test]
    public async Task SequentialReverseInsert_MaintainsOrder()
    {
        BPlusTree<int, int> tree = new();
        const int count = 1000;

        // Insert in reverse order
        for (int i = count - 1; i >= 0; i--)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify order
        int expected = 0;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Key).IsEqualTo(expected);
            await Assert.That(item.Value).IsEqualTo(expected * 10);
            expected++;
        }
    }

    [Test]
    public async Task AlternatingHighLowInsert_MaintainsOrder()
    {
        BPlusTree<int, int> tree = new();
        const int count = 500;

        // Insert alternating high and low values
        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(i, i);
            tree.InsertOrUpdate(count * 2 - i, count * 2 - i);
        }

        await Assert.That(tree.Count).IsEqualTo(count * 2);

        // Verify order
        int lastKey = int.MinValue;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Key).IsGreaterThan(lastKey);
            lastKey = item.Key;
        }
    }

    [Test]
    public async Task ManyUpdates_KeepsLatestValue()
    {
        BPlusTree<int, int> tree = new();

        // Insert same key many times with different values
        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(42, i);
        }

        await Assert.That(tree.Count).IsEqualTo(1);

        tree.Get(42, out var value);
        await Assert.That(value).IsEqualTo(999);
    }

    [Test]
    [MethodDataSource(nameof(GetDegreeTestCases))]
    public async Task VariousDegrees_AllWork(ushort degree)
    {
        BPlusTree<int, int> tree = new(degree);
        const int count = 500;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify all items exist
        for (int i = 0; i < count; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsTrue();
        }

        // Remove all
        for (int i = 0; i < count; i++)
        {
            bool removed = tree.Remove(i, out _);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task LeafChain_StaysIntact_AfterManyOperations()
    {
        BPlusTree<int, int> tree = new(4); // Small degree to force many splits
        Random random = new(42);

        // Insert many items
        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(random.Next(500), i);
        }

        // Remove some
        for (int i = 0; i < 250; i++)
        {
            tree.Remove(random.Next(500), out _);
        }

        // Insert more
        for (int i = 0; i < 500; i++)
        {
            tree.InsertOrUpdate(random.Next(500), i);
        }

        // Verify leaf chain by checking order
        int lastKey = int.MinValue;
        int itemCount = 0;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Key).IsGreaterThanOrEqualTo(lastKey);
            lastKey = item.Key;
            itemCount++;
        }

        await Assert.That(itemCount).IsEqualTo((int)tree.Count);
    }

    [Test]
    public async Task RangeQuery_AfterManyOperations_ReturnsCorrectResults()
    {
        BPlusTree<int, int> tree = new();

        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        // Remove every 3rd item
        for (int i = 0; i < 1000; i += 3)
        {
            tree.Remove(i, out _);
        }

        // Range query
        var range = tree.GetRange(
            new Option<int>(true, 100),
            new Option<int>(true, 200),
            true).ToList();

        // Verify all items in range
        foreach (var item in range)
        {
            await Assert.That(item.Key).IsGreaterThanOrEqualTo(100);
            await Assert.That(item.Key).IsLessThanOrEqualTo(200);
            await Assert.That(item.Key % 3).IsNotEqualTo(0);
        }

        // Verify order
        for (int i = 0; i < range.Count - 1; i++)
        {
            await Assert.That(range[i].Key).IsLessThan(range[i + 1].Key);
        }
    }

    public static IEnumerable<object[]> GetDegreeTestCases()
    {
        yield return [BPlusTree<int, int>.MinDegree];
        yield return [(ushort)4];
        yield return [(ushort)8];
        yield return [(ushort)16];
        yield return [(ushort)32];
        yield return [(ushort)64];
        yield return [(ushort)128];
        yield return [(ushort)256];
    }
}
