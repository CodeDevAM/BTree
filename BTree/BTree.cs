using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#pragma warning disable IDE1006
// ReSharper disable InconsistentNaming

namespace BTree;

[DebuggerDisplay("Count: {Count}")]
public class BTree<T>(ushort degree = BTree<T>.DefaultDegree) where T : IComparable<T>
{
    [DebuggerDisplay("Leaf: {IsLeaf}, Count: {Count}")]
    private class Node
    {
        /// <summary>
        /// Maximum number of children per node. Maximum number of items per node is <see cref="_Degree"/> - 1. Minimum is <see cref="MinDegree"/>
        /// </summary>
        private ushort _Degree { get; set; }

        internal T[] Items { get; private set; }
        internal Node[] Children { get; private set; }
        internal int Count { get; set; }
        internal bool IsLeaf { get; set; }

        private bool _IsFull => Count >= _Degree;
        private int _MinCount => (_Degree - 1) / 2;
        private bool _HasUnderflow => Count < _MinCount;
        private bool _CanBorrow => Count > _MinCount;

        internal Node(ushort degree, bool isLeaf)
        {
            _Degree = degree < MinDegree ? MinDegree : degree;
            IsLeaf = isLeaf;
            Items = new T[_Degree];
            Children = IsLeaf ? null : new Node[_Degree + 1];
        }

        internal Node(ushort degree, Node leftChild, Node rightChild, T splittingItem) : this(degree, false)
        {
            if (leftChild == null)
            {
                throw new ArgumentNullException(nameof(leftChild));
            }
            if (rightChild == null)
            {
                throw new ArgumentNullException(nameof(leftChild));
            }
            if (leftChild.IsLeaf != rightChild.IsLeaf)
            {
                throw new ArgumentException("Left child node and right child node must be of the same type.");
            }

            Count = 1;

            Items[0] = splittingItem;

            Children[0] = leftChild;
            Children[1] = rightChild;
        }

        private void MoveLeft(int index)
        {
            Debug.Assert(index >= 0);

            if (index < Count)
            {
                if (IsLeaf)
                {
                    Array.Copy(Items, index + 1, Items, index, Count - index - 1);
                    Items[Count - 1] = default;
                }
                else
                {
                    Array.Copy(Items, index + 1, Items, index, Count - index - 1);
                    Items[Count - 1] = default;
                    Array.Copy(Children, index + 1, Children, index, Count - index);
                    Children[Count] = default;
                }
            }

        }

        private void MoveRight(int index)
        {
            Debug.Assert(index >= 0);

            if (index <= Count)
            {
                if (IsLeaf)
                {
                    if (index < Count)
                    {
                        Array.Copy(Items, index, Items, index + 1, Count - index);
                    }
                    Items[index] = default;
                }
                else
                {
                    if (index < Count)
                    {
                        Array.Copy(Items, index, Items, index + 1, Count - index);
                    }
                    Items[index] = default;

                    Array.Copy(Children, index, Children, index + 1, Count - index + 1);
                }
            }
        }

        private int FindNextGreaterOrEqual<TKey>(TKey key) where TKey : IComparable<T>
        {
            int left = 0;
            int mid = 0;
            int right = Count - 1;
            int compareResult = 0;

            while (right >= left)
            {
                mid = (right + left) / 2;
                T currentItem = Items[mid];
                compareResult = key.CompareTo(currentItem);

                if (compareResult < 0)
                {
                    if (right == mid)
                    {
                        break;
                    }
                    right = mid;
                }
                else if (compareResult > 0)
                {
                    left = mid + 1;
                }
                else
                {
                    break;
                }
            }

            if (compareResult > 0)
            {
                return Count;
            }
            else
            {
                return mid;
            }
        }

        internal record struct InsertResult(bool Updated, SplitResult? SplitResult);

        internal InsertResult InsertOrUpdate(T item)
        {
            int index = FindNextGreaterOrEqual(item);

            // This item is already exists update it
            if (index < Count)
            {

                T currentItem = Items[index];
                int comparisonResult = item.CompareTo(currentItem);

                if (comparisonResult == 0)
                {
                    return new(true, null);
                }
            }

            // This item does not exist yet so insert it
            SplitResult? splitResult = null;

            if (IsLeaf)
            {
                MoveRight(index);
                Items[index] = item;
                Count++;
            }
            else
            {
                Node child = Children[index];
                InsertResult insertResult = child.InsertOrUpdate(item);

                if (insertResult.Updated)
                {
                    return new(true, null);
                }
                else if (insertResult.SplitResult.HasValue)
                {
                    SplitResult currentSplitResult = insertResult.SplitResult.Value;
                    MoveRight(index);
                    Items[index] = currentSplitResult.SplittingItem;
                    Children[index + 1] = currentSplitResult.NewRightNode;
                    Count++;
                }
            }

            if (_IsFull)
            {
                splitResult = SplitNode();
            }

            return new(false, splitResult);
        }

        internal record struct SplitResult(Node NewRightNode, T SplittingItem);
        private SplitResult SplitNode()
        {
            Node newRightNode = new(_Degree, IsLeaf);

            int leftNodeCount = (Count - 1) / 2;
            int splittingItemIndex = leftNodeCount;
            T splittingItem = Items[splittingItemIndex];
            Items[splittingItemIndex] = default;

            if (IsLeaf)
            {
                newRightNode.Count = Count - leftNodeCount - 1;
                Array.Copy(Items, leftNodeCount + 1, newRightNode.Items, 0, newRightNode.Count);
                Array.Clear(Items, leftNodeCount + 1, newRightNode.Count);

                Count = leftNodeCount;
            }
            else
            {
                newRightNode.Count = Count - leftNodeCount - 1;
                Array.Copy(Items, leftNodeCount + 1, newRightNode.Items, 0, newRightNode.Count);
                Array.Clear(Items, leftNodeCount + 1, newRightNode.Count);

                Array.Copy(Children, leftNodeCount + 1, newRightNode.Children, 0, newRightNode.Count + 1);
                Array.Clear(Children, leftNodeCount + 1, newRightNode.Count + 1);

                Count = leftNodeCount;
            }

            SplitResult result = new(newRightNode, splittingItem);
            return result;
        }

        internal bool Remove<TKey>(TKey key, out T item) where TKey : IComparable<T>
        {
            int index = FindNextGreaterOrEqual(key);

            // Check this node first
            if (index < Count)
            {
                T currentItem = Items[index];
                int comparisonResult = key.CompareTo(currentItem);

                if (comparisonResult == 0)
                {
                    item = currentItem;

                    if (IsLeaf)
                    {
                        MoveLeft(index);
                        Count--;
                        return true;
                    }
                    else
                    {
                        Node child = Children[index];
                        bool removeResult = child.RemoveMax(out T maxItem);

                        if (removeResult == false)
                        {
                            throw new Exception("Removing item failed.");
                        }

                        Items[index] = maxItem;

                        if (child._HasUnderflow)
                        {
                            HandlePotentialUnderflow(index);
                        }

                        return true;
                    }
                }
            }

            // Handle children afterward
            if (!IsLeaf)
            {
                Node child = Children[index];
                bool removeResult = child.Remove(key, out item);

                if (removeResult && child._HasUnderflow)
                {
                    HandlePotentialUnderflow(index);
                }

                return removeResult;
            }

            item = default;
            return false;
        }

        internal bool RemoveMin(out T minItem)
        {
            if (Count <= 0)
            {
                minItem = default;
                return false;
            }

            if (IsLeaf)
            {
                minItem = Items[0];
                MoveLeft(0);
                Count--;
                return true;
            }
            else
            {
                Node child = Children[0];

                bool removeResult = child.RemoveMin(out minItem);

                if (removeResult && child._HasUnderflow)
                {
                    HandlePotentialUnderflow(0);
                }

                return removeResult;
            }
        }

        internal bool RemoveMax(out T maxItem)
        {
            if (Count <= 0)
            {
                maxItem = default;
                return false;
            }

            if (IsLeaf)
            {
                maxItem = Items[Count - 1];
                Items[Count - 1] = default;
                Count--;
                return true;
            }
            else
            {
                Node child = Children[Count];

                bool removeResult = child.RemoveMax(out maxItem);

                if (removeResult && child._HasUnderflow)
                {
                    HandlePotentialUnderflow(Count);
                }

                return removeResult;
            }
        }

        private void BorrowFromLeftSibling(int index)
        {
            Debug.Assert(index >= 1);
            Debug.Assert(!IsLeaf);
            Debug.Assert(index < Count + 1);

            Node child = Children[index];
            Node leftSibling = Children[index - 1];

            Debug.Assert(child.IsLeaf == leftSibling.IsLeaf);
            Debug.Assert(leftSibling._CanBorrow);

            if (child.IsLeaf)
            {
                child.MoveRight(0);
                child.Items[0] = Items[index - 1]; // The child takes the splitting item of the parent
                child.Count++;

                Items[index - 1] = leftSibling.Items[leftSibling.Count - 1]; // The parent takes the max item of the left sibling

                leftSibling.Items[leftSibling.Count - 1] = default;
                leftSibling.Count--;
            }
            else
            {
                child.MoveRight(0);
                child.Items[0] = Items[index - 1]; // The child takes the splitting item of the parent
                child.Children[0] = leftSibling.Children[leftSibling.Count]; // The child takes the last child of the left sibling
                child.Count++;

                Items[index - 1] = leftSibling.Items[leftSibling.Count - 1]; // The parent takes the max item of the left sibling

                leftSibling.Items[leftSibling.Count - 1] = default;
                leftSibling.Children[leftSibling.Count] = default;
                leftSibling.Count--;
            }
        }

        private void BorrowFromRightSibling(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(!IsLeaf);
            Debug.Assert(index < Count);

            Node child = Children[index];
            Node rightSibling = Children[index + 1];

            Debug.Assert(child.IsLeaf == rightSibling.IsLeaf);
            Debug.Assert(rightSibling._CanBorrow);

            if (child.IsLeaf)
            {
                child.Items[child.Count] = Items[index]; // The child takes the splitting item of the parent
                child.Count++;

                Items[index] = rightSibling.Items[0]; // The parent takes the min item of the right sibling
                rightSibling.MoveLeft(0);
                rightSibling.Count--;
            }
            else
            {
                child.Items[child.Count] = Items[index]; // The child takes the splitting item of the parent
                child.Children[child.Count + 1] = rightSibling.Children[0]; // The child takes the first child of the right sibling
                child.Count++;

                Items[index] = rightSibling.Items[0]; // The parent takes the min item of the right sibling
                rightSibling.MoveLeft(0);
                rightSibling.Count--;
            }
        }

        private void Merge(int rightChildIndex)
        {
            int index = rightChildIndex - 1;

            Debug.Assert(index >= 0);
            Debug.Assert(!IsLeaf);
            Debug.Assert(index < Count);


            Node leftChild = Children[index];
            Node rightChild = Children[index + 1];

            Debug.Assert(leftChild.IsLeaf == rightChild.IsLeaf);
            Debug.Assert(!leftChild._CanBorrow && !rightChild._CanBorrow);

            if (leftChild.IsLeaf)
            {
                leftChild.Items[leftChild.Count] = Items[index]; // The left child takes the splitting item of the parent
                Items[index] = default;
                leftChild.Count++;

                // Copy all items of the right child to the left child and clear them in the right child
                Array.Copy(rightChild.Items, 0, leftChild.Items, leftChild.Count, rightChild.Count);
                leftChild.Count += rightChild.Count;
                Array.Clear(rightChild.Items, 0, rightChild.Count);

                rightChild.Count = 0;

                Children[index + 1] = Children[index];
                Children[index] = default;

                MoveLeft(index);
                Count--;
            }
            else
            {
                leftChild.Items[leftChild.Count] = Items[index]; // The left child takes the splitting item of the parent
                Items[index] = default;
                leftChild.Count++;

                Items[index] = rightChild.Items[0];

                // Copy all items and children of the right child to the left child and clear them in the right child
                Array.Copy(rightChild.Items, 0, leftChild.Items, leftChild.Count, rightChild.Count);
                Array.Copy(rightChild.Children, 0, leftChild.Children, leftChild.Count, rightChild.Count + 1);
                leftChild.Count += rightChild.Count;
                Array.Clear(rightChild.Items, 0, rightChild.Count);
                Array.Clear(rightChild.Children, 0, rightChild.Count + 1);

                Children[index + 1] = Children[index];
                Children[index] = default;

                MoveLeft(index);
                Count--;
            }
        }

        private void HandlePotentialUnderflow(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(!IsLeaf);
            Debug.Assert(index < Count + 1);

            Node child = Children[index];
            if (!child._HasUnderflow)
            {
                return;
            }

            Node leftSibling = index > 0 ? Children[index - 1] : null;
            Node rightSibling = index < Count ? Children[index + 1] : null;

            if (leftSibling?._CanBorrow == true)
            {
                BorrowFromLeftSibling(index);
            }
            else if (rightSibling?._CanBorrow == true)
            {
                BorrowFromRightSibling(index);
            }
            else if (leftSibling != null)
            {
                Merge(index);
            }
            else if (rightSibling != null)
            {
                Merge(index + 1);
            }
            else
            {
                throw new Exception("Unhandled underflow");
            }
        }

        internal bool Get<TKey>(TKey key, out T item) where TKey : IComparable<T>
        {
            item = default;

            int index = FindNextGreaterOrEqual(key);

            // check this node first
            if (index < Count)
            {
                T currentItem = Items[index];
                if (key.CompareTo(currentItem) == 0)
                {
                    item = currentItem;
                    return true;
                }
            }

            // Handle children afterward
            if (!IsLeaf)
            {
                Node child = Children[index];

                return child.Get(key, out item);
            }

            return false;
        }

        internal bool GetMin(out T minItem)
        {
            if (Count <= 0)
            {
                minItem = default;
                return false;
            }

            if (IsLeaf)
            {
                minItem = Items[0];
                return true;
            }
            else
            {
                Node child = Children[0];
                return child.GetMin(out minItem);
            }
        }

        internal bool GetMax(out T maxItem)
        {
            if (Count <= 0)
            {
                maxItem = default;
                return false;
            }

            if (IsLeaf)
            {
                maxItem = Items[Count - 1];
                return true;
            }
            else
            {
                Node child = Children[Count];
                return child.GetMax(out maxItem);
            }
        }

        internal NearestItems GetNearest<TKey>(TKey key) where TKey : IComparable<T>
        {
            if (Count <= 0)
            {
                return new(false, default, false, default);
            }

            int index = FindNextGreaterOrEqual(key);

            // check this node first
            if (index < Count)
            {
                T currentItem = Items[index];
                if (key.CompareTo(currentItem) == 0)
                {
                    return new(true, currentItem, false, default);
                }
            }

            if (IsLeaf)
            {
                if (index < Count)
                {
                    T currentItem = Items[index];
                    if (key.CompareTo(currentItem) < 0)
                    {
                        if (index > 0)
                        {
                            return new(true, Items[index - 1], true, currentItem);
                        }
                        else
                        {
                            return new(false, default, true, currentItem);
                        }
                    }
                    else // key > currentItem
                    {
                        if (index < Count - 1)
                        {
                            return new(true, currentItem, true, Items[index + 1]);
                        }
                        else
                        {
                            return new(true, currentItem, false, default);
                        }
                    }
                }
                else
                {
                    return new(true, Items[Count - 1], false, default);
                }
            }
            else
            {
                Node child = Children[index];

                NearestItems nearestItems = child.GetNearest(key);

                if (nearestItems.HasMinItem && nearestItems.HasMaxItem)
                {
                    return nearestItems;
                }
                else if (nearestItems.HasMinItem)
                {
                    if (key.CompareTo(nearestItems.MinItem) == 0)
                    {
                        return nearestItems;
                    }
                    else
                    {
                        if (index < Count)
                        {
                            return new(true, nearestItems.MinItem, true, Items[index]);
                        }
                        else
                        {
                            return nearestItems;
                        }
                    }
                }
                else if (nearestItems.HasMaxItem)
                {
                    if (index > 0)
                    {
                        return new(true, Items[index - 1], true, nearestItems.MaxItem);
                    }
                    else
                    {
                        return nearestItems;
                    }
                }

            }

            return new(false, default, false, default);
        }

        internal void DoForEach<TKey>(Action<T> action, TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
        {
            int index = FindNextGreaterOrEqual(minKey);

            for (int i = index; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    child.DoForEach(action, minKey, maxKey, maxInclusive);
                }

                if (i < Count)
                {
                    T currentItem = Items[i];
                    int comparisonResult = maxKey.CompareTo(currentItem);

                    if (maxInclusive)
                    {
                        if (comparisonResult < 0)
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (comparisonResult <= 0)
                        {
                            return;
                        }
                    }

                    action.Invoke(currentItem);
                }
            }
        }

        internal bool DoForEach<TKey>(Func<T, bool> actionAndCancelFunction, TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
        {
            int index = FindNextGreaterOrEqual(minKey);

            for (int i = index; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    bool cancel = child.DoForEach(actionAndCancelFunction, minKey, maxKey, maxInclusive);
                    if (cancel)
                    {
                        return true;
                    }
                }

                if (i < Count)
                {
                    T currentItem = Items[i];
                    int comparisonResult = maxKey.CompareTo(currentItem);

                    if (maxInclusive)
                    {
                        if (comparisonResult < 0)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (comparisonResult <= 0)
                        {
                            return true;
                        }
                    }

                    bool cancel = actionAndCancelFunction.Invoke(currentItem);
                    if (cancel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal void DoForEach(Action<T> action)
        {
            for (int i = 0; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    child.DoForEach(action);
                }

                if (i < Count)
                {
                    T item = Items[i];
                    action.Invoke(item);
                }
            }
        }

        internal bool DoForEach(Func<T, bool> actionAndCancelFunction)
        {
            for (int i = 0; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    bool cancel = child.DoForEach(actionAndCancelFunction);
                    if (cancel)
                    {
                        return true;
                    }
                }

                if (i < Count)
                {
                    T item = Items[i];
                    bool cancel = actionAndCancelFunction.Invoke(item);
                    if (cancel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal IEnumerable<T> GetRange<TKey>(TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
        {
            int index = FindNextGreaterOrEqual(minKey);

            for (int i = index; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    foreach (T item in child.GetRange(minKey, maxKey, maxInclusive))
                    {
                        yield return item;
                    }
                }

                if (i < Count)
                {
                    T currentItem = Items[i];
                    int comparisonResult = maxKey.CompareTo(currentItem);

                    if (maxInclusive)
                    {
                        if (comparisonResult < 0)
                        {
                            yield break;
                        }
                    }
                    else
                    {
                        if (comparisonResult <= 0)
                        {
                            yield break;
                        }
                    }

                    yield return currentItem;
                }
            }
        }

        internal IEnumerable<T> GetAll()
        {
            for (int i = 0; i <= Count; i++)
            {
                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    foreach (T item in child.GetAll())
                    {
                        yield return item;
                    }
                }

                if (i < Count)
                {
                    T item = Items[i];
                    yield return item;
                }
            }
        }
#if DEBUG

        internal void PrettyPrint(System.Text.StringBuilder builder, uint indentationCount)
        {
            for (int i = Count; i >= 0; i--)
            {
                if (Count == 0)
                {
                    for (int t = 0; t < indentationCount; t++)
                    {
                        builder.Append("  ");
                    }
                    builder.Append("-\n");
                }
                else if (i < Count)
                {
                    for (int t = 0; t < indentationCount; t++)
                    {
                        builder.Append("  ");
                    }
                    T item = Items[i];
                    builder.Append(item is null ? "-\n" : $"{item}\n");
                }

                // Handle children first
                if (!IsLeaf)
                {
                    Node child = Children[i];
                    if (child != null)
                    {
                        child.PrettyPrint(builder, indentationCount + 1);
                    }
                    else
                    {
                        for (int t = 0; t < indentationCount; t++)
                        {
                            builder.Append("  ");
                        }
                        builder.Append($"/\n");
                    }
                }
            }
        }

        internal string PrettyString
        {
            get
            {
                System.Text.StringBuilder builder = new();

                PrettyPrint(builder, 0);

                string result = builder.ToString();
                return result;
            }
        }
#endif
    }

    public record struct NearestItems(bool HasMinItem, T MinItem, bool HasMaxItem, T MaxItem);

    /// <summary>
    /// The default Degree is chosen to be a good compromise of performance and memory consumption.
    /// </summary>
    public const ushort DefaultDegree = 64;

    public const ushort MinDegree = 3;

    private Node _Root { get; set; } = new(degree, true);

    /// <summary>
    /// Maximum number of children per node. Maximum number of items per node is <see cref="_Degree"/> - 1. Minimum is <see cref="MinDegree"/>
    /// </summary>
    private ushort _Degree { get; set; } = degree < MinDegree ? MinDegree : degree;

    private long _Count { get; set; }

    /// <summary>
    /// The number of items.
    /// </summary>
    public long Count
    {
        get => _Count;
        set
        {
            if (value == _Count)
            {
                return;
            }
            _Count = value;

            try
            {
                CountChanged?.Invoke(this, value);
            }
            catch
            {
                // ignored
            }
        }
    }

    public event EventHandler<long> CountChanged;

    private long _IterationCount;

    /// <summary>
    /// Inserts an item or updates it if it already exists.
    /// </summary>
    /// <param name="item">The item that shall be inserted or updated.</param>
    /// <returns>true if an existing item was updated otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public bool InsertOrUpdate(T item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        Node.InsertResult insertResult = _Root.InsertOrUpdate(item);
        if (insertResult.Updated)
        {
            return true;
        }
        else if (insertResult.SplitResult.HasValue)
        {
            _Root = new(_Degree, _Root, insertResult.SplitResult.Value.NewRightNode, insertResult.SplitResult.Value.SplittingItem);
        }

        Count++;

        return false;
    }

    /// <summary>
    /// Removes the item that is associated to the given <paramref name="key"/>. The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/></param>
    /// <param name="item">The item that was removed</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool Remove<TKey>(TKey key, out T item) where TKey : IComparable<T>
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        bool removeResult = _Root.Remove(key, out item);

        if (removeResult)
        {
            if (!_Root.IsLeaf && _Root.Count <= 0)
            {
                _Root = _Root.Children[0];
            }

            Count--;
        }

        return removeResult;
    }

    /// <summary>
    /// Removes the minimum item if it exists.
    /// </summary>
    /// <param name="minItem">The minimum item that was removed</param>
    /// <returns>true if the minimum item was removed otherwise false</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool RemoveMin(out T minItem)
    {
        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        bool removeResult = _Root.RemoveMin(out minItem);

        if (removeResult)
        {
            if (!_Root.IsLeaf && _Root.Count <= 0)
            {
                _Root = _Root.Children[0];
            }

            Count--;
        }

        return removeResult;

    }

    /// <summary>
    /// Removes the maximum item if it exists.
    /// </summary>
    /// <param name="maxItem">The maximum item that was removed</param>
    /// <returns>true if the maximum item was removed otherwise false</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool RemoveMax(out T maxItem)
    {
        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }
        bool removeResult = _Root.RemoveMax(out maxItem);

        if (removeResult)
        {
            if (!_Root.IsLeaf && _Root.Count <= 0)
            {
                _Root = _Root.Children[0];
            }

            Count--;
        }

        return removeResult;
    }

    /// <summary>
    /// Checks if a specific item that is associated to the given key is contained. The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/>.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/></param>
    /// <returns>true if the item exists otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Contains<TKey>(TKey key) where TKey : IComparable<T>
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _Root.Get(key, out _);
    }

    /// <summary>
    /// Gets the item that is associated to the given <paramref name="key"/>. The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="key">The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/></param>
    /// <param name="item">The item that is associated to the given <paramref name="key"/> if it exists.</param>
    /// <returns>true if the key is exists otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Get<TKey>(TKey key, out T item) where TKey : IComparable<T>
    {
        item = default;

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _Root.Get(key, out item);
    }

    /// <summary>
    /// Gets the minimum item if it exists.
    /// </summary>
    /// <param name="minItem"></param>
    /// <returns>true if a minimum item exists otherwise false</returns>
    public bool GetMin(out T minItem)
    {
        return _Root.GetMin(out minItem);
    }

    /// <summary>
    /// Gets the maximum item if it exists.
    /// </summary>
    /// <param name="maxItem"></param>
    /// <returns>true if a maximum item exists otherwise false</returns>
    public bool GetMax(out T maxItem)
    {
        return _Root.GetMax(out maxItem);
    }

    /// <summary>
    /// Gets the next greater item as <see cref="NearestItems.MaxItem"/> and the next lower item as <see cref="NearestItems.MinItem"/> if existant. If the key equals an item only this item is returned an <see cref="NearestItems.MinItem"/>.
    /// The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/>.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="key">The <paramref name="key"/> can be a reduced version of an item as long as it implements <see cref="IComparable{T}"/></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public NearestItems GetNearest<TKey>(TKey key) where TKey : IComparable<T>
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _Root.GetNearest(key);
    }

    /// <summary>
    /// Performs an action for every single item within an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The parameter <paramref name="maxKey"/> and <paramref name="minKey"/> can be a reduced version of an item as long as they implement <see cref="IComparable{T}"/>.
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Use this over <see cref="GetRange{TKey}(TKey, TKey, bool)"/> in performance critical paths.
    /// </summary>
    /// <param name="action">Function that will be called for every relevant item.</param>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void DoForEach<TKey>(Action<T> action, TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
    {
        if (action == null)
        {
            return;
        }

        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }

        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            _Root.DoForEach(action, minKey, maxKey, maxInclusive);
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Performs an action for every single item within an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The parameter <paramref name="maxKey"/> and <paramref name="minKey"/> can be a reduced version of an item as long as they implement <see cref="IComparable{T}"/>.
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Use this over <see cref="GetRange{TKey}(TKey, TKey, bool)"/> in performance critical paths.
    /// It offers the possibility to cancel the iteration.
    /// </summary>
    /// <param name="actionAndCancelFunction">Function that will be called for every relevant item. It returns true to cancel or false to continue.</param>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void DoForEach<TKey>(Func<T, bool> actionAndCancelFunction, TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
    {
        if (actionAndCancelFunction == null)
        {
            return;
        }

        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }

        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            _Root.DoForEach(actionAndCancelFunction, minKey, maxKey, maxInclusive);
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Performs an action for every single item.
    /// Use this over <see cref="GetAll"/> if performance is critical.
    /// </summary>
    /// <param name="action">Function that will be called for every item.</param>
    public void DoForEach(Action<T> action)
    {
        if (action == null)
        {
            return;
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            _Root.DoForEach(action);
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Performs an action for every single item. It offers the possibility to cancel the iteration.
    /// Use this over <see cref="GetAll"/> if performance is critical.
    /// </summary>
    /// <param name="actionAndCancelFunction">Function that will be called for every item. It returns true to cancel or false to continue.</param>
    public void DoForEach(Func<T, bool> actionAndCancelFunction)
    {
        if (actionAndCancelFunction == null)
        {
            return;
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            _Root.DoForEach(actionAndCancelFunction);
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Gets a range of items limited by an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The parameter <paramref name="maxKey"/> and <paramref name="minKey"/> can be a reduced version of an item as long as they implement <see cref="IComparable{T}"/>.
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Consider using <see cref="DoForEach{TKey}(Action{T}, TKey, TKey, bool)"/> if performance is critical.
    /// </summary>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IEnumerable<T> GetRange<TKey>(TKey minKey, TKey maxKey, bool maxInclusive) where TKey : IComparable<T>
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }

        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            foreach (T item in _Root.GetRange(minKey, maxKey, maxInclusive))
            {
                yield return item;
            }
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }

    }

    /// <summary>
    /// Gets all items. Consider using <see cref="DoForEach(Action{T})"/> if performance is critical.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> GetAll()
    {
        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            foreach (T item in _Root.GetAll())
            {
                yield return item;
            }
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Resets and clears.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Clear()
    {
        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        _Root = new(_Degree, true);
        Count = 0L;
    }

    public override string ToString()
    {
        return $"Count: {Count}";
    }

#if DEBUG
    internal string PrettyPrint()
    {
        System.Text.StringBuilder builder = new();

        _Root.PrettyPrint(builder, 0);

        string result = builder.ToString();
        return result;
    }

    internal string PrettyString => PrettyPrint();

#endif

}