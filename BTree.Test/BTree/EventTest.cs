using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

public class EventTest
{
    [Test]
    public async Task CountChanged_FiresOnInsert()
    {
        BTree<Ref<int>> tree = new();
        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.InsertOrUpdate(new Ref<int>(1));
        await Assert.That(lastCount).IsEqualTo(1);

        tree.InsertOrUpdate(new Ref<int>(2));
        await Assert.That(lastCount).IsEqualTo(2);
    }

    [Test]
    public async Task CountChanged_FiresOnRemove()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.Remove(new Ref<int>(1), out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnRemoveMin()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.RemoveMin(out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnRemoveMax()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.RemoveMax(out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnClear()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));
        tree.InsertOrUpdate(new Ref<int>(2));

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.Clear();
        await Assert.That(lastCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnUpdate()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.InsertOrUpdate(new Ref<int>(1)); // Update existing
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnFailedRemove()
    {
        BTree<Ref<int>> tree = new();
        tree.InsertOrUpdate(new Ref<int>(1));

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.Remove(new Ref<int>(999), out _); // Non-existent key
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnEmptyTreeRemoveMin()
    {
        BTree<Ref<int>> tree = new();

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.RemoveMin(out _);
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnEmptyTreeRemoveMax()
    {
        BTree<Ref<int>> tree = new();

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.RemoveMax(out _);
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_SenderIsTree()
    {
        BTree<Ref<int>> tree = new();
        object capturedSender = null;
        tree.CountChanged += (sender, count) => capturedSender = sender;

        tree.InsertOrUpdate(new Ref<int>(1));
        await Assert.That(capturedSender).IsSameReferenceAs(tree);
    }

    [Test]
    public async Task CountChanged_ExceptionInHandler_DoesNotPreventOperation()
    {
        BTree<Ref<int>> tree = new();
        tree.CountChanged += (sender, count) => throw new InvalidOperationException("Test exception");

        // Should not throw
        tree.InsertOrUpdate(new Ref<int>(1));
        await Assert.That(tree.Count).IsEqualTo(1);

        tree.InsertOrUpdate(new Ref<int>(2));
        await Assert.That(tree.Count).IsEqualTo(2);

        tree.Remove(new Ref<int>(1), out _);
        await Assert.That(tree.Count).IsEqualTo(1);
    }
}
