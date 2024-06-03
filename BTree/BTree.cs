﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace BTree;

[DebuggerDisplay("Count: {Count}")]
public class BTree<T>(ushort degree = BTree<T>.DefaultDegree) where T : IComparable<T>
{
    [DebuggerDisplay("Leaf: {IsLeaf}, Count: {Count}")]
    private class Node
    {
        /// <summary>
        /// Maximum number of children per node. Maximum number of items per node is <see cref="Degree"/> - 1. Minimum is <see cref="MinDegree"/>
        /// </summary>
        internal ushort Degree { get; private set; }
        internal T[] Items { get; private set; }
        internal Node[] Children { get; private set; }
        internal int Count { get; set; } = 0;
        internal bool IsLeaf { get; set; }

        internal bool IsFull => Count >= Degree;
        internal int MinCount => (Degree - 1) / 2;
        internal bool HasUnderflow => Count < MinCount;
        public bool CanBorrow => Count > MinCount;

        internal Node(ushort degree, bool isLeaf)
        {
            Degree = degree < MinDegree ? MinDegree : degree;
            IsLeaf = isLeaf;
            Items = new T[Degree];
            Children = IsLeaf ? null : new Node[Degree + 1];
        }

        internal Node(ushort degree, Node leftChild, Node rightChild, T splittingItem) : this(degree, false)
        {
            if (leftChild == null)
            {
                throw new Exception(nameof(leftChild));
            }
            if (rightChild == null)
            {
                throw new Exception(nameof(leftChild));
            }
            if (leftChild.IsLeaf != rightChild.IsLeaf)
            {
                throw new Exception("Left child node and right child node must be of the same type.");
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
                    for (int i = index; i < Count; i++)
                    {
                        Items[i] = Items[i + 1];
                    }
                    Items[Count - 1] = default;
                }
                else
                {
                    for (int i = index; i < Count - 1; i++)
                    {
                        Items[i] = Items[i + 1];
                        Children[i] = Children[i + 1];
                    }

                    Children[Count - 1] = Children[Count];
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
                    for (int i = Count - 1; i >= index; i--)
                    {
                        Items[i + 1] = Items[i];
                    }

                    Items[index] = default;
                }
                else
                {
                    Children[Count + 1] = Children[Count];
                    for (int i = Count - 1; i >= index; i--)
                    {
                        Items[i + 1] = Items[i];
                        Children[i + 1] = Children[i];
                    }

                    Items[index] = default;
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
            if (index < Count && Items[index].CompareTo(item) == 0)
            {
                return new(true, null);
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

            if (IsFull)
            {
                splitResult = SplitNode();
            }

            return new(false, splitResult);
        }

        internal record struct SplitResult(Node NewRightNode, T SplittingItem);
        private SplitResult SplitNode()
        {
            Node newRightNode = new(Degree, IsLeaf);

            int leftNodeCount = (Count - 1) / 2;
            int splittingItemIndex = leftNodeCount;
            T splittingItem = Items[splittingItemIndex];
            Items[splittingItemIndex] = default;

            int rightNodeIndex = 0;

            if (IsLeaf)
            {
                for (int i = leftNodeCount + 1; i < Count; i++)
                {
                    newRightNode.Items[rightNodeIndex] = Items[i];
                    Items[i] = default;

                    newRightNode.Count++;
                    rightNodeIndex++;
                }

                Count = leftNodeCount;
            }
            else
            {
                for (int i = leftNodeCount + 1; i < Count; i++)
                {
                    newRightNode.Items[rightNodeIndex] = Items[i];
                    Items[i] = default;


                    newRightNode.Children[rightNodeIndex] = Children[i];
                    Children[i] = null;

                    // There is one more child than items
                    if (i == Count - 1)
                    {
                        newRightNode.Children[rightNodeIndex + 1] = Children[i + 1];
                        Children[i + 1] = null;
                    }

                    newRightNode.Count++;
                    rightNodeIndex++;
                }

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

                        if (removeResult && child.HasUnderflow)
                        {
                            HandlePotentialUnderflow(index);
                        }

                        return true;
                    }
                }
            }

            // Handle children afterwards
            if (!IsLeaf)
            {
                Node child = Children[index];
                bool removeResult = child.Remove(key, out item);

                if (removeResult && child.HasUnderflow)
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

                if (removeResult && child.HasUnderflow)
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

                if (removeResult && child.HasUnderflow)
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
            Debug.Assert(leftSibling.CanBorrow);

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
            Debug.Assert(rightSibling.CanBorrow);

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
            Debug.Assert(!leftChild.CanBorrow && !rightChild.CanBorrow);

            if (leftChild.IsLeaf)
            {
                leftChild.Items[leftChild.Count] = Items[index]; // The left child takes the splitting item of the parent
                Items[index] = default;
                leftChild.Count++;

                // Copy all items of the right child to the left child and clear them in the right child
                for (int i = 0; i < rightChild.Count; i++)
                {
                    leftChild.Items[leftChild.Count] = rightChild.Items[i];
                    leftChild.Count++;
                    rightChild.Items[i] = default;
                }

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

                for (int i = 0; i < rightChild.Count; i++)
                {
                    leftChild.Items[leftChild.Count] = rightChild.Items[i];
                    rightChild.Items[i] = default;
                    leftChild.Children[leftChild.Count] = rightChild.Children[i];
                    rightChild.Children[i] = default;

                    leftChild.Count++;
                }
                leftChild.Children[leftChild.Count] = rightChild.Children[rightChild.Count];
                rightChild.Children[rightChild.Count] = default;
                rightChild.Count = 0;

                Children[index + 1] = Children[index];
                Children[index] = default;

                MoveLeft(index);
                Count--;
            }
        }


        internal void HandlePotentialUnderflow(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(!IsLeaf);
            Debug.Assert(index < Count + 1);

            Node child = Children[index];
            if (!child.HasUnderflow)
            {
                return;
            }

            Node leftSibling = index > 0 ? Children[index - 1] : null;
            Node rightSibling = index < Count ? Children[index + 1] : null;

            if (leftSibling?.CanBorrow == true)
            {
                BorrowFromLeftSibling(index);
            }
            else if (rightSibling?.CanBorrow == true)
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
                throw new Exception("Unhandled undeflow");
            }
        }

        internal bool Contains<TKey>(TKey key) where TKey : IComparable<T>
        {
            int index = FindNextGreaterOrEqual(key);

            // check this node first
            if (index < Count)
            {
                T currentItem = Items[index];
                if (key.CompareTo(currentItem) == 0)
                {
                    return true;
                }
            }

            // Handle children afterwards
            if (!IsLeaf)
            {
                Node child = Children[index];

                return child.Contains(key);
            }

            return false;
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

            // Handle children afterwards
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

    /// <summary>
    /// The default Degree is choosen to be a good comprimise of performance and memory consumtion.
    /// </summary>
    public const ushort DefaultDegree = 64;

    public const ushort MinDegree = 3;

    private Node _Root { get; set; } = new(degree, true);

    /// <summary>
    /// Maximum number of children per node. Maximum number of items per node is <see cref="Degree"/> - 1. Minimum is <see cref="MinDegree"/>
    /// </summary>
    public ushort Degree { get; private set; } = degree < MinDegree ? MinDegree : degree;

    private long _Count { get; set; } = 0L;

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

            if (CountChanged != null)
            {
                try
                {
                    CountChanged.Invoke(this, value);
                }
                catch { }

            }
        }
    }

    public event EventHandler<long> CountChanged;

    private long _IterationCount = 0L;

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
            _Root = new(Degree, _Root, insertResult.SplitResult.Value.NewRightNode, insertResult.SplitResult.Value.SplittingItem);
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

        return _Root.Contains(key);
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
    /// <returns>true if a mininal item exists otherwise false</returns>
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
    /// Performs an action for every single item within an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The parameter <paramref name="maxKey"/> and <paramref name="minKey"/> can be a reduced version of an item as long as they implement <see cref="IComparable{T}"/>.
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Use this over <see cref="GetRange(T, T, bool)"/> in performance critical paths.
    /// </summary>
    /// <param name="action"></param>
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
    /// Performs an action for every single item.
    /// Use this over <see cref="GetAll"/> if performance is critical.
    /// </summary>
    /// <param name="action"></param>
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
    /// Gets a range of items limited by an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The parameter <paramref name="maxKey"/> and <paramref name="minKey"/> can be a reduced version of an item as long as they implement <see cref="IComparable{T}"/>.
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Consider using <see cref="DoForEach(Action{T}, T, T, bool)"/> if performance is critical.
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

        _Root = new(Degree, true);
        Count = 0L;
    }

#if DEBUG
    public string PrettyPrint()
    {
        System.Text.StringBuilder builder = new();

        _Root.PrettyPrint(builder, 0);

        string result = builder.ToString();
        return result;
    }

    internal string PrettyString => PrettyPrint();

    public override string ToString()
    {
        return PrettyPrint();
    }
#else
    public override string ToString()
    {
        return $"Count: {Count}";
    }
#endif

}