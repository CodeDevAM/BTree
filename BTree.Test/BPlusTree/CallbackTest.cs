using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public struct CountCallback : ICallback<int, string>
{
    public int Count;

    public bool Invoke(int key, string item)
    {
        Count++;
        return false;
    }
}

public struct SumKeysCallback : ICallback<int, string>
{
    public int Sum;

    public bool Invoke(int key, string item)
    {
        Sum += key;
        return false;
    }
}

public struct CancelAfterNCallback : ICallback<int, string>
{
    public int MaxCount;
    public int Count;

    public bool Invoke(int key, string item)
    {
        Count++;
        return Count >= MaxCount;
    }
}

public struct CollectCallback : ICallback<int, string>
{
    public List<KeyValuePair<int, string>> Items;

    public bool Invoke(int key, string item)
    {
        Items ??= [];
        Items.Add(new KeyValuePair<int, string>(key, item));
        return false;
    }
}

public class CallbackTest
{
    [Test]
    public async Task StructCallback_CountsAllItems()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        tree.DoForEach<CountCallback>(ref callback);

        await Assert.That(callback.Count).IsEqualTo(100);
    }

    [Test]
    public async Task StructCallback_SumsAllKeys()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 1; i <= 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        SumKeysCallback callback = new();
        tree.DoForEach<SumKeysCallback>(ref callback);

        await Assert.That(callback.Sum).IsEqualTo(55); // 1+2+...+10 = 55
    }

    [Test]
    public async Task StructCallback_CancelsEarly()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CancelAfterNCallback callback = new() { MaxCount = 10 };
        bool canceled = tree.DoForEach<CancelAfterNCallback>(ref callback);

        await Assert.That(canceled).IsTrue();
        await Assert.That(callback.Count).IsEqualTo(10);
    }

    [Test]
    public async Task StructCallback_WithRange()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        Option<int> min = new(true, 25);
        Option<int> max = new(true, 75);

        tree.DoForEach<CountCallback>(ref callback, min, max, true);

        await Assert.That(callback.Count).IsEqualTo(51); // 25 to 75 inclusive
    }

    [Test]
    public async Task StructCallback_ExclusiveMax()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        Option<int> min = new(true, 25);
        Option<int> max = new(true, 75);

        tree.DoForEach<CountCallback>(ref callback, min, max, false);

        await Assert.That(callback.Count).IsEqualTo(50); // 25 to 74 (exclusive 75)
    }

    [Test]
    public async Task StructCallback_CollectsItems()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(3, "three");
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");

        CollectCallback callback = new() { Items = [] };
        tree.DoForEach<CollectCallback>(ref callback);

        await Assert.That(callback.Items.Count).IsEqualTo(3);
        await Assert.That(callback.Items[0].Key).IsEqualTo(1);
        await Assert.That(callback.Items[0].Value).IsEqualTo("one");
        await Assert.That(callback.Items[1].Key).IsEqualTo(2);
        await Assert.That(callback.Items[2].Key).IsEqualTo(3);
    }

    [Test]
    public async Task StructCallback_EmptyTree()
    {
        BPlusTree<int, string> tree = new();

        CountCallback callback = new();
        tree.DoForEach<CountCallback>(ref callback);

        await Assert.That(callback.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StructCallback_SingleItem()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(42, "answer");

        SumKeysCallback callback = new();
        tree.DoForEach<SumKeysCallback>(ref callback);

        await Assert.That(callback.Sum).IsEqualTo(42);
    }

    [Test]
    public async Task StructCallback_MinGreaterThanMax_DoesNothing()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 100; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        Option<int> min = new(true, 75);
        Option<int> max = new(true, 25);

        bool canceled = tree.DoForEach<CountCallback>(ref callback, min, max, true);

        await Assert.That(canceled).IsFalse();
        await Assert.That(callback.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StructCallback_OnlyMin()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        Option<int> min = new(true, 5);

        tree.DoForEach<CountCallback>(ref callback, min, default, true);

        await Assert.That(callback.Count).IsEqualTo(5); // 5, 6, 7, 8, 9
    }

    [Test]
    public async Task StructCallback_OnlyMax()
    {
        BPlusTree<int, string> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, $"value{i}");
        }

        CountCallback callback = new();
        Option<int> max = new(true, 5);

        tree.DoForEach<CountCallback>(ref callback, default, max, true);

        await Assert.That(callback.Count).IsEqualTo(6); // 0, 1, 2, 3, 4, 5
    }
}
