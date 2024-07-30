using NUnit.Framework.Legacy;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BTree.Test.BPlusTree;

public class GetTest
{
    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void Get(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        Ref<int> currentMinItem = null;
        Ref<int> currentMaxItem = null;

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);

            currentMinItem = currentMinItem == null || item < currentMinItem ? item : currentMinItem;
            bool getMinResult = tree.GetMin(out KeyValuePair<Ref<int>, Ref<int>> retrievedMinItem);

            Assert.That(getMinResult, Is.EqualTo(true));
            Assert.That(retrievedMinItem.Key, Is.EqualTo(currentMinItem));
            Assert.That(retrievedMinItem.Value, Is.EqualTo(currentMinItem));

            currentMaxItem = currentMaxItem == null || item > currentMaxItem ? item : currentMaxItem;
            bool getMaxResult = tree.GetMax(out KeyValuePair<Ref<int>, Ref<int>> retrievedMaxItem);

            Assert.That(getMaxResult, Is.EqualTo(true));
            Assert.That(retrievedMaxItem.Key, Is.EqualTo(currentMaxItem));
            Assert.That(retrievedMaxItem.Value, Is.EqualTo(currentMaxItem));
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
    public void GetRange(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);
        }

        Option<Ref<int>> lowerLimit = minItem.HasValue ? new(true, new Ref<int>(minItem.Value)) : default;
        Option<Ref<int>> upperLimit = maxItem.HasValue ? new(true, new Ref<int>(maxItem.Value)) : default;

        KeyValuePair<Ref<int>, Ref<int>>[] expectedSequence = orderedItems.Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x)).ToArray();
        KeyValuePair<Ref<int>, Ref<int>>[] actualSequence = tree.GetAll().ToArray();
        CollectionAssert.AreEqual(expectedSequence, actualSequence);

        List<KeyValuePair<Ref<int>, Ref<int>>> actualList = [];
        bool canceled = tree.DoForEach((key, item) =>
        {
            actualList.Add(new KeyValuePair<Ref<int>, Ref<int>>(key, item));
            return false;
        });
        CollectionAssert.AreEqual(expectedSequence, actualList);
        Assert.That(canceled, Is.EqualTo(false));

        // inclusive max
        KeyValuePair<Ref<int>, Ref<int>>[] expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item <= maxItem.Value))
            .Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x))
            .ToArray();
        KeyValuePair<Ref<int>, Ref<int>>[] actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, true).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeSequence);

        List<KeyValuePair<Ref<int>, Ref<int>>> actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return false;
        }, lowerLimit, upperLimit, true);
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);
        Assert.That(canceled, Is.EqualTo(false));

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new KeyValuePair<Ref<int>, Ref<int>>(key, item));
            return true;
        }, lowerLimit, upperLimit, true);

        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);
        Assert.That(canceled, Is.EqualTo(expectedRangeSequence.Length > 0 ? true : false));

        // exclusive max
        expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item < maxItem.Value))
            .Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x))
            .ToArray();
        actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, false).ToArray();
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeSequence);

        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return false;
        }, lowerLimit, upperLimit, false);
        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);
        Assert.That(canceled, Is.EqualTo(false));

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return true;
        }, lowerLimit, upperLimit, false);

        CollectionAssert.AreEqual(expectedRangeSequence, actualRangeList);
        Assert.That(canceled, Is.EqualTo(expectedRangeSequence.Length > 0 ? true : false));
    }

    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void GetNearest(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(1, count).Select(x => new Ref<int>(x * 10)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);
        }

        foreach (Ref<int> item in items)
        {
            BPlusTree<Ref<int>, Ref<int>>.NearestItems nearestItems = tree.GetNearest(item);
            Assert.That(nearestItems.Lower.HasValue, Is.EqualTo(false));
            Assert.That(nearestItems.Upper.HasValue, Is.EqualTo(false));
            Assert.That(nearestItems.Match.HasValue, Is.EqualTo(true));
            Assert.That(nearestItems.Match.Value.Key, Is.EqualTo(item));
            Assert.That(nearestItems.Match.Value.Value, Is.EqualTo(item));
        }

        Ref<int>[] keys = Enumerable.Range(0, 10 * (count + 1) + 1).Select(x => new Ref<int>(x)).ToArray();
        keys.Shuffle();

        foreach (Ref<int> key in keys)
        {
            BPlusTree<Ref<int>, Ref<int>>.NearestItems nearestItems = tree.GetNearest(key);

            if (key.CompareTo(10) < 0) // only upper
            {
                Assert.That(nearestItems.Match.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Lower.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Upper.HasValue, Is.EqualTo(true));
                Assert.That(nearestItems.Upper.Value.Key, Is.EqualTo(new Ref<int>(10)));
                Assert.That(nearestItems.Upper.Value.Value, Is.EqualTo(new Ref<int>(10)));
            }
            else if (key.CompareTo(count * 10) > 0) // only lower
            {
                Assert.That(nearestItems.Match.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Lower.HasValue, Is.EqualTo(true));
                Assert.That(nearestItems.Lower.Value.Key, Is.EqualTo(new Ref<int>(count * 10)));
                Assert.That(nearestItems.Lower.Value.Value, Is.EqualTo(new Ref<int>(count * 10)));
                Assert.That(nearestItems.Upper.HasValue, Is.EqualTo(false));
            }
            else if (key.Value % 10 == 0) // match
            {
                Assert.That(nearestItems.Lower.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Upper.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Match.HasValue, Is.EqualTo(true));
                Assert.That(nearestItems.Match.Value.Key, Is.EqualTo(key));
                Assert.That(nearestItems.Match.Value.Value, Is.EqualTo(key));
            }
            else // between
            {
                Ref<int> expectedMinItem = (key / 10) * 10;
                Ref<int> expectedMaxItem = ((key / 10) + 1) * 10;

                Assert.That(nearestItems.Match.HasValue, Is.EqualTo(false));
                Assert.That(nearestItems.Lower.HasValue, Is.EqualTo(true));
                Assert.That(nearestItems.Lower.Value.Key, Is.EqualTo(expectedMinItem));
                Assert.That(nearestItems.Lower.Value.Value, Is.EqualTo(expectedMinItem));
                Assert.That(nearestItems.Upper.HasValue, Is.EqualTo(true));
                Assert.That(nearestItems.Upper.Value.Key, Is.EqualTo(expectedMaxItem));
                Assert.That(nearestItems.Upper.Value.Value, Is.EqualTo(expectedMaxItem));
            }

        }
    }

    public static IEnumerable TestCases
    {
        get
        {
            ushort[] degrees = [3, 4, 5, 6];
            int[] counts = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 90, 900];

            foreach (ushort degree in degrees)
            {
                foreach (int count in counts)
                {
                    int?[] minKeys = [null, 0, count, count / 2, count / 4];
                    int?[] maxKeys = [null, 0, count, count / 2, count / 4];

                    foreach (int? minKey in minKeys)
                    {
                        foreach (int? maxKey in maxKeys)
                        {
                            yield return new TestCaseData(degree, count, minKey, maxKey);
                        }
                    }
                }
            }
        }
    }
}