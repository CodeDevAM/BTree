﻿using BTree.Test.BTree.Spatial;
using NUnit.Framework.Legacy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BTree.Test.BPlusTree.Spatial;

public class GetTest
{
    [TestCaseSource(typeof(GetTest), nameof(TestCases))]
    public void GetRange(bool zOrder, int count, Pattern xPattern, Pattern yPattern, int seed, double? lowerLimitShareX, double? lowerLimitShareY, double? upperLimitShareX, double? upperLimitShareY)
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

        Point minPoint = points[0];
        Point maxPoint = points[0];

        for (int i = 0; i < points.Length; i++)
        {
            Point point = points[i];
            bool updated = tree.InsertOrUpdate(point, false);

            minPoint = point.X < minPoint.X ? minPoint with { X = point.X } : minPoint;
            minPoint = point.Y < minPoint.Y ? minPoint with { Y = point.Y } : minPoint;
            maxPoint = point.X > maxPoint.X ? maxPoint with { X = point.X } : maxPoint;
            maxPoint = point.Y > maxPoint.Y ? maxPoint with { Y = point.Y } : maxPoint;
        }

        Option<Point> lowerLimit = lowerLimitShareX.HasValue && lowerLimitShareY.HasValue ?
            new Option<Point>(true, new(minPoint.X + lowerLimitShareX.Value * (maxPoint.X - minPoint.X), minPoint.Y + lowerLimitShareY.Value * (maxPoint.Y - minPoint.Y), zOrder)) :
            default;
        Option<Point> upperLimit = upperLimitShareX.HasValue && upperLimitShareY.HasValue ?
            new Option<Point>(true, new(minPoint.X + upperLimitShareX.Value * (maxPoint.X - minPoint.X), minPoint.Y + upperLimitShareY.Value * (maxPoint.Y - minPoint.Y), zOrder)) :
            default;

        List<Point> expectedRange = points
            .Where(point => (!lowerLimit.HasValue || (point.X >= lowerLimit.Value.X && point.Y >= lowerLimit.Value.Y))
                && (!upperLimit.HasValue || (point.X <= upperLimit.Value.X && point.Y <= upperLimit.Value.Y)))
            .ToHashSet().ToList();

        List<Point> actualRange = [];
        tree.DoForEach((point, _) =>
        {
            if ((!lowerLimit.HasValue || (point.X >= lowerLimit.Value.X && point.Y >= lowerLimit.Value.Y))
                && (!upperLimit.HasValue || (point.X <= upperLimit.Value.X && point.Y <= upperLimit.Value.Y)))
            {
                actualRange.Add(point);
            }
            return false;
        }, lowerLimit, upperLimit, true);

        expectedRange.Sort((left, right) => left.X < right.X ? -1 : left.X > right.X ? 1 : left.Y < right.Y ? -1 : left.Y > right.Y ? 1 : 0);
        actualRange.Sort((left, right) => left.X < right.X ? -1 : left.X > right.X ? 1 : left.Y < right.Y ? -1 : left.Y > right.Y ? 1 : 0);

        CollectionAssert.AreEqual(expectedRange, actualRange);
    }

    public static IEnumerable TestCases
    {
        get
        {
            bool[] zOrders = [false, true];
            int[] counts = [10, 500];
            Pattern[] patterns = [
                Pattern.Random,
                Pattern.Const,
            ];
            int[] seeds = [0, 1];
            double?[] limitShares = [null, -1.0, 0.0, 0.5, 1.0, 2.0];

            foreach (bool zOrder in zOrders)
            {
                foreach (int count in counts)
                {
                    foreach (Pattern xPattern in patterns)
                    {
                        foreach (Pattern yPattern in patterns)
                        {
                            for (int i = 0; i < seeds.Length; i++)
                            {
                                if (xPattern != Pattern.Random && yPattern != Pattern.Random && i > 0)
                                {
                                    continue;
                                }
                                foreach (double? lowerLimitShareX in limitShares)
                                {
                                    foreach (double? lowerLimitShareY in limitShares)
                                    {
                                        if (lowerLimitShareY is null)
                                        {
                                            continue;
                                        }

                                        foreach (double? upperLimitShareX in limitShares)
                                        {
                                            foreach (double? upperLimitShareY in limitShares)
                                            {
                                                if (upperLimitShareY is null)
                                                {
                                                    continue;
                                                }

                                                yield return new TestCaseData(zOrder, count, xPattern, yPattern, seeds[i], lowerLimitShareX, lowerLimitShareY, upperLimitShareX, upperLimitShareY);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}