using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public class EdgeCaseTest
{
    [Test]
    public async Task EmptyTree_GetMin_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.GetMin(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetMax_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.GetMax(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_RemoveMin_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.RemoveMin(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_RemoveMax_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.RemoveMax(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Contains_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.Contains(42);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Get_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.Get(42, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Remove_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        bool result = tree.Remove(42, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetNearest_ReturnsNoValues()
    {
        BPlusTree<int, string> tree = new();
        var nearest = tree.GetNearest(42);
        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsFalse();
        await Assert.That(nearest.Upper.HasValue).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetAll_ReturnsEmpty()
    {
        BPlusTree<int, string> tree = new();
        var items = tree.GetAll().ToArray();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task EmptyTree_GetRange_ReturnsEmpty()
    {
        BPlusTree<int, string> tree = new();
        var items = tree.GetRange(default, default, true).ToArray();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task EmptyTree_DoForEach_DoesNotInvokeCallback()
    {
        BPlusTree<int, string> tree = new();
        int callCount = 0;
        tree.DoForEach((_, _) =>
        {
            callCount++;
            return false;
        });
        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_ResetsCount()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");
        await Assert.That(tree.Count).IsEqualTo(2);

        tree.Clear();
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_EmptyTree_RemainsEmpty()
    {
        BPlusTree<int, string> tree = new();
        tree.Clear();
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SingleItem_AllOperations()
    {
        BPlusTree<int, string> tree = new();

        tree.InsertOrUpdate(42, "value");
        await Assert.That(tree.Count).IsEqualTo(1);

        bool containsResult = tree.Contains(42);
        await Assert.That(containsResult).IsTrue();

        bool getResult = tree.Get(42, out var retrievedItem);
        await Assert.That(getResult).IsTrue();
        await Assert.That(retrievedItem).IsEqualTo("value");

        bool getMinResult = tree.GetMin(out var min);
        await Assert.That(getMinResult).IsTrue();
        await Assert.That(min.Key).IsEqualTo(42);
        await Assert.That(min.Value).IsEqualTo("value");

        bool getMaxResult = tree.GetMax(out var max);
        await Assert.That(getMaxResult).IsTrue();
        await Assert.That(max.Key).IsEqualTo(42);
        await Assert.That(max.Value).IsEqualTo("value");

        var nearest = tree.GetNearest(42);
        await Assert.That(nearest.Match.HasValue).IsTrue();
        await Assert.That(nearest.Match.Value.Key).IsEqualTo(42);
        await Assert.That(nearest.Lower.HasValue).IsFalse();
        await Assert.That(nearest.Upper.HasValue).IsFalse();
    }

    [Test]
    public async Task SingleItem_Remove_LeavesEmptyTree()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(42, "value");

        bool removeResult = tree.Remove(42, out var removedItem);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removedItem).IsEqualTo("value");
        await Assert.That(tree.Count).IsEqualTo(0);

        bool containsResult = tree.Contains(42);
        await Assert.That(containsResult).IsFalse();
    }

    [Test]
    public async Task SingleItem_RemoveMin_LeavesEmptyTree()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(42, "value");

        bool removeResult = tree.RemoveMin(out var removed);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removed.Key).IsEqualTo(42);
        await Assert.That(removed.Value).IsEqualTo("value");
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SingleItem_RemoveMax_LeavesEmptyTree()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(42, "value");

        bool removeResult = tree.RemoveMax(out var removed);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removed.Key).IsEqualTo(42);
        await Assert.That(removed.Value).IsEqualTo("value");
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Update_DoesNotChangeCount()
    {
        BPlusTree<int, string> tree = new();

        tree.InsertOrUpdate(42, "first");
        await Assert.That(tree.Count).IsEqualTo(1);

        bool updated = tree.InsertOrUpdate(42, "second");
        await Assert.That(updated).IsTrue();
        await Assert.That(tree.Count).IsEqualTo(1);

        tree.Get(42, out var value);
        await Assert.That(value).IsEqualTo("second");
    }

    [Test]
    public async Task MinDegree_Tree_Works()
    {
        BPlusTree<int, int> tree = new(BPlusTree<int, int>.MinDegree);

        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(100);

        for (int i = 0; i < 100; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsTrue();
        }
    }

    [Test]
    public async Task LargeDegree_Tree_Works()
    {
        BPlusTree<int, int> tree = new(256);

        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        await Assert.That(tree.Count).IsEqualTo(1000);

        for (int i = 0; i < 1000; i++)
        {
            bool contains = tree.Contains(i);
            await Assert.That(contains).IsTrue();
        }
    }

    [Test]
    public async Task DegreeBelowMinimum_UsesMinDegree()
    {
        BPlusTree<int, int> tree = new(1);

        tree.InsertOrUpdate(1, 10);
        tree.InsertOrUpdate(2, 20);
        tree.InsertOrUpdate(3, 30);

        await Assert.That(tree.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetNearest_ItemBetweenValues_ReturnsBoth()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(10, "ten");
        tree.InsertOrUpdate(20, "twenty");

        var nearest = tree.GetNearest(15);
        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsTrue();
        await Assert.That(nearest.Lower.Value.Key).IsEqualTo(10);
        await Assert.That(nearest.Upper.HasValue).IsTrue();
        await Assert.That(nearest.Upper.Value.Key).IsEqualTo(20);
    }

    [Test]
    public async Task GetNearest_ItemBelowAll_ReturnsOnlyUpper()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(10, "ten");
        tree.InsertOrUpdate(20, "twenty");

        var nearest = tree.GetNearest(5);
        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsFalse();
        await Assert.That(nearest.Upper.HasValue).IsTrue();
        await Assert.That(nearest.Upper.Value.Key).IsEqualTo(10);
    }

    [Test]
    public async Task GetNearest_ItemAboveAll_ReturnsOnlyLower()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(10, "ten");
        tree.InsertOrUpdate(20, "twenty");

        var nearest = tree.GetNearest(25);
        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsTrue();
        await Assert.That(nearest.Lower.Value.Key).IsEqualTo(20);
        await Assert.That(nearest.Upper.HasValue).IsFalse();
    }

    [Test]
    public async Task GetRange_ExclusiveMax_ExcludesMaxValue()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 3),
            new Option<int>(true, 7),
            false).ToArray();

        await Assert.That(items.Length).IsEqualTo(4); // 3, 4, 5, 6
        await Assert.That(items[0].Key).IsEqualTo(3);
        await Assert.That(items[^1].Key).IsEqualTo(6);
    }

    [Test]
    public async Task GetRange_InclusiveMax_IncludesMaxValue()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 3),
            new Option<int>(true, 7),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5); // 3, 4, 5, 6, 7
        await Assert.That(items[0].Key).IsEqualTo(3);
        await Assert.That(items[^1].Key).IsEqualTo(7);
    }

    [Test]
    public async Task DoForEach_CancelEarly_StopsIteration()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        int callCount = 0;
        bool canceled = tree.DoForEach((_, _) =>
        {
            callCount++;
            return callCount >= 10;
        });

        await Assert.That(canceled).IsTrue();
        await Assert.That(callCount).IsEqualTo(10);
    }

    [Test]
    public async Task DoForEach_NullCallback_ReturnsFalse()
    {
        BPlusTree<int, int> tree = new();
        tree.InsertOrUpdate(1, 10);

        bool result = tree.DoForEach(null!);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DoForEach_MinGreaterThanMax_ReturnsFalse()
    {
        BPlusTree<int, int> tree = new();
        tree.InsertOrUpdate(5, 50);

        bool result = tree.DoForEach(
            (_, _) => false,
            new Option<int>(true, 10),
            new Option<int>(true, 5),
            true);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DuplicateKeys_OnlyKeepsLast()
    {
        BPlusTree<int, string> tree = new();

        tree.InsertOrUpdate(5, "first");
        tree.InsertOrUpdate(5, "second");
        tree.InsertOrUpdate(5, "third");

        await Assert.That(tree.Count).IsEqualTo(1);
        tree.Get(5, out var value);
        await Assert.That(value).IsEqualTo("third");
    }

    [Test]
    public async Task NegativeKeys_Work()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(-10, "minus ten");
        tree.InsertOrUpdate(-5, "minus five");
        tree.InsertOrUpdate(0, "zero");
        tree.InsertOrUpdate(5, "five");
        tree.InsertOrUpdate(10, "ten");

        await Assert.That(tree.Count).IsEqualTo(5);

        tree.GetMin(out var min);
        await Assert.That(min.Key).IsEqualTo(-10);

        tree.GetMax(out var max);
        await Assert.That(max.Key).IsEqualTo(10);
    }

    [Test]
    public async Task LeafChain_Integrity_AfterInserts()
    {
        BPlusTree<int, int> tree = new(3); // Small degree to force splits

        // Insert enough items to cause multiple splits
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        // Verify order by iterating
        List<int> keys = [];
        tree.DoForEach((key, _) =>
        {
            keys.Add(key);
            return false;
        });

        await Assert.That(keys.Count).IsEqualTo(100);

        // Keys should be in order
        for (int i = 0; i < keys.Count - 1; i++)
        {
            await Assert.That(keys[i]).IsLessThan(keys[i + 1]);
        }
    }

    [Test]
    public async Task LeafChain_Integrity_AfterRemoves()
    {
        BPlusTree<int, int> tree = new(3);

        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        // Remove half the items
        for (int i = 0; i < 50; i++)
        {
            tree.Remove(i * 2, out _);
        }

        // Verify chain
        List<int> keys = [];
        tree.DoForEach((key, _) =>
        {
            keys.Add(key);
            return false;
        });

        await Assert.That(keys.Count).IsEqualTo(50);

        for (int i = 0; i < keys.Count - 1; i++)
        {
            await Assert.That(keys[i]).IsLessThan(keys[i + 1]);
        }
    }

    [Test]
    public async Task GetAll_ReturnsItemsInOrder()
    {
        BPlusTree<int, int> tree = new();
        int[] insertOrder = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];

        foreach (int i in insertOrder)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        int expected = 0;
        foreach (var kvp in tree.GetAll())
        {
            await Assert.That(kvp.Key).IsEqualTo(expected);
            await Assert.That(kvp.Value).IsEqualTo(expected * 10);
            expected++;
        }
    }

    [Test]
    public async Task NullValue_IsAllowed()
    {
        BPlusTree<int, string> tree = new();

        tree.InsertOrUpdate(1, null);
        await Assert.That(tree.Count).IsEqualTo(1);

        bool getResult = tree.Get(1, out var value);
        await Assert.That(getResult).IsTrue();
        await Assert.That(value).IsNull();
    }
}
