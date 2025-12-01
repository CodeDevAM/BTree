using System;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace BTree.Test.BTree;

public class ExceptionTest
{
    [Test]
    public async Task InsertOrUpdate_NullItem_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.InsertOrUpdate(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Remove_NullKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.Remove<Ref<int>>(null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Get_NullKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.Get<Ref<int>>(null!, out _))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Contains_NullKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.Contains<Ref<int>>(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetNearest_NullKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.GetNearest<Ref<int>>(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetRange_NullMinKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.GetRange(
            new Option<Ref<int>>(true, null!),
            default,
            true).ToArray())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task GetRange_NullMaxKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.GetRange(
            default,
            new Option<Ref<int>>(true, null!),
            true).ToArray())
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DoForEach_NullMinKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.DoForEach(
            _ => false,
            new Option<Ref<int>>(true, null!),
            default,
            true))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task DoForEach_NullMaxKey_ThrowsArgumentNullException()
    {
        BTree<Ref<int>> tree = new();

        await Assert.That(() => tree.DoForEach(
            _ => false,
            default,
            new Option<Ref<int>>(true, null!),
            true))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task InsertDuringGetAll_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            foreach (var _ in tree.GetAll())
            {
                tree.InsertOrUpdate(new Ref<int>(100));
            }
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveDuringGetAll_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            foreach (var item in tree.GetAll())
            {
                tree.Remove(item, out _);
            }
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ClearDuringGetAll_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
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
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            tree.DoForEach<Ref<int>>(_ =>
            {
                tree.InsertOrUpdate(new Ref<int>(100));
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMinDuringDoForEach_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            tree.DoForEach<Ref<int>>(_ =>
            {
                tree.RemoveMin(out _);
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMaxDuringDoForEach_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            tree.DoForEach<Ref<int>>(_ =>
            {
                tree.RemoveMax(out _);
                return false;
            });
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task InsertDuringGetRange_ThrowsInvalidOperationException()
    {
        BTree<Ref<int>> tree = new();
        for (int i = 0; i < 10; i++)
        {
            tree.InsertOrUpdate(new Ref<int>(i));
        }

        await Assert.That(() =>
        {
            foreach (var _ in tree.GetRange<Ref<int>>(default, default, true))
            {
                tree.InsertOrUpdate(new Ref<int>(100));
            }
        }).Throws<InvalidOperationException>();
    }
}
