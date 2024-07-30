using System.Collections;
using System.Linq;

namespace BTree.Test.BPlusTree;

public class InsertTest
{
    [TestCaseSource(typeof(InsertTest), nameof(TestCases))]
    public void Insert(ushort degree, int count, bool reverseOrder, bool randomOrder, bool withUpdate)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));

        foreach (Ref<int> item in items)
        {
            bool updated = tree.InsertOrUpdate(item, item);

            Assert.That(updated, Is.EqualTo(false));

            currentExpectedCount++;

            Assert.That(tree.Count, Is.EqualTo(currentExpectedCount));
        }

        Assert.That(tree.Count, Is.EqualTo(count));

        items.Shuffle();

        foreach (Ref<int> item in items)
        {
            bool getResult = tree.Contains(item);

            Assert.That(getResult, Is.EqualTo(true));
        }

        if (withUpdate)
        {
            items.Shuffle();

            foreach (Ref<int> item in items)
            {
                bool updated = tree.InsertOrUpdate(item, item);

                Assert.That(updated, Is.EqualTo(true));

                Assert.That(tree.Count, Is.EqualTo(count));
            }

            Assert.That(tree.Count, Is.EqualTo(count));

            items.Shuffle();

            foreach (Ref<int> item in items)
            {
                bool getResult = tree.Contains(item);

                Assert.That(getResult, Is.EqualTo(true));
            }
        }
    }

    public static IEnumerable TestCases
    {
        get
        {
            ushort[] degrees = [3, 4, 5, 6];
            int[] counts = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 90, 900];
            bool[] reverseOrders = [false, true];
            bool[] randomOrders = [false, true];
            bool[] withUpdates = [false, true];

            foreach (ushort degree in degrees)
            {
                foreach (int count in counts)
                {
                    foreach (bool reverseOrder in reverseOrders)
                    {
                        foreach (bool randomOrder in randomOrders)
                        {
                            foreach (bool withUpdate in withUpdates)
                            {
                                yield return new TestCaseData(degree, count, reverseOrder, randomOrder, withUpdate);
                            }
                        }
                    }
                }
            }
        }
    }
}