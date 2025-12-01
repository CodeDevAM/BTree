using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BPlusTree;

public class ExceptionTest
{
    [Test]
    public async Task InsertOrUpdate_NullKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.InsertOrUpdate(null!, 42))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Remove_NullKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.Remove(null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Get_NullKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.Get(null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Contains_NullKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.Contains(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetNearest_NullKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.GetNearest(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetRange_NullMinKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.GetRange(
            new Option<string>(true, null!),
            default,
            true).ToArray())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetRange_NullMaxKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.GetRange(
            default,
            new Option<string>(true, null!),
            true).ToArray())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DoForEach_NullMinKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.DoForEach(
            (_, _) => false,
            new Option<string>(true, null!),
            default,
            true))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DoForEach_NullMaxKey_ThrowsArgumentNullException()
    {
        BPlusTree<string, int> tree = new();

        await Assert.That(() => tree.DoForEach(
            (_, _) => false,
            default,
            new Option<string>(true, null!),
            true))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InsertDuringGetAll_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            foreach (var _ in tree.GetAll())
            {
                tree.InsertOrUpdate(100, 100);
            }
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveDuringGetAll_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            foreach (var item in tree.GetAll())
            {
                tree.Remove(item.Key, out _);
            }
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ClearDuringGetAll_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            foreach (var _ in tree.GetAll())
            {
                tree.Clear();
            }
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task InsertDuringDoForEach_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            tree.DoForEach((_, _) =>
            {
                tree.InsertOrUpdate(100, 100);
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMinDuringDoForEach_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            tree.DoForEach((_, _) =>
            {
                tree.RemoveMin(out _);
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMaxDuringDoForEach_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            tree.DoForEach((_, _) =>
            {
                tree.RemoveMax(out _);
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task InsertDuringGetRange_ThrowsInvalidOperationException()
    {
        BPlusTree<int, int> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(i, i);
        }

        await Assert.That(() =>
        {
            foreach (var _ in tree.GetRange(default, default, true))
            {
                tree.InsertOrUpdate(100, 100);
            }
        }).Throws<InvalidOperationException>();
    }
}
