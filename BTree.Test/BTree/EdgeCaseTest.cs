using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

public class EdgeCaseTest
{
    [Test]
    public async Task EmptyTree_GetMin_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.GetMin(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetMax_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.GetMax(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_RemoveMin_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.RemoveMin(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_RemoveMax_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.RemoveMax(out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Contains_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.Contains(new Ref<int>(42));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Get_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.Get(new Ref<int>(42), out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_Remove_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        bool result = tree.Remove(new Ref<int>(42), out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetNearest_ReturnsNoValues()
    {
        BTree<Ref<int>> tree = new();
        var nearest = tree.GetNearest(new Ref<int>(42));
        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsFalse();
        await Assert.That(nearest.UpperHasValue).IsFalse();
    }

    [Test]
    public async Task EmptyTree_GetAll_ReturnsEmpty()
    {
        BTree<Ref<int>> tree = new();
        var items = tree.GetAll().ToArray();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task EmptyTree_GetRange_ReturnsEmpty()
    {
        BTree<Ref<int>> tree = new();
        var items = tree.GetRange<Ref<int>>(default, default, true).ToArray();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task EmptyTree_DoForEach_DoesNotInvokeCallback()
    {
        BTree<Ref<int>> tree = new();
        int callCount = 0;
        tree.DoForEach<Ref<int>>(_ =>
        {
            callCount++;
            return false;
        });
        await Assert.That(callCount).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_ResetsCount()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));
        await Assert.That(tree.Count).IsEqualTo(2);

        tree.Clear();
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Clear_EmptyTree_RemainsEmpty()
    {
        BTree<Ref<int>> tree = new();
        tree.Clear();
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SingleItem_AllOperations()
    {
        BTree<Ref<int>> tree = new();
        var item = new Ref<int>(42);

        tree.InsertOrUpdate(item);
        await Assert.That(tree.Count).IsEqualTo(1);

        bool containsResult = tree.Contains(item);
        await Assert.That(containsResult).IsTrue();

        bool getResult = tree.Get(item, out var retrievedItem);
        await Assert.That(getResult).IsTrue();
        await Assert.That(retrievedItem).IsEqualTo(item);

        bool getMinResult = tree.GetMin(out var min);
        await Assert.That(getMinResult).IsTrue();
        await Assert.That(min).IsEqualTo(item);

        bool getMaxResult = tree.GetMax(out var max);
        await Assert.That(getMaxResult).IsTrue();
        await Assert.That(max).IsEqualTo(item);

        var nearest = tree.GetNearest(item);
        await Assert.That(nearest.MatchHasValue).IsTrue();
        await Assert.That(nearest.MatchItem).IsEqualTo(item);
        await Assert.That(nearest.LowerHasValue).IsFalse();
        await Assert.That(nearest.UpperHasValue).IsFalse();
    }

    [Test]
    public async Task SingleItem_Remove_LeavesEmptyTree()
    {
        BTree<Ref<int>> tree = new();
        var item = new Ref<int>(42);
        tree.InsertOrUpdate(item);

        bool removeResult = tree.Remove(item, out var removedItem);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removedItem).IsEqualTo(item);
        await Assert.That(tree.Count).IsEqualTo(0);

        bool containsResult = tree.Contains(item);
        await Assert.That(containsResult).IsFalse();
    }

    [Test]
    public async Task SingleItem_RemoveMin_LeavesEmptyTree()
    {
        BTree<Ref<int>> tree = new();
        var item = new Ref<int>(42);
        tree.InsertOrUpdate(item);

        bool removeResult = tree.RemoveMin(out var removedItem);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removedItem).IsEqualTo(item);
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SingleItem_RemoveMax_LeavesEmptyTree()
    {
        BTree<Ref<int>> tree = new();
        var item = new Ref<int>(42);
        tree.InsertOrUpdate(item);

        bool removeResult = tree.RemoveMax(out var removedItem);
        await Assert.That(removeResult).IsTrue();
        await Assert.That(removedItem).IsEqualTo(item);
        await Assert.That(tree.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Update_DoesNotChangeCount()
    {
        BTree<Ref<int>> tree = new();
        var item = new Ref<int>(42);

        tree.InsertOrUpdate(item);
        await Assert.That(tree.Count).IsEqualTo(1);

        bool updated = tree.InsertOrUpdate(item);
        await Assert.That(updated).IsTrue();
        await Assert.That(tree.Count).IsEqualTo(1);
    }

    [Test]
    public async Task MinDegree_Tree_Works()
    {
        BTree<Ref<int>> tree = new(BTree<Ref<int>>.MinDegree);

        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(tree.Count).IsEqualTo(100);

        for (int i = 0; i < 100; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsTrue();
        }
    }

    [Test]
    public async Task LargeDegree_Tree_Works()
    {
        BTree<Ref<int>> tree = new(256);

        for (int i = 0; i < 1000; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(tree.Count).IsEqualTo(1000);

        for (int i = 0; i < 1000; i++)
        {
            bool contains = tree.Contains(new Ref<int>(i));
            await Assert.That(contains).IsTrue();
        }
    }

    [Test]
    public async Task DegreeBelowMinimum_UsesMinDegree()
    {
        // Degree 1 and 2 are below minimum (3), tree should still work
        BTree<Ref<int>> tree = new(1);

        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));
        tree.InsertOrUpdate(new Ref<int>(3));

        await Assert.That(tree.Count).IsEqualTo(3);
    }

    [Test]
    public async Task GetNearest_ItemBetweenValues_ReturnsBoth()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(10));
        tree.InsertOrUpdate(new Ref<int>(20));

        var nearest = tree.GetNearest(new Ref<int>(15));
        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsTrue();
        await Assert.That(nearest.LowerItem.Value).IsEqualTo(10);
        await Assert.That(nearest.UpperHasValue).IsTrue();
        await Assert.That(nearest.UpperItem.Value).IsEqualTo(20);
    }

    [Test]
    public async Task GetNearest_ItemBelowAll_ReturnsOnlyUpper()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(10));
        tree.InsertOrUpdate(new Ref<int>(20));

        var nearest = tree.GetNearest(new Ref<int>(5));
        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsFalse();
        await Assert.That(nearest.UpperHasValue).IsTrue();
        await Assert.That(nearest.UpperItem.Value).IsEqualTo(10);
    }

    [Test]
    public async Task GetNearest_ItemAboveAll_ReturnsOnlyLower()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(10));
        tree.InsertOrUpdate(new Ref<int>(20));

        var nearest = tree.GetNearest(new Ref<int>(25));
        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsTrue();
        await Assert.That(nearest.LowerItem.Value).IsEqualTo(20);
        await Assert.That(nearest.UpperHasValue).IsFalse();
    }

    [Test]
    public async Task GetRange_ExclusiveMax_ExcludesMaxValue()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(3)),
            new Option<Ref<int>>(true, new Ref<int>(7)),
            false).ToArray();

        await Assert.That(items.Length).IsEqualTo(4); // 3, 4, 5, 6
        await Assert.That(items[0].Value).IsEqualTo(3);
        await Assert.That(items[^1].Value).IsEqualTo(6);
    }

    [Test]
    public async Task GetRange_InclusiveMax_IncludesMaxValue()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(3)),
            new Option<Ref<int>>(true, new Ref<int>(7)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5); // 3, 4, 5, 6, 7
        await Assert.That(items[0].Value).IsEqualTo(3);
        await Assert.That(items[^1].Value).IsEqualTo(7);
    }

    [Test]
    public async Task DoForEach_CancelEarly_StopsIteration()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        int callCount = 0;
        bool canceled = tree.DoForEach<Ref<int>>(_ =>
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
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));

        bool result = tree.DoForEach<Ref<int>>(null!);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DoForEach_MinGreaterThanMax_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(5));

        bool result = tree.DoForEach(
            _ => false,
            new Option<Ref<int>>(true, new Ref<int>(10)),
            new Option<Ref<int>>(true, new Ref<int>(5)),
            true);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DuplicateItems_OnlyKeepsLast()
    {
        BTree<Ref<int>> tree = new();

        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(5)); // Same key
        }

        await Assert.That(tree.Count).IsEqualTo(1);
    }

    [Test]
    public async Task NegativeValues_Work()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(-10));
        tree.InsertOrUpdate(new Ref<int>(-5));
        tree.InsertOrUpdate(new Ref<int>(0));
        tree.InsertOrUpdate(new Ref<int>(5));
        tree.InsertOrUpdate(new Ref<int>(10));

        await Assert.That(tree.Count).IsEqualTo(5);

        tree.GetMin(out var min);
        await Assert.That(min.Value).IsEqualTo(-10);

        tree.GetMax(out var max);
        await Assert.That(max.Value).IsEqualTo(10);
    }
}
