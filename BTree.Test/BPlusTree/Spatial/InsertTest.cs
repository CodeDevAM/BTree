using BTree.Test.BTree.Spatial;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BTree.Test.BPlusTree.Spatial;

public class InsertTest
{
    [TestCaseSource(typeof(InsertTest), nameof(TestCases))]
    public void Insert(bool zOrder, int count, Pattern xPattern, Pattern yPattern, int seed)
    {
        Random random = new(seed);

        Point[] points = Enumerable.Range(0, count).Select(value =>
        {
            double x = Point.GenerateValue(xPattern, value, count, random);
            double y = Point.GenerateValue(yPattern, value, count, random);
            Point point = new(x, y, zOrder);
            return point;
        }).ToArray();

        BPlusTree<Point, bool> tree = new();

        HashSet<Point> seenPoints = [];

        for (int i = 0; i < points.Length; i++)
        {
            Point point = points[i];
            if (i == points.Length - 1)
            {
                // Placeholder for a breakpoint and debugging
            }

            bool updated = tree.InsertOrUpdate(point, false);
            bool seenPointsAdded = seenPoints.Add(point);

            Assert.That(updated, Is.EqualTo(!seenPointsAdded));

            Assert.That(tree.Count, Is.EqualTo(seenPoints.Count));

            bool found = tree.Contains(point);
            if (found == false)
            {
                // Placeholder for a breakpoint and debugging
                found = tree.Contains(point);
            }
            Assert.That(found, Is.EqualTo(true));
        }

        foreach (Point point in points)
        {
            bool found = tree.Contains(point);
            if (found == false)
            {
                // Placeholder for a breakpoint and debugging
                found = tree.Contains(point);
            }
            Assert.That(found, Is.EqualTo(true));
        }
    }

    public static IEnumerable TestCases
    {
        get
        {
            bool[] zOrders = [false, true];
            int[] counts = [2, 5, 10, 20, 50, 100, 200, 500];
            Pattern[] patterns = [
                Pattern.Increasing,
                Pattern.LowerHalfIncreasing,
                Pattern.UpperHalfIncreasing,
                Pattern.Decreasing,
                Pattern.LowerHalfDecreasing,
                Pattern.UpperHalfDecreasing,
                Pattern.Random,
                Pattern.Const,
                Pattern.Alternating,
                Pattern.ReverseAlternating,
            ];
            int[] seeds = [0, 1, 2];

            foreach (bool zOrder in zOrders)
            {
                foreach (int count in counts)
                {
                    foreach (Pattern xPattern in patterns)
                    {
                        foreach (Pattern yPattern in patterns)
                        {
                            if (xPattern == Pattern.Random || yPattern == Pattern.Random)
                            {
                                foreach (int seed in seeds)
                                {
                                    yield return new TestCaseData(zOrder, count, xPattern, yPattern, seed);
                                }
                            }
                            else
                            {
                                yield return new TestCaseData(zOrder, count, xPattern, yPattern, -1);
                            }

                        }
                    }
                }
            }

        }
    }
}