using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

public class BoundaryTest
{
    [Test]
    public async Task GetRange_EmptyRange_ReturnsEmpty()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        // Range where min > max
        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(8)),
            new Option<Ref<int>>(true, new Ref<int>(2)),
            true).ToArray();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task GetRange_SingleItemRange_ReturnsSingleItem()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(5)),
            new Option<Ref<int>>(true, new Ref<int>(5)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].Value).IsEqualTo(5);
    }

    [Test]
    public async Task GetRange_ExclusiveSingleItemRange_ReturnsEmpty()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(5)),
            new Option<Ref<int>>(true, new Ref<int>(5)),
            false).ToArray();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task GetRange_BeyondTreeBounds_ReturnsAll()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(0)),
            new Option<Ref<int>>(true, new Ref<int>(100)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_NoMinLimit_StartsFromBeginning()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            default,
            new Option<Ref<int>>(true, new Ref<int>(5)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(6); // 0 to 5 inclusive
        await Assert.That(items[0].Value).IsEqualTo(0);
    }

    [Test]
    public async Task GetRange_NoMaxLimit_GoesToEnd()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(5)),
            default,
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5); // 5 to 9
        await Assert.That(items[^1].Value).IsEqualTo(9);
    }

    [Test]
    public async Task GetRange_NoLimits_ReturnsAll()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange<Ref<int>>(default, default, true).ToArray();

        await Assert.That(items.Length).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_MinAtTreeMin_Works()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(10)),
            new Option<Ref<int>>(true, new Ref<int>(15)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(6);
        await Assert.That(items[0].Value).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_MaxAtTreeMax_Works()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var items = tree.GetRange(
            new Option<Ref<int>>(true, new Ref<int>(15)),
            new Option<Ref<int>>(true, new Ref<int>(19)),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5);
        await Assert.That(items[^1].Value).IsEqualTo(19);
    }

    [Test]
    public async Task GetNearest_ExactMatch_OnlyReturnsMatch()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i * 10));
        }

        var nearest = tree.GetNearest(new Ref<int>(50));

        await Assert.That(nearest.MatchHasValue).IsTrue();
        await Assert.That(nearest.MatchItem.Value).IsEqualTo(50);
        await Assert.That(nearest.LowerHasValue).IsFalse();
        await Assert.That(nearest.UpperHasValue).IsFalse();
    }

    [Test]
    public async Task GetNearest_AtMinBoundary_ReturnsOnlyUpper()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i <= 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var nearest = tree.GetNearest(new Ref<int>(5));

        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsFalse();
        await Assert.That(nearest.UpperHasValue).IsTrue();
        await Assert.That(nearest.UpperItem.Value).IsEqualTo(10);
    }

    [Test]
    public async Task GetNearest_AtMaxBoundary_ReturnsOnlyLower()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i <= 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        var nearest = tree.GetNearest(new Ref<int>(25));

        await Assert.That(nearest.MatchHasValue).IsFalse();
        await Assert.That(nearest.LowerHasValue).IsTrue();
        await Assert.That(nearest.LowerItem.Value).IsEqualTo(20);
        await Assert.That(nearest.UpperHasValue).IsFalse();
    }

    [Test]
    public async Task Insert_AtTreeMinimum_Works()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        tree.InsertOrUpdate(new Ref<int>(5));

        tree.GetMin(out var min);
        await Assert.That(min.Value).IsEqualTo(5);
    }

    [Test]
    public async Task Insert_AtTreeMaximum_Works()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        tree.InsertOrUpdate(new Ref<int>(25));

        tree.GetMax(out var max);
        await Assert.That(max.Value).IsEqualTo(25);
    }

    [Test]
    public async Task Remove_NonExistent_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        bool result = tree.Remove(new Ref<int>(100), out _);
        await Assert.That(result).IsFalse();
        await Assert.That(tree.Count).IsEqualTo(10);
    }

    [Test]
    public async Task Contains_NonExistent_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        bool result = tree.Contains(new Ref<int>(100));
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Get_NonExistent_ReturnsFalse()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        bool result = tree.Get(new Ref<int>(100), out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DoForEach_CancelImmediately_ProcessesOneItem()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        int count = 0;
        bool canceled = tree.DoForEach<Ref<int>>(_ =>
        {
            count++;
            return true; // Cancel immediately
        });

        await Assert.That(canceled).IsTrue();
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task DoForEach_OnEmptyRange_DoesNotInvoke()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        int count = 0;
        tree.DoForEach(
            _ =>
            {
                count++;
                return false;
            },
            new Option<Ref<int>>(true, new Ref<int>(100)),
            new Option<Ref<int>>(true, new Ref<int>(200)),
            true);

        await Assert.That(count).IsEqualTo(0);
    }
}
