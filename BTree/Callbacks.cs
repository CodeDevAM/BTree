namespace BTree;

/// <summary>
/// Interface for high-performance, allocation-free iteration over BTree items.
/// Implement this interface on a struct to avoid delegate overhead in hot paths.
/// </summary>
/// <typeparam name="T">The item type</typeparam>
/// <example>
/// <code>
/// struct SumCallback : ICallback&lt;int&gt;
/// {
///     public int Sum;
///     public bool Invoke(int item) { Sum += item; return false; }
/// }
/// 
/// var callback = new SumCallback();
/// tree.DoForEach(minKey, maxKey, ref callback);
/// Console.WriteLine(callback.Sum);
/// </code>
/// </example>
public interface ICallback<T> where T : notnull
{
    /// <summary>
    /// Called for each item during iteration.
    /// </summary>
    /// <param name="item">The current item</param>
    /// <returns>true to cancel iteration, false to continue</returns>
    bool Invoke(T item);
}

/// <summary>
/// Interface for high-performance, allocation-free iteration over BPlusTree items.
/// Implement this interface on a struct to avoid delegate overhead in hot paths.
/// </summary>
/// <typeparam name="TKey">The key type</typeparam>
/// <typeparam name="TItem">The item type</typeparam>
/// <example>
/// <code>
/// struct CollectCallback : ICallback&lt;int, string&gt;
/// {
///     public List&lt;string&gt; Items;
///     public bool Invoke(int key, string item) { Items.Add(item); return false; }
/// }
/// 
/// var callback = new CollectCallback { Items = new List&lt;string&gt;() };
/// tree.DoForEach(minKey, maxKey, ref callback);
/// </code>
/// </example>
public interface ICallback<TKey, TItem> where TKey : notnull
{
    /// <summary>
    /// Called for each item during iteration.
    /// </summary>
    /// <param name="key">The key of the current item</param>
    /// <param name="item">The current item</param>
    /// <returns>true to cancel iteration, false to continue</returns>
    bool Invoke(TKey key, TItem item);
}
