namespace BTree.Test;

internal static class Extensions
{
    private const int ShuffleCount = 3;
    private static readonly Random RandomNumberGenerator = new(7);
    public static void Shuffle<T>(this T[] list)
    {
        for (int i = 0; i < ShuffleCount; i++)
        {
            int n = list.Length;
            while (n > 1)
            {
                int k = RandomNumberGenerator.Next(n--);
                T temp = list[n];
                list[n] = list[k];
                list[k] = temp;
            }
        }
    }
}
