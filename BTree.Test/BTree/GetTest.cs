using NUnit.Framework.Legacy;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BTree.Test.BTree;

public class GetTest
{
    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void Get(ushort degree, int count, int minItem, int maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BTree<Ref<int>> tree = new(degree);

        Ref<int> currentMinItem = null;
        Ref<int> currentMaxItem = null;

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);

            currentMinItem = currentMinItem == null || item < currentMinItem ? item : currentMinItem;
            bool getMinResult = tree.GetMin(out Ref<int> retrievedMinItem);

            Assert.That(getMinResult, Is.EqualTo(true));
            Assert.That(retrievedMinItem, Is.EqualTo(currentMinItem));

            currentMaxItem = currentMaxItem == null || item > currentMaxItem ? item : currentMaxItem;
            bool getMaxResult = tree.GetMax(out Ref<int> retrievedMaxItem);

            Assert.That(getMaxResult, Is.EqualTo(true));
            Assert.That(retrievedMaxItem, Is.EqualTo(currentMaxItem));
        }

        foreach (Ref<int> item in items)
        {
            bool getResult = tree.Get(item, out Ref<int> existingItem);

            Assert.That(getResult, Is.EqualTo(true));
            Assert.That(existingItem, Is.EqualTo(item));
        }

        Assert.That(tree.Count, Is.EqualTo(count));
    }

    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void GetRange(ushort degree, int count, int minItem, int maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BTree<Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);
        }

        Ref<int>[] expectedSequence = orderedItems;
        Ref<int>[] actualSequence = tree.GetAll().ToArray();
        CollectionAssert.AreEqual(expectedSequence, actualSequence);

        List<Ref<int>> actualList = [];
        tree.DoForEach(item =>
        {
            actualList.Add(item);
        });
        CollectionAssert.AreEqual(expectedSequence, actualList);

        // inclusive max
        Ref<int>[] expectedRangeSequence = orderedItems.Where(item => item >= minItem && item <= maxItem).ToArray();
        Ref<int>[] actualRangeSequence = tree.GetRange<Ref<int>>(minItem, maxItem, true).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeSequence);

        List<Ref<int>> actualRangeList = [];
        tree.DoForEach<Ref<int>>(actualRangeList.Add, minItem, maxItem, true);
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);

        actualRangeList = [];
        tree.DoForEach<Ref<int>>(item =>
        {
            actualRangeList.Add(item);
            return true;
        }, minItem, maxItem, true);

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);

        // exclusive max
        expectedRangeSequence = orderedItems.Where(item => item >= minItem && item < maxItem).ToArray();
        actualRangeSequence = tree.GetRange<Ref<int>>(minItem, maxItem, false).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeSequence);

        actualRangeList = [];
        tree.DoForEach<Ref<int>>(actualRangeList.Add, minItem, maxItem, false);
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);

        actualRangeList = [];
        tree.DoForEach<Ref<int>>(item =>
        {
            actualRangeList.Add(item);
            return true;
        }, minItem, maxItem, false);

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);
    }

    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void GetNearest(ushort degree, int count, int minItem, int maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(1, count).Select(x => new Ref<int>(x * 10)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BTree<Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);
        }

        foreach (Ref<int> item in items)
        {
            BTree<Ref<int>>.NearestItems nearestItems = tree.GetNearest(item);
            Assert.That(nearestItems.LowerHasValue, Is.EqualTo(false));
            Assert.That(nearestItems.UpperHasValue, Is.EqualTo(false));
            Assert.That(nearestItems.MatchHasValue, Is.EqualTo(true));
            Assert.That(nearestItems.MatchItem, Is.EqualTo(item));
        }

        Ref<int>[] keys = Enumerable.Range(0, 10 * (count + 1) + 1).Select(x => new Ref<int>(x)).ToArray();
        keys.Shuffle();

        foreach (Ref<int> key in keys)
        {
            BTree<Ref<int>>.NearestItems nearestItems = tree.GetNearest(key);

            if (key.CompareTo(10) < 0) // only upper
            {
                Assert.That(nearestItems.MatchHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.LowerHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.UpperHasValue, Is.EqualTo(true));
                Assert.That(nearestItems.UpperItem, Is.EqualTo(new Ref<int>(10)));
            }
            else if (key.CompareTo(count * 10) > 0) // only lower
            {
                Assert.That(nearestItems.MatchHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.LowerHasValue, Is.EqualTo(true));
                Assert.That(nearestItems.LowerItem, Is.EqualTo(new Ref<int>(count * 10)));
                Assert.That(nearestItems.UpperHasValue, Is.EqualTo(false));
            }
            else if (key.Value % 10 == 0) // match
            {
                Assert.That(nearestItems.LowerHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.UpperHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.MatchHasValue, Is.EqualTo(true));
                Assert.That(nearestItems.MatchItem, Is.EqualTo(key));
            }
            else // between
            {
                Ref<int> expectedMinItem = (key / 10) * 10;
                Ref<int> expectedMaxItem = ((key / 10) + 1) * 10;

                Assert.That(nearestItems.MatchHasValue, Is.EqualTo(false));
                Assert.That(nearestItems.LowerHasValue, Is.EqualTo(true));
                Assert.That(nearestItems.LowerItem, Is.EqualTo(expectedMinItem));
                Assert.That(nearestItems.UpperHasValue, Is.EqualTo(true));
                Assert.That(nearestItems.UpperItem, Is.EqualTo(expectedMaxItem));
            }
        }
    }

    public static IEnumerable TestCases
    {
        get
        {
            ushort[] degrees = [3, 4, 5, 6, 7, 8, 9];
            int[] counts = [3, 4, 5, 6, 7, 8, 9, 90, 900];

            foreach (ushort degree in degrees)
                foreach (int count in counts)
                {
                    int[] minKeys = [int.MinValue, int.MaxValue, 0, count, count / 2, count / 4];
                    int[] maxKeys = [int.MinValue, int.MaxValue, 0, count, count / 2, count / 4];

                    foreach (int minKey in minKeys)
                        foreach (int maxKey in maxKeys)
                        {
                            yield return new TestCaseData(degree, count, minKey, maxKey);
                        }
                }

        }
    }
}