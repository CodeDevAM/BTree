using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

public class StressTest
{
    [Test]
    public async Task InsertAndRemove_LargeDataset_MaintainsIntegrity()
    {
        BTree<Ref<int>> tree = new();
        const int count = 10000;

        // Insert all items
        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify all items exist
        for (int i = 0; i < count; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsTrue();
        }

        // Remove half in random order
        int[] indicesToRemove = Enumerable.Range(0, count / 2).ToArray();
        indicesToRemove.Shuffle();

        foreach (int i in indicesToRemove)
        {
            bool removed = tree.Remove(new Ref<int>(i), out _);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(count / 2);

        // Verify remaining items
        for (int i = count / 2; i < count; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsTrue();
        }

        // Verify removed items are gone
        for (int i = 0; i < count / 2; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsFalse();
        }
    }

    [Test]
    public async Task RandomInsertRemove_MaintainsIntegrity()
    {
        BTree<Ref<int>> tree = new();
        HashSet<int> expectedItems = [];
        Random random = new(42);
        const int operations = 5000;

        for (int op = 0; op < operations; op++)
        {
            if (random.Next(2) == 0 || expectedItems.Count == 0)
            {
                // Insert
                int value = random.Next(1000);
                tree.InsertOrUpdate(new Ref<int>(value));
                expectedItems.Add(value);
            }
            else
            {
                // Remove
                int value = expectedItems.First();
                tree.Remove(new Ref<int>(value), out _);
                expectedItems.Remove(value);
            }
        }

        await Assert.That(tree.Count).IsEqualTo(expectedItems.Count);

        foreach (int value in expectedItems)
        {
            bool contains = tree.Contains(new Ref<int>(value));
            await Assert.That(contains).IsTrue();
        }
    }

    [Test]
    public async Task AlternatingInsertRemove_MaintainsOrder()
    {
        BTree<Ref<int>> tree = new();

        for (int round = 0; round < 100; round++)
        {
            // Insert 100 items
            for (int i = 0; i < 100; i++)
            {
                tree.InsertOrUpdate(new Ref<int>(round * 100 + i));
            }

            // Remove first 50
            for (int i = 0; i < 50; i++)
            {
                tree.Remove(new Ref<int>(round * 100 + i), out _);
            }
        }

        await Assert.That(tree.Count).IsEqualTo(5000);

        // Verify items are in order
        int lastValue = int.MinValue;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Value).IsGreaterThan(lastValue);
            lastValue = item.Value;
        }
    }

    [Test]
    public async Task RemoveMinRepeatedly_EmptiesTree()
    {
        BTree<Ref<int>> tree = new();
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        int expectedMin = 0;
        while (tree.Count > 0)
        {
            bool result = tree.RemoveMin(out var min);
            await Assert.That(result).IsTrue();
            await Assert.That(min.Value).IsEqualTo(expectedMin);
            expectedMin++;
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveMaxRepeatedly_EmptiesTree()
    {
        BTree<Ref<int>> tree = new();
        const int count = 1000;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        int expectedMax = count - 1;
        while (tree.Count > 0)
        {
            bool result = tree.RemoveMax(out var max);
            await Assert.That(result).IsTrue();
            await Assert.That(max.Value).IsEqualTo(expectedMax);
            expectedMax--;
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RepeatedClearAndRefill_Works()
    {
        BTree<Ref<int>> tree = new();

        for (int round = 0; round < 10; round++)
        {
            for (int i = 0; i < 100; i++)
            {
                tree.InsertOrUpdate(new Ref<int>(i));
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
        BTree<Ref<int>> tree = new();
        const int count = 1000;

        // Insert in reverse order
        for (int i = count - 1; i >= 0; i--)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify order
        int expected = 0;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Value).IsEqualTo(expected);
            expected++;
        }
    }

    [Test]
    public async Task AlternatingHighLowInsert_MaintainsOrder()
    {
        BTree<Ref<int>> tree = new();
        const int count = 500;

        // Insert alternating high and low values
        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
            tree.InsertOrUpdate(new Ref<int>(count * 2 - i));
        }

        await Assert.That(tree.Count).IsEqualTo(count * 2);

        // Verify order
        int lastValue = int.MinValue;
        foreach (var item in tree.GetAll())
        {
            await Assert.That(item.Value).IsGreaterThan(lastValue);
            lastValue = item.Value;
        }
    }

    [Test]
    public async Task ManyDuplicateInserts_OnlyKeepsOne()
    {
        BTree<Ref<int>> tree = new();

        // Insert same value many times
        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(42));
        }

        await Assert.That(tree.Count).IsEqualTo(1);
    }

    [Test]
    [MethodDataSource(nameof(GetDegreeTestCases))]
    public async Task VariousDegrees_AllWork(ushort degree)
    {
        BTree<Ref<int>> tree = new(degree);
        const int count = 500;

        for (int i = 0; i < count; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        // Verify all items exist
        for (int i = 0; i < count; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsTrue();
        }

        // Remove all
        for (int i = 0; i < count; i++)
        {
            bool removed = tree.Remove(new Ref<int>(i), out _);
            await Assert.That(removed).IsTrue();
        }

        await Assert.That(tree.Count).IsEqualTo(0);
    }

    public static IEnumerable<object[]> GetDegreeTestCases()
    {
        yield return [BTree<int>.MinDegree];
        yield return [(ushort)4];
        yield return [(ushort)8];
        yield return [(ushort)16];
        yield return [(ushort)32];
        yield return [(ushort)64];
        yield return [(ushort)128];
        yield return [(ushort)256];
    }
}
