using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BTree.Test.BTree.Spatial;

public record struct Point(double X, double Y, bool ZOrder) : IComparable<Point>
{
    public static ulong EncodeAsUInt64Orderable(double value)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("NaN is not allowed");
        }

        value = value == -0.0 ? 0.0 : value;

        ulong result = BitConverter.DoubleToUInt64Bits(value);

        result = value >= 0.0 ? result ^ 0x8000_0000_0000_0000UL : result ^ 0xFFFF_FFFF_FFFF_FFFFUL;

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Point other)
    {
        int result = 0;
        if (ZOrder)
        {
            result = CompareToRegular(other);
        }
        else
        {
            result = CompareToInZOrder(other);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareToRegular(Point other)
    {
        ulong left = EncodeAsUInt64Orderable(X);
        ulong right = EncodeAsUInt64Orderable(other.X);

        int result = left < right ? -1 : left > right ? 1 : 0;

        if (result == 0)
        {
            left = EncodeAsUInt64Orderable(Y);
            right = EncodeAsUInt64Orderable(other.Y);

            result = left < right ? -1 : left > right ? 1 : 0;
        }

        return result;
    }

    internal static readonly ulong[] Bitmasks = Enumerable.Range(0, 64).Select(x => 1UL << x).ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareToInZOrder(Point other)
    {
        ulong leftX = EncodeAsUInt64Orderable(X);
        ulong rightX = EncodeAsUInt64Orderable(other.X);

        ulong leftY = EncodeAsUInt64Orderable(Y);
        ulong rightY = EncodeAsUInt64Orderable(other.Y);

        for (int i = 63; i >= 0; i--)
        {
            ulong bitmask = Bitmasks[i];

            int comparisonResult = (leftX & bitmask).CompareTo(rightX & bitmask);

            if (comparisonResult != 0)
            {
                return comparisonResult;
            }

            comparisonResult = (leftY & bitmask).CompareTo(rightY & bitmask);

            if (comparisonResult != 0)
            {
                return comparisonResult;
            }
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GenerateValue(Pattern pattern, int value, int count, Random random)
    {
        int half = count / 2;
        double result = pattern switch
        {
            Pattern.Increasing => value,
            Pattern.LowerHalfIncreasing => value < half ? value : half,
            Pattern.UpperHalfIncreasing => value > half ? value : half,
            Pattern.Decreasing => -value,
            Pattern.UpperHalfDecreasing => value > half ? -value : -half,
            Pattern.LowerHalfDecreasing => value < half ? -value : -half,
            Pattern.Random => 10.0 * Math.Round(random.NextDouble(), 4),
            Pattern.Alternating => value % 2 == 0 ? value : -value,
            Pattern.ReverseAlternating => value % 2 == 0 ? count - value : -count + value,
            _ => 0
        };
        return result;
    }
}