# BTree

BTree is a simple and high performant BTree for C# and .NET.

<div align="center">
    <img 
        src="./icon.jpeg" 
        width="256" 
        height="256">
</div>

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
While the number of items `N` increases from 1_000 to 1_000_000_000 by a factor of 1_000_000 the average duration of the benchmark function `BTree()` only increases by a factor of 7.46.


```CSharp
[Benchmark]
public void BTree()
{
    int index = _Random.Next(Items.Length);
    int item = Items[index];
    Tree.Get(item, out int _);
    Tree.Remove(item, out int _);
    Tree.InsertOrUpdate(item);
}
```

| Method | N             | Mean       | Error    | StdDev   |
|------- |-------------- |-----------:|---------:|---------:|
| BTree  | 1_000         |   183.7 ns |  0.46 ns |  0.39 ns |
| BTree  | 10_000        |   253.4 ns |  0.74 ns |  0.69 ns |
| BTree  | 100_000       |   323.1 ns |  0.69 ns |  0.65 ns |
| BTree  | 1_000_000     |   386.5 ns |  1.26 ns |  1.05 ns |
| BTree  | 10_000_000    |   635.0 ns |  4.86 ns |  4.55 ns |
| BTree  | 100_000_000   |   848.4 ns | 10.46 ns |  9.79 ns |
| BTree  | 1_000_000_000 | 1,370.8 ns | 27.06 ns | 25.31 ns |

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
