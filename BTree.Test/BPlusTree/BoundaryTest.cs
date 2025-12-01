using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public class BoundaryTest
{
    [Test]
    public async Task GetRange_EmptyRange_ReturnsEmpty()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        // Range where min > max
        var items = tree.GetRange(
            new Option<int>(true, 8),
            new Option<int>(true, 2),
            true).ToArray();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task GetRange_SingleItemRange_ReturnsSingleItem()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 5),
            new Option<int>(true, 5),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(1);
        await Assert.That(items[0].Key).IsEqualTo(5);
        await Assert.That(items[0].Value).IsEqualTo(50);
    }

    [Test]
    public async Task GetRange_ExclusiveSingleItemRange_ReturnsEmpty()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 5),
            new Option<int>(true, 5),
            false).ToArray();

        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task GetRange_BeyondTreeBounds_ReturnsAll()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 0),
            new Option<int>(true, 100),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_NoMinLimit_StartsFromBeginning()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            default,
            new Option<int>(true, 5),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(6); // 0 to 5 inclusive
        await Assert.That(items[0].Key).IsEqualTo(0);
    }

    [Test]
    public async Task GetRange_NoMaxLimit_GoesToEnd()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 5),
            default,
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5); // 5 to 9
        await Assert.That(items[^1].Key).IsEqualTo(9);
    }

    [Test]
    public async Task GetRange_NoLimits_ReturnsAll()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(default, default, true).ToArray();

        await Assert.That(items.Length).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_MinAtTreeMin_Works()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 10),
            new Option<int>(true, 15),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(6);
        await Assert.That(items[0].Key).IsEqualTo(10);
    }

    [Test]
    public async Task GetRange_MaxAtTreeMax_Works()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(i, i * 10);
        }

        var items = tree.GetRange(
            new Option<int>(true, 15),
            new Option<int>(true, 19),
            true).ToArray();

        await Assert.That(items.Length).IsEqualTo(5);
        await Assert.That(items[^1].Key).IsEqualTo(19);
    }

    [Test]
    public async Task GetNearest_ExactMatch_OnlyReturnsMatch()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i * 10, $"value{i * 10}");
        }

        var nearest = tree.GetNearest(50);

        await Assert.That(nearest.Match.HasValue).IsTrue();
        await Assert.That(nearest.Match.Value.Key).IsEqualTo(50);
        await Assert.That(nearest.Lower.HasValue).IsFalse();
        await Assert.That(nearest.Upper.HasValue).IsFalse();
    }

    [Test]
    public async Task GetNearest_AtMinBoundary_ReturnsOnlyUpper()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 10; i <= 20; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        var nearest = tree.GetNearest(5);

        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsFalse();
        await Assert.That(nearest.Upper.HasValue).IsTrue();
        await Assert.That(nearest.Upper.Value.Key).IsEqualTo(10);
    }

    [Test]
    public async Task GetNearest_AtMaxBoundary_ReturnsOnlyLower()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 10; i <= 20; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        var nearest = tree.GetNearest(25);

        await Assert.That(nearest.Match.HasValue).IsFalse();
        await Assert.That(nearest.Lower.HasValue).IsTrue();
        await Assert.That(nearest.Lower.Value.Key).IsEqualTo(20);
        await Assert.That(nearest.Upper.HasValue).IsFalse();
    }

    [Test]
    public async Task Insert_AtTreeMinimum_Works()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        tree.InsertOrUpdate(5, "value5");

        tree.GetMin(out var min);
        await Assert.That(min.Key).IsEqualTo(5);
        await Assert.That(min.Value).IsEqualTo("value5");
    }

    [Test]
    public async Task Insert_AtTreeMaximum_Works()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 10; i < 20; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        tree.InsertOrUpdate(25, "value25");

        tree.GetMax(out var max);
        await Assert.That(max.Key).IsEqualTo(25);
        await Assert.That(max.Value).IsEqualTo("value25");
    }

    [Test]
    public async Task Remove_NonExistent_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        bool result = tree.Remove(100, out _);
        await Assert.That(result).IsFalse();
        await Assert.That(tree.Count).IsEqualTo(10);
    }

    [Test]
    public async Task Contains_NonExistent_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        bool result = tree.Contains(100);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Get_NonExistent_ReturnsFalse()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        bool result = tree.Get(100, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DoForEach_CancelImmediately_ProcessesOneItem()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        int count = 0;
        bool canceled = tree.DoForEach((_, _) =>
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
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        int count = 0;
        tree.DoForEach(
            (_, _) =>
            {
                count++;
                return false;
            },
            new Option<int>(true, 100),
            new Option<int>(true, 200),
            true);

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Update_ExistingKey_ReturnsNewValue()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(42, "first");

        bool updated = tree.InsertOrUpdate(42, "second");
        await Assert.That(updated).IsTrue();

        tree.Get(42, out var value);
        await Assert.That(value).IsEqualTo("second");
    }

    [Test]
    public async Task StringKeys_Work()
    {
        BPlusTree<string, int> tree = new();
        tree.InsertOrUpdate("apple", 1);
        tree.InsertOrUpdate("banana", 2);
        tree.InsertOrUpdate("cherry", 3);

        await Assert.That(tree.Count).IsEqualTo(3);

        tree.GetMin(out var min);
        await Assert.That(min.Key).IsEqualTo("apple");

        tree.GetMax(out var max);
        await Assert.That(max.Key).IsEqualTo("cherry");
    }

    [Test]
    public async Task DoubleKeys_Work()
    {
        BPlusTree<double, string> tree = new();
        tree.InsertOrUpdate(1.5, "one-half");
        tree.InsertOrUpdate(2.5, "two-half");
        tree.InsertOrUpdate(0.5, "half");

        await Assert.That(tree.Count).IsEqualTo(3);

        tree.GetMin(out var min);
        await Assert.That(min.Key).IsEqualTo(0.5);

        tree.GetMax(out var max);
        await Assert.That(max.Key).IsEqualTo(2.5);
    }

    [Test]
    public async Task GuidKeys_Work()
    {
        BPlusTree<System.Guid, string> tree = new();
        var guid1 = System.Guid.NewGuid();
        var guid2 = System.Guid.NewGuid();
        var guid3 = System.Guid.NewGuid();

        tree.InsertOrUpdate(guid1, "value1");
        tree.InsertOrUpdate(guid2, "value2");
        tree.InsertOrUpdate(guid3, "value3");

        await Assert.That(tree.Count).IsEqualTo(3);
        await Assert.That(tree.Contains(guid1)).IsTrue();
        await Assert.That(tree.Contains(guid2)).IsTrue();
        await Assert.That(tree.Contains(guid3)).IsTrue();
    }
}
