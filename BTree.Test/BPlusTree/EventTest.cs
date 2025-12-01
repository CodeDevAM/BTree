using System;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public class EventTest
{
    [Test]
    public async Task CountChanged_FiresOnInsert()
    {
        BPlusTree<int, string> tree = new();
        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.InsertOrUpdate(1, "one");
        await Assert.That(lastCount).IsEqualTo(1);

        tree.InsertOrUpdate(2, "two");
        await Assert.That(lastCount).IsEqualTo(2);
    }

    [Test]
    public async Task CountChanged_FiresOnRemove()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.Remove(1, out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnRemoveMin()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.RemoveMin(out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnRemoveMax()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.RemoveMax(out _);
        await Assert.That(lastCount).IsEqualTo(1);
    }

    [Test]
    public async Task CountChanged_FiresOnClear()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");
        tree.InsertOrUpdate(2, "two");

        long lastCount = -1;
        tree.CountChanged += (sender, count) => lastCount = count;

        tree.Clear();
        await Assert.That(lastCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnUpdate()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.InsertOrUpdate(1, "updated"); // Update existing
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnFailedRemove()
    {
        BPlusTree<int, string> tree = new();
        tree.InsertOrUpdate(1, "one");

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.Remove(999, out _); // Non-existent key
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnEmptyTreeRemoveMin()
    {
        BPlusTree<int, string> tree = new();

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.RemoveMin(out _);
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_DoesNotFireOnEmptyTreeRemoveMax()
    {
        BPlusTree<int, string> tree = new();

        int eventCount = 0;
        tree.CountChanged += (sender, count) => eventCount++;

        tree.RemoveMax(out _);
        await Assert.That(eventCount).IsEqualTo(0);
    }

    [Test]
    public async Task CountChanged_SenderIsTree()
    {
        BPlusTree<int, string> tree = new();
        object capturedSender = null;
        tree.CountChanged += (sender, count) => capturedSender = sender;

        tree.InsertOrUpdate(1, "one");
        await Assert.That(capturedSender).IsSameReferenceAs(tree);
    }

    [Test]
    public async Task CountChanged_ExceptionInHandler_DoesNotPreventOperation()
    {
        BPlusTree<int, string> tree = new();
        tree.CountChanged += (sender, count) => throw new InvalidOperationException("Test exception");

        // Should not throw
        tree.InsertOrUpdate(1, "one");
        await Assert.That(tree.Count).IsEqualTo(1);

        tree.InsertOrUpdate(2, "two");
        await Assert.That(tree.Count).IsEqualTo(2);

        tree.Remove(1, out _);
        await Assert.That(tree.Count).IsEqualTo(1);
    }
}
