using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#pragma warning disable IDE1006
// ReSharper disable InconsistentNaming

namespace BTree;

[DebuggerDisplay("Count: {Count}")]
public class BPlusTree<TKey, TItem>(ushort degree = BTree<TKey>.DefaultDegree) where TKey : IComparable<TKey>
{
    [DebuggerDisplay("Leaf: {IsLeaf}, Count: {Count}")]
    private class Node
    {
        /// <summary>
        /// Maximum number of children per node. Maximum number of keys per node is <see cref="_Degree"/> - 1. Minimum is <see cref="MinDegree"/>
        /// </summary>
        private ushort _Degree { get; set; }

        internal TKey[] Keys { get; private set; }
        internal TItem[] Items { get; private set; }
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
            Keys = new TKey[_Degree];
            Items = IsLeaf ? new TItem[_Degree] : null;
            Children = IsLeaf ? null : new Node[_Degree + 1];
        }

        internal Node(ushort degree, Node leftChild, Node rightChild, TKey splittingKey) : this(degree, false)
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

            Keys[0] = splittingKey;

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
                    Array.Copy(Keys, index + 1, Keys, index, Count - index - 1);
                    Keys[Count - 1] = default;

                    Array.Copy(Items, index + 1, Items, index, Count - index - 1);
                    Items[Count - 1] = default;
                }
                else
                {
                    Array.Copy(Keys, index + 1, Keys, index, Count - index - 1);
                    Keys[Count - 1] = default;
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
                        Array.Copy(Keys, index, Keys, index + 1, Count - index);
                        Array.Copy(Items, index, Items, index + 1, Count - index);
                    }
                    Keys[index] = default;
                    Items[index] = default;
                }
                else
                {
                    if (index < Count)
                    {
                        Array.Copy(Keys, index, Keys, index + 1, Count - index);
                    }
                    Keys[index] = default;

                    Array.Copy(Children, index, Children, index + 1, Count - index + 1);
                }
            }
        }

        private int FindNextGreaterOrEqual(TKey key)
        {
            int left = 0;
            int mid = 0;
            int right = Count - 1;
            int compareResult = 0;

            while (right >= left)
            {
                mid = (right + left) / 2;
                TKey currentKey = Keys[mid];
                compareResult = key.CompareTo(currentKey);

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

        protected int BinarySearch(TKey key)
        {
            int left = 0;
            int mid = 0;
            int right = Count - 1;
            int compareResult = 0;

            while (right >= left)
            {
                mid = (right + left) / 2;

                TKey currentKey = Keys[mid];
                compareResult = key.CompareTo(currentKey);

                if (compareResult < 0)
                {
                    right = mid - 1;

                }
                else if (compareResult > 0)
                {
                    left = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return -1;
        }

        private (Node Child, int Index) GetChild(TKey key)
        {
            int index = FindNextGreaterOrEqual(key);

            TKey currentKey = Keys[index];
            index = index < Count && key.CompareTo(currentKey) >= 0 ? index + 1 : index;
            Node child = Children[index];
            return (child, index);
        }

        internal record struct InsertResult(bool Updated, SplitResult? SplitResult);

        internal InsertResult InsertOrUpdate(TKey key, TItem item)
        {
            SplitResult? splitResult = null;

            if (IsLeaf)
            {
                int index = FindNextGreaterOrEqual(key);
                // This item is already exists update it
                if (index < Count)
                {
                    TKey currentKey = Keys[index];
                    int comparisonResult = key.CompareTo(currentKey);

                    if (comparisonResult == 0)
                    {
                        Keys[index] = key;
                        Items[index] = item;
                        return new(true, null);
                    }
                }

                MoveRight(index);
                Keys[index] = key;
                Items[index] = item;
                Count++;
            }
            else
            {
                (Node child, int index) = GetChild(key);
                InsertResult insertResult = child.InsertOrUpdate(key, item);

                if (insertResult.Updated)
                {
                    return new(true, null);
                }
                else if (insertResult.SplitResult.HasValue)
                {
                    SplitResult currentSplitResult = insertResult.SplitResult.Value;
                    MoveRight(index);
                    Keys[index] = currentSplitResult.SplittingKey;
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

        internal record struct SplitResult(Node NewRightNode, TKey SplittingKey);
        private SplitResult SplitNode()
        {
            Node newRightNode = new(_Degree, IsLeaf);

            int leftNodeCount = (Count - 1) / 2;
            int splittingKeyIndex = leftNodeCount;
            TKey splittingKey = Keys[splittingKeyIndex];

            if (IsLeaf)
            {
                newRightNode.Count = Count - leftNodeCount;

                Array.Copy(Keys, leftNodeCount, newRightNode.Keys, 0, newRightNode.Count);
                Array.Clear(Keys, leftNodeCount, newRightNode.Count);

                Array.Copy(Items, leftNodeCount, newRightNode.Items, 0, newRightNode.Count);
                Array.Clear(Items, leftNodeCount, newRightNode.Count);

                Count = leftNodeCount;
            }
            else
            {
                newRightNode.Count = Count - leftNodeCount - 1;
                Array.Copy(Keys, leftNodeCount + 1, newRightNode.Keys, 0, newRightNode.Count);
                Array.Clear(Keys, leftNodeCount + 1, newRightNode.Count);

                Array.Copy(Children, leftNodeCount + 1, newRightNode.Children, 0, newRightNode.Count + 1);
                Array.Clear(Children, leftNodeCount + 1, newRightNode.Count + 1);

                Count = leftNodeCount;
            }

            SplitResult result = new(newRightNode, splittingKey);
            return result;
        }

        internal bool Remove(TKey key, out TItem item)
        {
            if (IsLeaf)
            {
                int index = FindNextGreaterOrEqual(key);

                if (index < Count)
                {
                    TKey currentKey = Keys[index];
                    int comparisonResult = key.CompareTo(currentKey);

                    if (comparisonResult == 0)
                    {
                        item = Items[index];

                        MoveLeft(index);
                        Count--;
                        return true;
                    }
                }
                else
                {
                    item = default;
                    return false;
                }
            }
            else
            {
                (Node child, int index) = GetChild(key);
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

        internal bool RemoveMin(out KeyValuePair<TKey, TItem> min)
        {
            if (IsLeaf)
            {
                if (Count <= 0)
                {
                    min = default;
                    return false;
                }
                else
                {
                    min = new(Keys[0], Items[0]);
                    MoveLeft(0);
                    Count--;
                    return true;
                }
            }
            else
            {
                Node child = Children[0];

                bool removeResult = child.RemoveMin(out min);

                if (removeResult && child._HasUnderflow)
                {
                    HandlePotentialUnderflow(0);
                }

                return removeResult;
            }
        }

        internal bool RemoveMax(out KeyValuePair<TKey, TItem> max)
        {
            if (IsLeaf)
            {
                if (Count <= 0)
                {
                    max = default;
                    return false;
                }
                else
                {
                    max = new(Keys[Count - 1], Items[Count - 1]);
                    Keys[Count - 1] = default;
                    Items[Count - 1] = default;
                    Count--;
                    return true;
                }
            }
            else
            {
                Node child = Children[Count];

                bool removeResult = child.RemoveMax(out max);

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
                child.Keys[0] = leftSibling.Keys[leftSibling.Count - 1]; // The child takes the splitting key of the left sibling
                child.Items[0] = leftSibling.Items[leftSibling.Count - 1]; // and the item
                child.Count++;

                Keys[index - 1] = leftSibling.Keys[leftSibling.Count - 1]; // The parent takes the max key of the left sibling

                leftSibling.Keys[leftSibling.Count - 1] = default;
                leftSibling.Items[leftSibling.Count - 1] = default;
                leftSibling.Count--;
            }
            else
            {
                child.MoveRight(0);
                child.Keys[0] = Keys[index - 1]; // The child takes the splitting key of the parent
                child.Children[0] = leftSibling.Children[leftSibling.Count]; // The child takes the last child of the left sibling
                child.Count++;

                Keys[index - 1] = leftSibling.Keys[leftSibling.Count - 1]; // The parent takes the max key of the left sibling

                leftSibling.Keys[leftSibling.Count - 1] = default;
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
                child.Keys[child.Count] = rightSibling.Keys[0]; // The child takes the splitting key of the right sibling
                child.Items[child.Count] = rightSibling.Items[0]; // and the item
                child.Count++;

                rightSibling.MoveLeft(0);
                Keys[index] = rightSibling.Keys[0]; // The parent takes the min key of the right sibling
                rightSibling.Count--;
            }
            else
            {
                child.Keys[child.Count] = Keys[index]; // The child takes the splitting key of the parent
                child.Children[child.Count + 1] = rightSibling.Children[0]; // The child takes the first child of the right sibling
                child.Count++;

                Keys[index] = rightSibling.Keys[0]; // The parent takes the min key of the right sibling
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
                // Copy all keys and items of the right child to the left child and clear them in the right child
                Array.Copy(rightChild.Keys, 0, leftChild.Keys, leftChild.Count, rightChild.Count);
                Array.Clear(rightChild.Keys, 0, rightChild.Count);
                Array.Copy(rightChild.Items, 0, leftChild.Items, leftChild.Count, rightChild.Count);
                Array.Clear(rightChild.Items, 0, rightChild.Count);
                leftChild.Count += rightChild.Count;

                rightChild.Count = 0;

                Children[index + 1] = Children[index];
                Children[index] = default;

                MoveLeft(index);
                Count--;
            }
            else
            {
                leftChild.Keys[leftChild.Count] = Keys[index]; // The left child takes the splitting key of the parent
                Keys[index] = default;
                leftChild.Count++;

                Keys[index] = rightChild.Keys[0];

                // Copy all keys and children of the right child to the left child and clear them in the right child
                Array.Copy(rightChild.Keys, 0, leftChild.Keys, leftChild.Count, rightChild.Count);
                Array.Copy(rightChild.Children, 0, leftChild.Children, leftChild.Count, rightChild.Count + 1);
                leftChild.Count += rightChild.Count;
                Array.Clear(rightChild.Keys, 0, rightChild.Count);
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

        internal bool Get(TKey key, out TItem item)
        {
            if (IsLeaf)
            {
                int index = FindNextGreaterOrEqual(key);
                // This item is already exists update it
                if (index < Count)
                {
                    TKey currentKey = Keys[index];
                    int comparisonResult = key.CompareTo(currentKey);

                    if (comparisonResult == 0)
                    {
                        item = Items[index];
                        return true;
                    }
                }
            }
            else
            {
                (Node child, int index) = GetChild(key);

                return child.Get(key, out item);
            }

            item = default;
            return false;
        }

        internal bool GetMin(out KeyValuePair<TKey, TItem> min)
        {
            if (IsLeaf)
            {
                if (Count <= 0)
                {
                    min = default;
                    return false;
                }

                min = new(Keys[0], Items[0]);
                return true;
            }
            else
            {
                Node child = Children[0];
                return child.GetMin(out min);
            }
        }

        internal bool GetMax(out KeyValuePair<TKey, TItem> max)
        {
            if (IsLeaf)
            {
                if (Count <= 0)
                {
                    max = default;
                    return false;
                }

                max = new(Keys[Count - 1], Items[Count - 1]);
                return true;
            }
            else
            {
                Node child = Children[Count];
                return child.GetMax(out max);
            }
        }

        internal NearestItems GetNearest(TKey key)
        {
            if (Count <= 0)
            {
                return new(default, default, default);
            }

            if (IsLeaf)
            {
                int index = FindNextGreaterOrEqual(key);

                if (index < Count)
                {
                    TKey currentKey = Keys[index];
                    if (key.CompareTo(currentKey) == 0)
                    {
                        NearestItems nearestItems = new(new(currentKey, Items[index]), default, default);
                        return nearestItems;
                    }
                    else if (key.CompareTo(currentKey) < 0)
                    {
                        if (index > 0)
                        {
                            NearestItems nearestItems = new(default, new(Keys[index - 1], Items[index - 1]), new(currentKey, Items[index]));
                            return nearestItems;
                        }
                        else
                        {
                            NearestItems nearestItems = new(default, default, new(currentKey, Items[index]));
                            return nearestItems;
                        }
                    }
                    else // key > currentKey
                    {
                        if (index < Count - 1)
                        {
                            NearestItems nearestItems = new(default, new(currentKey, Items[index]), new(Keys[index + 1], Items[index + 1]));
                            return nearestItems;
                        }
                        else
                        {
                            NearestItems nearestItems = new(default, new(currentKey, Items[index]), default);
                            return nearestItems;
                        }
                    }
                }
                else
                {
                    NearestItems nearestItems = new(default, new(Keys[Count - 1], Items[Count - 1]), default);
                    return nearestItems;
                }
            }
            else
            {
                (Node child, int index) = GetChild(key);

                NearestItems nearestItems = child.GetNearest(key);

                if (nearestItems.Match.HasValue)
                {
                    return nearestItems;
                }
                else if (nearestItems.Lower.HasValue && nearestItems.Upper.HasValue)
                {
                    return nearestItems;
                }
                else if (nearestItems.Lower.HasValue)
                {
                    if (index < Count)
                    {
                        Node rightSibling = Children[index + 1];
                        NearestItems rightSiblingNearestItems = rightSibling.GetNearest(key);
                        NearestItems newNearestItems = new(default, nearestItems.Lower, rightSiblingNearestItems.Upper);
                        return newNearestItems;
                    }
                    else
                    {
                        return nearestItems;
                    }
                }
                else if (nearestItems.Upper.HasValue)
                {
                    if (index > 0)
                    {
                        Node leftSibling = Children[index - 1];
                        NearestItems leftSiblingNearestItems = leftSibling.GetNearest(key);
                        NearestItems newNearestItems = new(default, leftSiblingNearestItems.Lower, nearestItems.Upper);
                        return newNearestItems;
                    }
                    else
                    {
                        return nearestItems;
                    }
                }
            }

            return new(default, default, default);
        }

        internal bool DoForEach(Func<TKey, TItem, bool> actionAndCancelFunction, TKey minKey, TKey maxKey, bool maxInclusive)
        {
            int index = FindNextGreaterOrEqual(minKey);

            for (int i = index; i <= Count; i++)
            {
                if (IsLeaf)
                {
                    if (i < Count)
                    {
                        TKey currentKey = Keys[i];
                        int comparisonResult = maxKey.CompareTo(currentKey);

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

                        TItem currentItem = Items[i];
                        bool cancel = actionAndCancelFunction.Invoke(currentKey, currentItem);
                        if (cancel)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Node child = Children[i];
                    bool cancel = child.DoForEach(actionAndCancelFunction, minKey, maxKey, maxInclusive);
                    if (cancel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool DoForEach(Func<TKey, TItem, bool> actionAndCancelFunction)
        {
            for (int i = 0; i <= Count; i++)
            {
                if (IsLeaf)
                {
                    if (i < Count)
                    {
                        TKey key = Keys[i];
                        TItem item = Items[i];
                        bool cancel = actionAndCancelFunction.Invoke(key, item);
                        if (cancel)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    Node child = Children[i];
                    bool cancel = child.DoForEach(actionAndCancelFunction);
                    if (cancel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal IEnumerable<KeyValuePair<TKey, TItem>> GetRange(TKey minKey, TKey maxKey, bool maxInclusive)
        {
            int index = FindNextGreaterOrEqual(minKey);

            for (int i = index; i <= Count; i++)
            {
                if (IsLeaf)
                {
                    if (i < Count)
                    {
                        KeyValuePair<TKey, TItem> keyValuePair = new(Keys[i], Items[i]);
                        int comparisonResult = maxKey.CompareTo(keyValuePair.Key);

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

                        yield return keyValuePair;
                    }
                }
                else
                {
                    Node child = Children[i];
                    foreach (KeyValuePair<TKey, TItem> keyValuePair in child.GetRange(minKey, maxKey, maxInclusive))
                    {
                        yield return keyValuePair;
                    }
                }
            }
        }

        internal IEnumerable<KeyValuePair<TKey, TItem>> GetAll()
        {
            for (int i = 0; i <= Count; i++)
            {
                if (IsLeaf)
                {
                    if (i < Count)
                    {
                        KeyValuePair<TKey, TItem> keyValuePair = new(Keys[i], Items[i]);
                        yield return keyValuePair;
                    }
                }
                else
                {
                    Node child = Children[i];
                    foreach (KeyValuePair<TKey, TItem> keyValuePair in child.GetAll())
                    {
                        yield return keyValuePair;
                    }
                }
            }
        }
#if DEBUG

        internal void PrettyPrint(System.Text.StringBuilder builder, uint indentationCount)
        {
            for (int i = Count; i >= 0; i--)
            {
                if (IsLeaf)
                {
                    if (i < Count)
                    {
                        for (int t = 0; t < indentationCount; t++)
                        {
                            builder.Append("  ");
                        }
                        TKey key = Keys[i];
                        TItem item = Items[i];
                        builder.Append($"🍃 [{key}] {item}\n");
                    }
                    else if (Count == 0)
                    {
                        for (int t = 0; t < indentationCount; t++)
                        {
                            builder.Append("  ");
                        }
                        builder.Append($"🍃 -\n");
                    }
                }
                else
                {
                    if (i < Count)
                    {
                        for (int t = 0; t < indentationCount; t++)
                        {
                            builder.Append("  ");
                        }
                        TKey key = Keys[i];
                        builder.Append($"[{key}]\n");
                    }

                    Node child = Children[i];
                    child.PrettyPrint(builder, indentationCount + 1);
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

    public record struct NearestItems(KeyValuePair<TKey, TItem>? Match, KeyValuePair<TKey, TItem>? Lower, KeyValuePair<TKey, TItem>? Upper);

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
    /// <param name="key">The key that identifies the item</param>
    /// <param name="item">The item that shall be inserted or updated</param>
    /// <returns>true if an existing item was updated otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public bool InsertOrUpdate(TKey key, TItem item)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        Node.InsertResult insertResult = _Root.InsertOrUpdate(key, item);
        if (insertResult.Updated)
        {
            return true;
        }
        else if (insertResult.SplitResult.HasValue)
        {
            _Root = new(_Degree, _Root, insertResult.SplitResult.Value.NewRightNode, insertResult.SplitResult.Value.SplittingKey);
        }

        Count++;

        return false;
    }

    /// <summary>
    /// Removes the <see cref="item"> that is identified by the given <see cref="key"/>.
    /// </summary>
    /// <param name="key">The key that identifies the item/></param>
    /// <param name="item">The item that was removed</param>
    /// <returns>true if an item was removed otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool Remove(TKey key, out TItem item)
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
    /// <param name="min">The key value pair that was removed</param>
    /// <returns>true if an item was removed otherwise false</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool RemoveMin(out KeyValuePair<TKey, TItem> min)
    {
        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }

        bool removeResult = _Root.RemoveMin(out min);

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
    /// <param name="max">The key value pair that was removed</param>
    /// <returns>true if an item was removed otherwise false</returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Exception"></exception>
    public bool RemoveMax(out KeyValuePair<TKey, TItem> max)
    {
        if (_IterationCount > 0L)
        {
            throw new InvalidOperationException("Modifications during enumerations are not allowed.");
        }
        bool removeResult = _Root.RemoveMax(out max);

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
    /// Checks if a specific item that is identified by the given <paramref name="key"/> is contained.
    /// </summary>
    /// <param name="key">The key that identifies the item</param>
    /// <returns>true if the item exists otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Contains(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _Root.Get(key, out _);
    }

    /// <summary>
    /// Gets the <see cref="item"/> that is identified by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key that identifies the item</param>
    /// <param name="item">The item that is identified by the given <paramref name="key"/> if it exists.</param>
    /// <returns>true if the key is exists otherwise false</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Get(TKey key, out TItem item)
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
    /// <param name="min"></param>
    /// <returns>true if a minimum item exists otherwise false</returns>
    public bool GetMin(out KeyValuePair<TKey, TItem> min)
    {
        return _Root.GetMin(out min);
    }

    /// <summary>
    /// Gets the maximum item if it exists.
    /// </summary>
    /// <param name="max"></param>
    /// <returns>true if a maximum item exists otherwise false</returns>
    public bool GetMax(out KeyValuePair<TKey, TItem> max)
    {
        return _Root.GetMax(out max);
    }

    /// <summary>
    /// Gets the next greater item as <see cref="NearestItems.Upper"/> and the next lower item as <see cref="NearestItems.Lower"/> if existant. If the key equals an existing key only this item is returned an <see cref="NearestItems.Match"/>.
    /// </summary>
    /// <param name="key">The key that identifies the item</param>
    /// <returns>Gets the next greater item as <see cref="NearestItems.Upper"/> and the next lower item as <see cref="NearestItems.Lower"/> if existant. If the key equals an existing key only this item is returned an <see cref="NearestItems.Match"/>.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public NearestItems GetNearest(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _Root.GetNearest(key);
    }

    /// <summary>
    /// Performs an action for every single item within an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Use this over <see cref="GetRange(TKey, TKey, bool)"/> in performance critical paths.
    /// </summary>
    /// <param name="action">Function that will be called for every relevant item.</param>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void DoForEach(Action<TKey, TItem> action, TKey minKey, TKey maxKey, bool maxInclusive)
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

        if (minKey.CompareTo(maxKey) > 0)
        {
            return;
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);

            bool Action(TKey key, TItem item)
            {
                action.Invoke(key, item);
                return false;
            }

            _Root.DoForEach(Action, minKey, maxKey, maxInclusive);
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }
    }

    /// <summary>
    /// Performs an action for every single item within an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Use this over <see cref="GetRange(TKey, TKey, bool)"/> in performance critical paths.
    /// It offers the possibility to cancel the iteration.
    /// </summary>
    /// <param name="actionAndCancelFunction">Function that will be called for every relevant item. It returns true to cancel or false to continue.</param>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void DoForEach(Func<TKey, TItem, bool> actionAndCancelFunction, TKey minKey, TKey maxKey, bool maxInclusive)
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

        if (minKey.CompareTo(maxKey) > 0)
        {
            return;
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
    public void DoForEach(Action<TKey, TItem> action)
    {
        if (action == null)
        {
            return;
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);

            bool Action(TKey key, TItem item)
            {
                action.Invoke(key, item);
                return false;
            }

            _Root.DoForEach(Action);
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
    public void DoForEach(Func<TKey, TItem, bool> actionAndCancelFunction)
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
    /// Gets a range of items as key value pair limited by an inclusive lower limit (<paramref name="minKey"/>) and an upper limit (<paramref name="maxKey"/>). 
    /// The upper limit is inclusive if <paramref name="maxInclusive"/> is true otherwise the upper limit is exclusive.
    /// Consider using <see cref="DoForEach(Action{TKey, TItem}, TKey, TKey, bool)"/> if performance is critical.
    /// </summary>
    /// <param name="minKey">Inclusive lower limit</param>
    /// <param name="maxKey">Upper limit</param>
    /// <param name="maxInclusive">The upper limit is inclusive if true otherwise the upper limit is exclusive</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IEnumerable<KeyValuePair<TKey, TItem>> GetRange(TKey minKey, TKey maxKey, bool maxInclusive)
    {
        if (minKey is null)
        {
            throw new ArgumentNullException(nameof(minKey));
        }

        if (maxKey is null)
        {
            throw new ArgumentNullException(nameof(maxKey));
        }

        if (minKey.CompareTo(maxKey) > 0)
        {
            yield break;
        }

        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            foreach (KeyValuePair<TKey, TItem> keyValuePair in _Root.GetRange(minKey, maxKey, maxInclusive))
            {
                yield return keyValuePair;
            }
        }
        finally
        {
            Interlocked.Add(ref _IterationCount, -1);
        }

    }

    /// <summary>
    /// Gets all items as key value pair. Consider using <see cref="DoForEach(Action{TKey, TItem})"/> if performance is critical.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TKey, TItem>> GetAll()
    {
        try
        {
            Interlocked.Add(ref _IterationCount, 1);
            foreach (KeyValuePair<TKey, TItem> keyValuePair in _Root.GetAll())
            {
                yield return keyValuePair;
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