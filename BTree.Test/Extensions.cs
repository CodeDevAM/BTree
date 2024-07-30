using System;

namespace BTree.Test;

internal static class Extensions
{
    private const int ShuffleCount = 3;
    public static void Shuffle<T>(this T[] list)
    {
        Random randomNumberGenerator = new(7);
        for (int i = 0; i < ShuffleCount; i++)
        {
            int n = list.Length;
            while (n > 1)
            {
                int k = randomNumberGenerator.Next(n--);
                // Swap
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
