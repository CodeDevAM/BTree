# BTree

BTree offers a simple and high performant implementation of a BTree and a B+Tree (BPlusTree) for C# and .NET.


<img src="https://github.com/CodeDevAM/BTree/blob/main/icon.jpeg" width="256" height="256"/>

## Usage
Items of type `T` that shall be stored in the `BTree<T>` must implement the interface `IComparable<T>`. In this case the usage is straight forward.

```CSharp
record struct IntItem(int Value) : IComparable<IntItem>
{
    public readonly int CompareTo(IntItem other) => Value < other.Value ? -1 : Value > other.Value ? 1 : 0;

    // Add an implicit cast from int to simplify the example
    public static implicit operator IntItem(int value) => new(value);
}

BTree<IntItem> tree = new();

bool itemAlreadyExisted = tree.InsertOrUpdate(5);

bool contains = tree.Contains(5);

// Do a range query
foreach(IntItem item in tree.GetRange(5, 5))
{
    Console.WriteLine($"Item: {item}");
}
// or
tree.DoForEach(item => Console.WriteLine($"Item: {item}"), 5, 5);
// or
List<IntItem> range = [];
tree.DoForEach(range.Add, 5, 5);
foreach(IntItem item in range)
{
    Console.WriteLine($"Item: {item}");
}

```

If the items shall be stored with a dedicated key or the type of the item does not implement `IComparable<T>` then you could wrap it within a simple struct. The following example illustrates how to store some strings with associated integer keys.

```CSharp
// Wrap the string value as an key value pair called Item
record struct Item(int Key, string Value) : IComparable<Item>
{
    public readonly int CompareTo(Item other) => Key < other.Key ? -1 : Key > other.Key ? 1 : 0;
}

// Reduced "key only" version of Item
record struct KeyOnly(int Key) : IComparable<Item>
{
    public readonly int CompareTo(Item other) => Key < other.Key ? -1 : Key > other.Key ? 1 : 0;
}

BTree<Item> tree = new();

Item item = new(5, "BTree");
KeyOnly key = new(item.Key);

bool itemAlreadyExisted = tree.InsertOrUpdate(item);

bool contains = tree.Contains(item);
//or
contains = tree.Contains(key);

bool exists = tree.Get(key, out Item existingItem);
// or if you don't want to use KeyOnly
Item keyOnlyItem = new(5, null);
exists = tree.Get(keyOnlyItem, out existingItem);
```

## Performance

BTree offers a time complexity of `O(log N)` for insertion, retrieval and removal.

The following simple benchmark combines a `Get()`, a `Remove()` and an `InsertOrUpdate()` for a different number of items `N`.
While the number of items `N` increases from 1_000 to 1_000_000_000 by a factor of 1_000_000 the average duration of the benchmark function `BTree()` only increases by a factor less than 10.
The `BPlusTree` scales even better for a huge number of items as the average duration of the benchmark function `BPlusTree()` only increases by a factor less than 5.


```CSharp
[Benchmark]
public void BTree()
{
    int index = _Random.Next(_Items.Length);
    int item = _Items[index];
    _BTree.Get(item, out int _);
    _BTree.Remove(item, out int _);
    _BTree.InsertOrUpdate(item);
}

[Benchmark]
public void BPlusTree()
{
    int index = _Random.Next(_Items.Length);
    int item = _Items[index];
    _BPlusTree.Get(item, out int _);
    _BPlusTree.Remove(item, out int _);
    _BPlusTre
}
```

| Method    | N             | Mean       | Error    | StdDev   |
|---------- |-------------- |-----------:|---------:|---------:|
| BTree     | 100           |   107.0 ns |  0.61 ns |  0.58 ns |
| BPlusTree | 100           |   112.9 ns |  0.89 ns |  0.84 ns |
| BTree     | 1_000         |   179.3 ns |  0.74 ns |  0.69 ns |
| BPlusTree | 1_000         |   187.1 ns |  0.71 ns |  0.63 ns |
| BTree     | 10_000        |   258.0 ns |  1.31 ns |  1.23 ns |
| BPlusTree | 10_000        |   261.6 ns |  0.90 ns |  0.84 ns |
| BTree     | 100_000       |   320.0 ns |  2.03 ns |  1.80 ns |
| BPlusTree | 100_000       |   317.3 ns |  1.23 ns |  1.09 ns |
| BTree     | 1_000_000     |   411.2 ns |  7.14 ns |  5.97 ns |
| BPlusTree | 1_000_000     |   424.4 ns |  5.38 ns |  4.49 ns |
| BTree     | 10_000_000    |   631.6 ns |  2.85 ns |  2.52 ns |
| BPlusTree | 10_000_000    |   672.4 ns |  5.25 ns |  4.66 ns |
| BTree     | 100_000_000   |   837.4 ns |  3.95 ns |  3.69 ns |
| BPlusTree | 100_000_000   |   799.8 ns | 15.93 ns | 35.29 ns |
| BTree     | 1_000_000_000 | 1,552.1 ns | 29.94 ns | 39.96 ns |
| BPlusTree | 1_000_000_000 |   878.6 ns | 17.56 ns | 19.52 ns |


## Contribution

Contributions are welcome.

## License

MIT License

Copyright (c) 2024 DevAM

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
