namespace BTree.Test
{
    internal record Ref<T>(T Value) : IComparable<Ref<T>>, IComparable<T> where T : IComparable<T>
    {
        public int CompareTo(T other)
        {
            return Value.CompareTo(other);
        }

        public int CompareTo(Ref<T> other)
        {
            return Value.CompareTo(other.Value);
        }

        public static implicit operator T(Ref<T> _ref) => _ref.Value;
        public static implicit operator Ref<T>(T value) => new(value);

        public override string ToString()
        {
            return Value?.ToString();
        }
    }
}
