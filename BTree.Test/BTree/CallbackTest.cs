using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

internal struct CountCallback : ICallback<Ref<int>>
{
    public int Count;

    public bool Invoke(Ref<int> item)
    {
        Count++;
        return false;
    }
}

internal struct SumCallback : ICallback<Ref<int>>
{
    public int Sum;

    public bool Invoke(Ref<int> item)
    {
        Sum += item.Value;
        return false;
    }
}

internal struct CancelAfterNCallback : ICallback<Ref<int>>
{
    public int MaxCount;
    public int Count;

    public bool Invoke(Ref<int> item)
    {
        Count++;
        return Count >= MaxCount;
    }
}

internal struct CollectCallback : ICallback<Ref<int>>
{
    public List<int> Items;

    public bool Invoke(Ref<int> item)
    {
        Items ??= [];
        Items.Add(item.Value);
        return false;
    }
}

public class CallbackTest
{
    [Test]
    public async Task StructCallback_CountsAllItems()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        tree.DoForEach<Ref<int>, CountCallback>(ref callback);

        await Assert.That(callback.Count).IsEqualTo(100);
    }

    [Test]
    public async Task StructCallback_SumsAllItems()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 1; i <= 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        SumCallback callback = new();
        tree.DoForEach<Ref<int>, SumCallback>(ref callback);

        await Assert.That(callback.Sum).IsEqualTo(55); // 1+2+...+10 = 55
    }

    [Test]
    public async Task StructCallback_CancelsEarly()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CancelAfterNCallback callback = new() { MaxCount = 10 };
        bool canceled = tree.DoForEach<Ref<int>, CancelAfterNCallback>(ref callback);

        await Assert.That(canceled).IsTrue();
        await Assert.That(callback.Count).IsEqualTo(10);
    }

    [Test]
    public async Task StructCallback_WithRange()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        Option<Ref<int>> min = new(true, new Ref<int>(25));
        Option<Ref<int>> max = new(true, new Ref<int>(75));

        tree.DoForEach<Ref<int>, CountCallback>(ref callback, min, max, true);

        await Assert.That(callback.Count).IsEqualTo(51); // 25 to 75 inclusive
    }

    [Test]
    public async Task StructCallback_ExclusiveMax()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        Option<Ref<int>> min = new(true, new Ref<int>(25));
        Option<Ref<int>> max = new(true, new Ref<int>(75));

        tree.DoForEach<Ref<int>, CountCallback>(ref callback, min, max, false);

        await Assert.That(callback.Count).IsEqualTo(50); // 25 to 74 (exclusive 75)
    }

    [Test]
    public async Task StructCallback_CollectsItems()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(3));
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));

        CollectCallback callback = new() { Items = [] };
        tree.DoForEach<Ref<int>, CollectCallback>(ref callback);

        await Assert.That(callback.Items).IsEquivalentTo([1, 2, 3]);
    }

    [Test]
    public async Task StructCallback_EmptyTree()
    {
        BTree<Ref<int>> tree = new();

        CountCallback callback = new();
        tree.DoForEach<Ref<int>, CountCallback>(ref callback);

        await Assert.That(callback.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StructCallback_SingleItem()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(42));

        SumCallback callback = new();
        tree.DoForEach<Ref<int>, SumCallback>(ref callback);

        await Assert.That(callback.Sum).IsEqualTo(42);
    }

    [Test]
    public async Task StructCallback_MinGreaterThanMax_DoesNothing()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        Option<Ref<int>> min = new(true, new Ref<int>(75));
        Option<Ref<int>> max = new(true, new Ref<int>(25));

        bool canceled = tree.DoForEach<Ref<int>, CountCallback>(ref callback, min, max, true);

        await Assert.That(canceled).IsFalse();
        await Assert.That(callback.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StructCallback_OnlyMin()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        Option<Ref<int>> min = new(true, new Ref<int>(5));

        tree.DoForEach<Ref<int>, CountCallback>(ref callback, min, default, true);

        await Assert.That(callback.Count).IsEqualTo(5); // 5, 6, 7, 8, 9
    }

    [Test]
    public async Task StructCallback_OnlyMax()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        CountCallback callback = new();
        Option<Ref<int>> max = new(true, new Ref<int>(5));

        tree.DoForEach<Ref<int>, CountCallback>(ref callback, default, max, true);

        await Assert.That(callback.Count).IsEqualTo(6); // 0, 1, 2, 3, 4, 5
    }
}
