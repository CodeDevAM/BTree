using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BTree.Test.BPlusTree;

public class RemoveTest
{
    [TestCaseSource(typeof(RemoveTest), nameof(TestCases))]
    public void Remove(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);
        }

        Assert.That(tree.Count, Is.EqualTo(count));

        int currentExpectedCount = count;

        foreach (Ref<int> item in removeItems)
        {
            bool removeResult = tree.Remove(item, out Ref<int> existingItem);

            Assert.That(removeResult, Is.EqualTo(true));

            Assert.That(existingItem, Is.EqualTo(item));

            currentExpectedCount--;
            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));

            tree.InsertOrUpdate(item, item);

            bool getResult = tree.Get(item, out Ref<int> existingValue);

            Assert.That(getResult, Is.EqualTo(true));

            Assert.That(existingValue, Is.EqualTo(item));

            removeResult = tree.Remove(item, out existingItem);

            Assert.That(removeResult, Is.EqualTo(true));

            Assert.That(existingItem, Is.EqualTo(item));

            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));

            getResult = tree.Get(item, out _);

            Assert.That(getResult, Is.EqualTo(false));
        }
    }

    [TestCaseSource(typeof(RemoveTest), nameof(TestCases))]
    public void RemoveMax(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);

            currentExpectedCount++;

            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));
        }

        Assert.That(tree.Count, Is.EqualTo(count));

        currentExpectedCount = count;

        while (tree.Count > 0)
        {
            currentExpectedCount--;

            bool removeResult = tree.RemoveMax(out KeyValuePair<Ref<int>, Ref<int>> max);
            Assert.That(removeResult, Is.EqualTo(true));
            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));
            Assert.That(max.Key, Is.EqualTo(new Ref<int>(currentExpectedCount)));
            Assert.That(max.Value, Is.EqualTo(new Ref<int>(currentExpectedCount)));

            bool getResult = tree.Get(max.Key, out Ref<int> _);

            Assert.That(getResult, Is.EqualTo(false));
        }
    }

    [TestCaseSource(typeof(RemoveTest), nameof(TestCases))]
    public void RemoveMin(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);

            currentExpectedCount++;

            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));
        }

        Assert.That(tree.Count, Is.EqualTo(count));

        int currentMin = 0;
        currentExpectedCount = count;

        while (tree.Count > 0)
        {
            currentExpectedCount--;

            bool removeResult = tree.RemoveMin(out KeyValuePair<Ref<int>, Ref<int>> min);
            Assert.That(removeResult, Is.EqualTo(true));
            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));
            Assert.That(min.Key, Is.EqualTo(new Ref<int>(currentMin)));
            Assert.That(min.Value, Is.EqualTo(new Ref<int>(currentMin)));

            bool getResult = tree.Get(min.Key, out Ref<int> _);

            Assert.That(getResult, Is.EqualTo(false));

            currentMin++;
        }
    }

    public static IEnumerable TestCases
    {
        get
        {
            ushort[] degrees = [3, 4, 5, 6, 7, 8, 9];
            int[] counts = [3, 4, 5, 6, 7, 8, 9, 90, 900];
            bool[] reverseOrders = [false, true];
            bool[] randomOrders = [false, true];

            foreach (ushort degree in degrees)
                foreach (int count in counts)
                    foreach (bool reverseOrder in reverseOrders)
                        foreach (bool randomOrder in randomOrders)
                        {
                            yield return new TestCaseData(degree, count, reverseOrder, randomOrder);
                        }
        }
    }
}