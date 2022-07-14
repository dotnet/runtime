// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Collections.Immutable
{
    public partial interface IImmutableDictionary<TKey, TValue> : System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.IEnumerable
    {
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Clear();
        bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> Remove(TKey key);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value);
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items);
        bool TryGetKey(TKey equalKey, out TKey actualKey);
    }
    public partial interface IImmutableList<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.IEnumerable
    {
        System.Collections.Immutable.IImmutableList<T> Add(T value);
        System.Collections.Immutable.IImmutableList<T> AddRange(System.Collections.Generic.IEnumerable<T> items);
        System.Collections.Immutable.IImmutableList<T> Clear();
        int IndexOf(T item, int index, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer);
        System.Collections.Immutable.IImmutableList<T> Insert(int index, T element);
        System.Collections.Immutable.IImmutableList<T> InsertRange(int index, System.Collections.Generic.IEnumerable<T> items);
        int LastIndexOf(T item, int index, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer);
        System.Collections.Immutable.IImmutableList<T> Remove(T value, System.Collections.Generic.IEqualityComparer<T>? equalityComparer);
        System.Collections.Immutable.IImmutableList<T> RemoveAll(System.Predicate<T> match);
        System.Collections.Immutable.IImmutableList<T> RemoveAt(int index);
        System.Collections.Immutable.IImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer);
        System.Collections.Immutable.IImmutableList<T> RemoveRange(int index, int count);
        System.Collections.Immutable.IImmutableList<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer);
        System.Collections.Immutable.IImmutableList<T> SetItem(int index, T value);
    }
    public partial interface IImmutableQueue<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        bool IsEmpty { get; }
        System.Collections.Immutable.IImmutableQueue<T> Clear();
        System.Collections.Immutable.IImmutableQueue<T> Dequeue();
        System.Collections.Immutable.IImmutableQueue<T> Enqueue(T value);
        T Peek();
    }
    public partial interface IImmutableSet<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.IEnumerable
    {
        System.Collections.Immutable.IImmutableSet<T> Add(T value);
        System.Collections.Immutable.IImmutableSet<T> Clear();
        bool Contains(T value);
        System.Collections.Immutable.IImmutableSet<T> Except(System.Collections.Generic.IEnumerable<T> other);
        System.Collections.Immutable.IImmutableSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other);
        bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other);
        bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other);
        bool Overlaps(System.Collections.Generic.IEnumerable<T> other);
        System.Collections.Immutable.IImmutableSet<T> Remove(T value);
        bool SetEquals(System.Collections.Generic.IEnumerable<T> other);
        System.Collections.Immutable.IImmutableSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other);
        bool TryGetValue(T equalValue, out T actualValue);
        System.Collections.Immutable.IImmutableSet<T> Union(System.Collections.Generic.IEnumerable<T> other);
    }
    public partial interface IImmutableStack<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable
    {
        bool IsEmpty { get; }
        System.Collections.Immutable.IImmutableStack<T> Clear();
        T Peek();
        System.Collections.Immutable.IImmutableStack<T> Pop();
        System.Collections.Immutable.IImmutableStack<T> Push(T value);
    }
    public static partial class ImmutableArray
    {
        public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, int index, int length, T value) { throw null; }
        public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, int index, int length, T value, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value) { throw null; }
        public static int BinarySearch<T>(this System.Collections.Immutable.ImmutableArray<T> array, T value, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T>.Builder CreateBuilder<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T>.Builder CreateBuilder<T>(int initialCapacity) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, int start, int length, System.Func<TSource, TResult> selector) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, System.Func<TSource, TArg, TResult> selector, TArg arg) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(System.Collections.Immutable.ImmutableArray<TSource> items, int start, int length, System.Func<TSource, TArg, TResult> selector, TArg arg) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.Collections.Immutable.ImmutableArray<T> items, int start, int length) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T item1, T item2, T item3, T item4) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(params T[]? items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(T[] items, int start, int length) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.ReadOnlySpan<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> Create<T>(System.Span<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this System.Collections.Generic.IEnumerable<TSource> items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<TSource> ToImmutableArray<TSource>(this System.Collections.Immutable.ImmutableArray<TSource>.Builder builder) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this System.ReadOnlySpan<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> ToImmutableArray<T>(this System.Span<T> items) { throw null; }
    }
    public readonly partial struct ImmutableArray<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Immutable.IImmutableList<T>, System.Collections.IStructuralComparable, System.Collections.IStructuralEquatable, System.IEquatable<System.Collections.Immutable.ImmutableArray<T>>
    {
        private readonly T[] array;
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly System.Collections.Immutable.ImmutableArray<T> Empty;
        public bool IsDefault { get { throw null; } }
        public bool IsDefaultOrEmpty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        int System.Collections.Generic.ICollection<T>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        int System.Collections.Generic.IReadOnlyCollection<T>.Count { get { throw null; } }
        T System.Collections.Generic.IReadOnlyList<T>.this[int index] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public System.Collections.Immutable.ImmutableArray<T> Add(T item) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(System.Collections.Immutable.ImmutableArray<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(T[] items, int length) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(TDerived[] items) where TDerived : T { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(ImmutableArray<T> items, int length) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange<TDerived>(ImmutableArray<TDerived> items) where TDerived : T { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(System.ReadOnlySpan<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> AddRange(params T[] items) { throw null; }
        public System.ReadOnlyMemory<T> AsMemory() { throw null; }
        public System.ReadOnlySpan<T> AsSpan() { throw null; }
        public System.ReadOnlySpan<T> AsSpan(int start, int length) { throw null; }
        public System.Collections.Immutable.ImmutableArray<
#nullable disable
            TOther
#nullable restore
            > As<TOther>() where TOther : class? { throw null; }
        public System.Collections.Immutable.ImmutableArray<
#nullable disable
            TOther
#nullable restore
            > CastArray<TOther>() where TOther : class? { throw null; }
        public static System.Collections.Immutable.ImmutableArray<
#nullable disable
            T
#nullable restore
            > CastUp<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items) where TDerived : class?, T { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Clear() { throw null; }
        public bool Contains(T item) { throw null; }
        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) { }
        public void CopyTo(T[] destination) { }
        public void CopyTo(T[] destination, int destinationIndex) { }
        public void CopyTo(System.Span<T> destination) { }
        public bool Equals(System.Collections.Immutable.ImmutableArray<T> other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        public int IndexOf(T item) { throw null; }
        public int IndexOf(T item, int startIndex) { throw null; }
        public int IndexOf(T item, int startIndex, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public int IndexOf(T item, int startIndex, int count) { throw null; }
        public int IndexOf(T item, int startIndex, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Insert(int index, T item) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, System.Collections.Immutable.ImmutableArray<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, T[] items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> InsertRange(int index, System.ReadOnlySpan<T> items) { throw null; }
        public ref readonly T ItemRef(int index) { throw null; }
        public int LastIndexOf(T item) { throw null; }
        public int LastIndexOf(T item, int startIndex) { throw null; }
        public int LastIndexOf(T item, int startIndex, int count) { throw null; }
        public int LastIndexOf(T item, int startIndex, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Generic.IEnumerable<TResult> OfType<TResult>() { throw null; }
        public static bool operator ==(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right) { throw null; }
        public static bool operator ==(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right) { throw null; }
        public static bool operator !=(System.Collections.Immutable.ImmutableArray<T> left, System.Collections.Immutable.ImmutableArray<T> right) { throw null; }
        public static bool operator !=(System.Collections.Immutable.ImmutableArray<T>? left, System.Collections.Immutable.ImmutableArray<T>? right) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Remove(T item) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Remove(T item, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveAll(System.Predicate<T> match) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveAt(int index) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.Collections.Immutable.ImmutableArray<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(int index, int length) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(System.ReadOnlySpan<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer = null) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> RemoveRange(T[] items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer = null) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> SetItem(int index, T item) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Slice(int start, int length) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Sort() { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Sort(System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Sort(System.Comparison<T> comparison) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T> Sort(int index, int count, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Add(T value) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.AddRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Insert(int index, T element) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Remove(T value, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveAll(System.Predicate<T> match) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveAt(int index) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveRange(int index, int count) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.SetItem(int index, T value) { throw null; }
        int System.Collections.IStructuralComparable.CompareTo(object? other, System.Collections.IComparer comparer) { throw null; }
        bool System.Collections.IStructuralEquatable.Equals(object? other, System.Collections.IEqualityComparer comparer) { throw null; }
        int System.Collections.IStructuralEquatable.GetHashCode(System.Collections.IEqualityComparer comparer) { throw null; }
        public System.Collections.Immutable.ImmutableArray<T>.Builder ToBuilder() { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.IEnumerable
        {
            internal Builder() { }
            public int Capacity { get { throw null; } set { } }
            public int Count { get { throw null; } set { } }
            public T this[int index] { get { throw null; } set { } }
            bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
            public void Add(T item) { }
            public void AddRange(System.Collections.Generic.IEnumerable<T> items) { }
            public void AddRange(System.Collections.Immutable.ImmutableArray<T> items) { }
            public void AddRange(System.Collections.Immutable.ImmutableArray<T> items, int length) { }
            public void AddRange(System.Collections.Immutable.ImmutableArray<T>.Builder items) { }
            public void AddRange(params T[] items) { }
            public void AddRange(T[] items, int length) { }
            public void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived> items) where TDerived : T { }
            public void AddRange<TDerived>(System.Collections.Immutable.ImmutableArray<TDerived>.Builder items) where TDerived : T { }
            public void AddRange<TDerived>(TDerived[] items) where TDerived : T { }
            public void AddRange(System.ReadOnlySpan<T> items) { }
            public void AddRange<TDerived>(System.ReadOnlySpan<TDerived> items) where TDerived : T { }
            public void Clear() { }
            public bool Contains(T item) { throw null; }
            public void CopyTo(T[] array, int index) { }
            public void CopyTo(T[] destination) { throw null; }
            public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int length) { throw null; }
            public void CopyTo(System.Span<T> destination) { }
            public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
            public int IndexOf(T item) { throw null; }
            public int IndexOf(T item, int startIndex) { throw null; }
            public int IndexOf(T item, int startIndex, int count) { throw null; }
            public int IndexOf(T item, int startIndex, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public int IndexOf(T item, int startIndex, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Insert(int index, T item) { }
            public void InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { throw null; }
            public void InsertRange(int index, ImmutableArray<T> items) { throw null; }
            public ref readonly T ItemRef(int index) { throw null; }
            public int LastIndexOf(T item) { throw null; }
            public int LastIndexOf(T item, int startIndex) { throw null; }
            public int LastIndexOf(T item, int startIndex, int count) { throw null; }
            public int LastIndexOf(T item, int startIndex, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public System.Collections.Immutable.ImmutableArray<T> MoveToImmutable() { throw null; }
            public bool Remove(T element) { throw null; }
            public bool Remove(T element, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void RemoveAll(Predicate<T> match) { throw null; }
            public void RemoveAt(int index) { }
            public void RemoveRange(int index, int length) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Replace(T oldValue, T newValue) { throw null; }
            public void Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Reverse() { }
            public void Sort() { }
            public void Sort(System.Collections.Generic.IComparer<T>? comparer) { }
            public void Sort(System.Comparison<T> comparison) { }
            public void Sort(int index, int count, System.Collections.Generic.IComparer<T>? comparer) { }
            System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public T[] ToArray() { throw null; }
            public System.Collections.Immutable.ImmutableArray<T> ToImmutable() { throw null; }
        }
        public partial struct Enumerator
        {
            private readonly T[] _array;
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class ImmutableDictionary
    {
        public static bool Contains<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> map, TKey key, TValue value) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>() where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static TValue? GetValueOrDefault<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull { throw null; }
        public static TValue GetValueOrDefault<TKey, TValue>(this System.Collections.Immutable.IImmutableDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Builder builder) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
    }
    public sealed partial class ImmutableDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableDictionary<TKey, TValue> where TKey : notnull
    {
        internal ImmutableDictionary() { }
        public static readonly System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Empty;
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } }
        public System.Collections.Generic.IEqualityComparer<TKey> KeyComparer { get { throw null; } }
        public System.Collections.Generic.IEnumerable<TKey> Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
        TValue System.Collections.Generic.IDictionary<TKey,TValue>.this[TKey key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get { throw null; } }
        public System.Collections.Generic.IEnumerable<TValue> Values { get { throw null; } }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Add(TKey key, TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Clear() { throw null; }
        public bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair) { throw null; }
        public bool ContainsKey(TKey key) { throw null; }
        public bool ContainsValue(TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> Remove(TKey key) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Clear() { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.IDictionary<TKey,TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        void System.Collections.IDictionary.Add(object key, object? value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Add(TKey key, TValue value) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.SetItem(TKey key, TValue value) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Builder ToBuilder() { throw null; }
        public bool TryGetKey(TKey equalKey, out TKey actualKey) { throw null; }
        public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer) { throw null; }
        public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IEqualityComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
        {
            internal Builder() { }
            public int Count { get { throw null; } }
            public TValue this[TKey key] { get { throw null; } set { } }
            public System.Collections.Generic.IEqualityComparer<TKey> KeyComparer { get { throw null; } set { } }
            public System.Collections.Generic.IEnumerable<TKey> Keys { get { throw null; } }
            bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
            System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
            System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
            bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
            object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
            System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
            System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
            public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get { throw null; } set { } }
            public System.Collections.Generic.IEnumerable<TValue> Values { get { throw null; } }
            public void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
            public void Add(TKey key, TValue value) { }
            public void AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { }
            public void Clear() { }
            public bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
            public bool ContainsKey(TKey key) { throw null; }
            public bool ContainsValue(TValue value) { throw null; }
            public System.Collections.Immutable.ImmutableDictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
            public TValue? GetValueOrDefault(TKey key) { throw null; }
            public TValue GetValueOrDefault(TKey key, TValue defaultValue) { throw null; }
            public bool Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
            public bool Remove(TKey key) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { }
            void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
            System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
            void System.Collections.IDictionary.Add(object key, object? value) { }
            bool System.Collections.IDictionary.Contains(object key) { throw null; }
            System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
            void System.Collections.IDictionary.Remove(object key) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public System.Collections.Immutable.ImmutableDictionary<TKey, TValue> ToImmutable() { throw null; }
            public bool TryGetKey(TKey equalKey, out TKey actualKey) { throw null; }
            public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
        }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public static partial class ImmutableHashSet
    {
        public static System.Collections.Immutable.ImmutableHashSet<T>.Builder CreateBuilder<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T>.Builder CreateBuilder<T>(System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> CreateRange<T>(System.Collections.Generic.IEqualityComparer<T>? equalityComparer, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T>? equalityComparer, T item) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(System.Collections.Generic.IEqualityComparer<T>? equalityComparer, params T[] items) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<T> Create<T>(params T[] items) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IEqualityComparer<TSource>? equalityComparer) { throw null; }
        public static System.Collections.Immutable.ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this System.Collections.Immutable.ImmutableHashSet<TSource>.Builder builder) { throw null; }
    }

#if NETCOREAPP
    public sealed partial class ImmutableHashSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableSet<T>
#else
    public sealed partial class ImmutableHashSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableSet<T>
#endif
    {
        internal ImmutableHashSet() { }
        public static readonly System.Collections.Immutable.ImmutableHashSet<T> Empty;
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Collections.Generic.IEqualityComparer<T> KeyComparer { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public System.Collections.Immutable.ImmutableHashSet<T> Add(T item) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> Clear() { throw null; }
        public bool Contains(T item) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> Except(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T>.Enumerator GetEnumerator() { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> Remove(T item) { throw null; }
        public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        bool System.Collections.Generic.ISet<T>.Add(T item) { throw null; }
        void System.Collections.Generic.ISet<T>.ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Add(T item) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Except(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Intersect(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Remove(T item) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.SymmetricExcept(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Union(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T>.Builder ToBuilder() { throw null; }
        public bool TryGetValue(T equalValue, out T actualValue) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> Union(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableHashSet<T> WithComparer(System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.IEnumerable
        {
            internal Builder() { }
            public int Count { get { throw null; } }
            public System.Collections.Generic.IEqualityComparer<T> KeyComparer { get { throw null; } set { } }
            bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
            public bool Add(T item) { throw null; }
            public void Clear() { }
            public bool Contains(T item) { throw null; }
            public void ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
            public System.Collections.Immutable.ImmutableHashSet<T>.Enumerator GetEnumerator() { throw null; }
            public void IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
            public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool Remove(T item) { throw null; }
            public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
            public bool TryGetValue(T equalValue, out T actualValue) { throw null; }
            void System.Collections.Generic.ICollection<T>.Add(T item) { }
            void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
            System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public System.Collections.Immutable.ImmutableHashSet<T> ToImmutable() { throw null; }
            public void UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        }
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            object? System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public static partial class ImmutableInterlocked
    {
        public static TValue AddOrUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, System.Func<TKey, TValue> addValueFactory, System.Func<TKey, TValue, TValue> updateValueFactory) where TKey : notnull { throw null; }
        public static TValue AddOrUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue addValue, System.Func<TKey, TValue, TValue> updateValueFactory) where TKey : notnull { throw null; }
        public static void Enqueue<T>(ref System.Collections.Immutable.ImmutableQueue<T> location, T value) { }
        public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, System.Func<TKey, TValue> valueFactory) where TKey : notnull { throw null; }
        public static TValue GetOrAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue value) where TKey : notnull { throw null; }
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, System.Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> InterlockedCompareExchange<T>(ref System.Collections.Immutable.ImmutableArray<T> location, System.Collections.Immutable.ImmutableArray<T> value, System.Collections.Immutable.ImmutableArray<T> comparand) { throw null; }
        public static System.Collections.Immutable.ImmutableArray<T> InterlockedExchange<T>(ref System.Collections.Immutable.ImmutableArray<T> location, System.Collections.Immutable.ImmutableArray<T> value) { throw null; }
        public static bool InterlockedInitialize<T>(ref System.Collections.Immutable.ImmutableArray<T> location, System.Collections.Immutable.ImmutableArray<T> value) { throw null; }
        public static void Push<T>(ref System.Collections.Immutable.ImmutableStack<T> location, T value) { }
        public static bool TryAdd<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue value) where TKey : notnull { throw null; }
        public static bool TryDequeue<T>(ref System.Collections.Immutable.ImmutableQueue<T> location, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out T value) { throw null; }
        public static bool TryPop<T>(ref System.Collections.Immutable.ImmutableStack<T> location, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out T value) { throw null; }
        public static bool TryRemove<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) where TKey : notnull { throw null; }
        public static bool TryUpdate<TKey, TValue>(ref System.Collections.Immutable.ImmutableDictionary<TKey, TValue> location, TKey key, TValue newValue, TValue comparisonValue) where TKey : notnull { throw null; }
        public static bool Update<T>(ref T location, System.Func<T, T> transformer) where T : class? { throw null; }
        public static bool Update<T, TArg>(ref T location, System.Func<T, TArg, T> transformer, TArg transformerArgument) where T : class? { throw null; }
        public static bool Update<T>(ref System.Collections.Immutable.ImmutableArray<T> location, Func<System.Collections.Immutable.ImmutableArray<T>, System.Collections.Immutable.ImmutableArray<T>> transformer) { throw null; }
        public static bool Update<T, TArg>(ref System.Collections.Immutable.ImmutableArray<T> location, Func<System.Collections.Immutable.ImmutableArray<T>, TArg, System.Collections.Immutable.ImmutableArray<T>> transformer, TArg transformerArgument) { throw null; }
    }
    public static partial class ImmutableList
    {
        public static System.Collections.Immutable.ImmutableList<T>.Builder CreateBuilder<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableList<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableList<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableList<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableList<T> Create<T>(params T[] items) { throw null; }
        public static int IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item) { throw null; }
        public static int IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public static int IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, int startIndex) { throw null; }
        public static int IndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, int startIndex, int count) { throw null; }
        public static int LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item) { throw null; }
        public static int LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public static int LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, int startIndex) { throw null; }
        public static int LastIndexOf<T>(this System.Collections.Immutable.IImmutableList<T> list, T item, int startIndex, int count) { throw null; }
        public static System.Collections.Immutable.IImmutableList<T> RemoveRange<T>(this System.Collections.Immutable.IImmutableList<T> list, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.IImmutableList<T> Remove<T>(this System.Collections.Immutable.IImmutableList<T> list, T value) { throw null; }
        public static System.Collections.Immutable.IImmutableList<T> Replace<T>(this System.Collections.Immutable.IImmutableList<T> list, T oldValue, T newValue) { throw null; }
        public static System.Collections.Immutable.ImmutableList<TSource> ToImmutableList<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Immutable.ImmutableList<TSource> ToImmutableList<TSource>(this System.Collections.Immutable.ImmutableList<TSource>.Builder builder) { throw null; }
    }
    public sealed partial class ImmutableList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Immutable.IImmutableList<T>
    {
        internal ImmutableList() { }
        public static readonly System.Collections.Immutable.ImmutableList<T> Empty;
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public System.Collections.Immutable.ImmutableList<T> Add(T value) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> AddRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public int BinarySearch(int index, int count, T item, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public int BinarySearch(T item) { throw null; }
        public int BinarySearch(T item, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Clear() { throw null; }
        public bool Contains(T value) { throw null; }
        public System.Collections.Immutable.ImmutableList<TOutput> ConvertAll<TOutput>(System.Func<T, TOutput> converter) { throw null; }
        public void CopyTo(int index, T[] array, int arrayIndex, int count) { }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int arrayIndex) { }
        public bool Exists(System.Predicate<T> match) { throw null; }
        public T? Find(System.Predicate<T> match) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> FindAll(System.Predicate<T> match) { throw null; }
        public int FindIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
        public int FindIndex(int startIndex, System.Predicate<T> match) { throw null; }
        public int FindIndex(System.Predicate<T> match) { throw null; }
        public T? FindLast(System.Predicate<T> match) { throw null; }
        public int FindLastIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
        public int FindLastIndex(int startIndex, System.Predicate<T> match) { throw null; }
        public int FindLastIndex(System.Predicate<T> match) { throw null; }
        public void ForEach(System.Action<T> action) { }
        public System.Collections.Immutable.ImmutableList<T>.Enumerator GetEnumerator() { throw null; }
        public System.Collections.Immutable.ImmutableList<T> GetRange(int index, int count) { throw null; }
        public int IndexOf(T value) { throw null; }
        public int IndexOf(T item, int index, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Insert(int index, T item) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public ref readonly T ItemRef(int index) { throw null; }
        public int LastIndexOf(T item, int index, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Remove(T value) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Remove(T value, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> RemoveAll(System.Predicate<T> match) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> RemoveAt(int index) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> RemoveRange(int index, int count) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Replace(T oldValue, T newValue) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Reverse() { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Reverse(int index, int count) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> SetItem(int index, T value) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Sort() { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Sort(System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Sort(System.Comparison<T> comparison) { throw null; }
        public System.Collections.Immutable.ImmutableList<T> Sort(int index, int count, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Add(T value) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.AddRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Insert(int index, T item) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Remove(T value, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveAll(System.Predicate<T> match) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveAt(int index) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.RemoveRange(int index, int count) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
        System.Collections.Immutable.IImmutableList<T> System.Collections.Immutable.IImmutableList<T>.SetItem(int index, T value) { throw null; }
        public System.Collections.Immutable.ImmutableList<T>.Builder ToBuilder() { throw null; }
        public bool TrueForAll(System.Predicate<T> match) { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
        {
            internal Builder() { }
            public int Count { get { throw null; } }
            public T this[int index] { get { throw null; } set { } }
            bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            bool System.Collections.IList.IsFixedSize { get { throw null; } }
            bool System.Collections.IList.IsReadOnly { get { throw null; } }
            object? System.Collections.IList.this[int index] { get { throw null; } set { } }
            public void Add(T item) { }
            public void AddRange(System.Collections.Generic.IEnumerable<T> items) { }
            public int BinarySearch(int index, int count, T item, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
            public int BinarySearch(T item) { throw null; }
            public int BinarySearch(T item, System.Collections.Generic.IComparer<T>? comparer) { throw null; }
            public void Clear() { }
            public bool Contains(T item) { throw null; }
            public System.Collections.Immutable.ImmutableList<TOutput> ConvertAll<TOutput>(System.Func<T, TOutput> converter) { throw null; }
            public void CopyTo(int index, T[] array, int arrayIndex, int count) { }
            public void CopyTo(T[] array) { }
            public void CopyTo(T[] array, int arrayIndex) { }
            public bool Exists(System.Predicate<T> match) { throw null; }
            public T? Find(System.Predicate<T> match) { throw null; }
            public System.Collections.Immutable.ImmutableList<T> FindAll(System.Predicate<T> match) { throw null; }
            public int FindIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
            public int FindIndex(int startIndex, System.Predicate<T> match) { throw null; }
            public int FindIndex(System.Predicate<T> match) { throw null; }
            public T? FindLast(System.Predicate<T> match) { throw null; }
            public int FindLastIndex(int startIndex, int count, System.Predicate<T> match) { throw null; }
            public int FindLastIndex(int startIndex, System.Predicate<T> match) { throw null; }
            public int FindLastIndex(System.Predicate<T> match) { throw null; }
            public void ForEach(System.Action<T> action) { }
            public System.Collections.Immutable.ImmutableList<T>.Enumerator GetEnumerator() { throw null; }
            public System.Collections.Immutable.ImmutableList<T> GetRange(int index, int count) { throw null; }
            public int IndexOf(T item) { throw null; }
            public int IndexOf(T item, int index) { throw null; }
            public int IndexOf(T item, int index, int count) { throw null; }
            public int IndexOf(T item, int index, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Insert(int index, T item) { }
            public void InsertRange(int index, System.Collections.Generic.IEnumerable<T> items) { }
            public ref readonly T ItemRef(int index) { throw null; }
            public int LastIndexOf(T item) { throw null; }
            public int LastIndexOf(T item, int startIndex) { throw null; }
            public int LastIndexOf(T item, int startIndex, int count) { throw null; }
            public int LastIndexOf(T item, int startIndex, int count, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public bool Remove(T item) { throw null; }
            public bool Remove(T item, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public int RemoveAll(System.Predicate<T> match) { throw null; }
            public void RemoveAt(int index) { }
            public void RemoveRange(int index, int count) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<T> items) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<T> items, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Replace(T oldValue, T newValue) { throw null; }
            public void Replace(T oldValue, T newValue, System.Collections.Generic.IEqualityComparer<T>? equalityComparer) { throw null; }
            public void Reverse() { }
            public void Reverse(int index, int count) { }
            public void Sort() { }
            public void Sort(System.Collections.Generic.IComparer<T>? comparer) { }
            public void Sort(System.Comparison<T> comparison) { }
            public void Sort(int index, int count, System.Collections.Generic.IComparer<T>? comparer) { }
            System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            int System.Collections.IList.Add(object? value) { throw null; }
            void System.Collections.IList.Clear() { }
            bool System.Collections.IList.Contains(object? value) { throw null; }
            int System.Collections.IList.IndexOf(object? value) { throw null; }
            void System.Collections.IList.Insert(int index, object? value) { }
            void System.Collections.IList.Remove(object? value) { }
            public System.Collections.Immutable.ImmutableList<T> ToImmutable() { throw null; }
            public bool TrueForAll(System.Predicate<T> match) { throw null; }
        }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            object? System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public static partial class ImmutableQueue
    {
        public static System.Collections.Immutable.ImmutableQueue<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableQueue<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableQueue<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableQueue<T> Create<T>(params T[] items) { throw null; }
        public static System.Collections.Immutable.IImmutableQueue<T> Dequeue<T>(this System.Collections.Immutable.IImmutableQueue<T> queue, out T value) { throw null; }
    }
    public sealed partial class ImmutableQueue<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableQueue<T>
    {
        internal ImmutableQueue() { }
        public static System.Collections.Immutable.ImmutableQueue<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Collections.Immutable.ImmutableQueue<T> Clear() { throw null; }
        public System.Collections.Immutable.ImmutableQueue<T> Dequeue() { throw null; }
        public System.Collections.Immutable.ImmutableQueue<T> Dequeue(out T value) { throw null; }
        public System.Collections.Immutable.ImmutableQueue<T> Enqueue(T value) { throw null; }
        public System.Collections.Immutable.ImmutableQueue<T>.Enumerator GetEnumerator() { throw null; }
        public T Peek() { throw null; }
        public ref readonly T PeekRef() { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Collections.Immutable.IImmutableQueue<T> System.Collections.Immutable.IImmutableQueue<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableQueue<T> System.Collections.Immutable.IImmutableQueue<T>.Dequeue() { throw null; }
        System.Collections.Immutable.IImmutableQueue<T> System.Collections.Immutable.IImmutableQueue<T>.Enqueue(T value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class ImmutableSortedDictionary
    {
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>() where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> source, System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Builder builder) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IComparer<TKey>? keyComparer) where TKey : notnull { throw null; }
        public static System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this System.Collections.Generic.IEnumerable<TSource> source, System.Func<TSource, TKey> keySelector, System.Func<TSource, TValue> elementSelector, System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) where TKey : notnull { throw null; }
    }
    public sealed partial class ImmutableSortedDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableDictionary<TKey, TValue> where TKey : notnull
    {
        internal ImmutableSortedDictionary() { }
        public static readonly System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Empty;
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } }
        public System.Collections.Generic.IComparer<TKey> KeyComparer { get { throw null; } }
        public System.Collections.Generic.IEnumerable<TKey> Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
        TValue System.Collections.Generic.IDictionary<TKey,TValue>.this[TKey key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get { throw null; } }
        public System.Collections.Generic.IEnumerable<TValue> Values { get { throw null; } }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Add(TKey key, TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Clear() { throw null; }
        public bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> pair) { throw null; }
        public bool ContainsKey(TKey key) { throw null; }
        public bool ContainsValue(TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> Remove(TKey value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> SetItem(TKey key, TValue value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Clear() { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
        void System.Collections.Generic.IDictionary<TKey,TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object? value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Add(TKey key, TValue value) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> pairs) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.SetItem(TKey key, TValue value) { throw null; }
        System.Collections.Immutable.IImmutableDictionary<TKey, TValue> System.Collections.Immutable.IImmutableDictionary<TKey,TValue>.SetItems(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Builder ToBuilder() { throw null; }
        public bool TryGetKey(TKey equalKey, out TKey actualKey) { throw null; }
        public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
        public ref readonly TValue ValueRef(TKey key) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IComparer<TKey>? keyComparer) { throw null; }
        public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> WithComparers(System.Collections.Generic.IComparer<TKey>? keyComparer, System.Collections.Generic.IEqualityComparer<TValue>? valueComparer) { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
        {
            internal Builder() { }
            public int Count { get { throw null; } }
            public TValue this[TKey key] { get { throw null; } set { } }
            public System.Collections.Generic.IComparer<TKey> KeyComparer { get { throw null; } set { } }
            public System.Collections.Generic.IEnumerable<TKey> Keys { get { throw null; } }
            bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
            System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey,TValue>.Keys { get { throw null; } }
            System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey,TValue>.Values { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
            bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
            object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
            System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
            System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
            public System.Collections.Generic.IEqualityComparer<TValue> ValueComparer { get { throw null; } set { } }
            public System.Collections.Generic.IEnumerable<TValue> Values { get { throw null; } }
            public void Add(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { }
            public void Add(TKey key, TValue value) { }
            public void AddRange(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> items) { }
            public void Clear() { }
            public bool Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
            public bool ContainsKey(TKey key) { throw null; }
            public bool ContainsValue(TValue value) { throw null; }
            public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>.Enumerator GetEnumerator() { throw null; }
            public TValue? GetValueOrDefault(TKey key) { throw null; }
            public TValue GetValueOrDefault(TKey key, TValue defaultValue) { throw null; }
            public bool Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> item) { throw null; }
            public bool Remove(TKey key) { throw null; }
            public void RemoveRange(System.Collections.Generic.IEnumerable<TKey> keys) { }
            void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
            System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey,TValue>>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            void System.Collections.IDictionary.Add(object key, object? value) { }
            bool System.Collections.IDictionary.Contains(object key) { throw null; }
            System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
            void System.Collections.IDictionary.Remove(object key) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue> ToImmutable() { throw null; }
            public bool TryGetKey(TKey equalKey, out TKey actualKey) { throw null; }
            public bool TryGetValue(TKey key, [System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute(false)] out TValue value) { throw null; }
            public ref readonly TValue ValueRef(TKey key) { throw null; }
        }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public static partial class ImmutableSortedSet
    {
        public static System.Collections.Immutable.ImmutableSortedSet<T>.Builder CreateBuilder<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T>.Builder CreateBuilder<T>(System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> CreateRange<T>(System.Collections.Generic.IComparer<T>? comparer, System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T>? comparer, T item) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(System.Collections.Generic.IComparer<T>? comparer, params T[] items) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<T> Create<T>(params T[] items) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Generic.IComparer<TSource>? comparer) { throw null; }
        public static System.Collections.Immutable.ImmutableSortedSet<TSource> ToImmutableSortedSet<TSource>(this System.Collections.Immutable.ImmutableSortedSet<TSource>.Builder builder) { throw null; }
    }

#if NETCOREAPP
    public sealed partial class ImmutableSortedSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.Generic.ISet<T>, System.Collections.Generic.IReadOnlySet<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Immutable.IImmutableSet<T>
#else
    public sealed partial class ImmutableSortedSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Collections.Immutable.IImmutableSet<T>
#endif
    {
        internal ImmutableSortedSet() { }
        public static readonly System.Collections.Immutable.ImmutableSortedSet<T> Empty;
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; } }
        public System.Collections.Generic.IComparer<T> KeyComparer { get { throw null; } }
        public T? Max { get { throw null; } }
        public T? Min { get { throw null; } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
        T System.Collections.Generic.IList<T>.this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public System.Collections.Immutable.ImmutableSortedSet<T> Add(T value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> Clear() { throw null; }
        public bool Contains(T value) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> Except(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T>.Enumerator GetEnumerator() { throw null; }
        public int IndexOf(T item) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> Intersect(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public ref readonly T ItemRef(int index) { throw null; }
        public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> Remove(T value) { throw null; }
        public System.Collections.Generic.IEnumerable<T> Reverse() { throw null; }
        public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> SymmetricExcept(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        void System.Collections.Generic.ICollection<T>.Clear() { }
        void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<T>.Remove(T item) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        void System.Collections.Generic.IList<T>.Insert(int index, T item) { }
        void System.Collections.Generic.IList<T>.RemoveAt(int index) { }
        bool System.Collections.Generic.ISet<T>.Add(T item) { throw null; }
        void System.Collections.Generic.ISet<T>.ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ISet<T>.UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Add(T value) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Except(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Intersect(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Remove(T value) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.SymmetricExcept(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        System.Collections.Immutable.IImmutableSet<T> System.Collections.Immutable.IImmutableSet<T>.Union(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T>.Builder ToBuilder() { throw null; }
        public bool TryGetValue(T equalValue, out T actualValue) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> Union(System.Collections.Generic.IEnumerable<T> other) { throw null; }
        public System.Collections.Immutable.ImmutableSortedSet<T> WithComparer(System.Collections.Generic.IComparer<T>? comparer) { throw null; }
        public sealed partial class Builder : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            internal Builder() { }
            public int Count { get { throw null; } }
            public T this[int index] { get { throw null; } }
            public System.Collections.Generic.IComparer<T> KeyComparer { get { throw null; } set { } }
            public T? Max { get { throw null; } }
            public T? Min { get { throw null; } }
            bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public bool Add(T item) { throw null; }
            public void Clear() { }
            public bool Contains(T item) { throw null; }
            public void ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
            public System.Collections.Immutable.ImmutableSortedSet<T>.Enumerator GetEnumerator() { throw null; }
            public void IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
            public int IndexOf(T item) { throw null; }
            public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public ref readonly T ItemRef(int index) { throw null; }
            public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public bool Remove(T item) { throw null; }
            public System.Collections.Generic.IEnumerable<T> Reverse() { throw null; }
            public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { throw null; }
            public void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
            public bool TryGetValue(T equalValue, out T actualValue) { throw null; }
            void System.Collections.Generic.ICollection<T>.Add(T item) { }
            void System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex) { }
            System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
            void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public System.Collections.Immutable.ImmutableSortedSet<T> ToImmutable() { throw null; }
            public void UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            object? System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public static partial class ImmutableStack
    {
        public static System.Collections.Immutable.ImmutableStack<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> items) { throw null; }
        public static System.Collections.Immutable.ImmutableStack<T> Create<T>() { throw null; }
        public static System.Collections.Immutable.ImmutableStack<T> Create<T>(T item) { throw null; }
        public static System.Collections.Immutable.ImmutableStack<T> Create<T>(params T[] items) { throw null; }
        public static System.Collections.Immutable.IImmutableStack<T> Pop<T>(this System.Collections.Immutable.IImmutableStack<T> stack, out T value) { throw null; }
    }
    public sealed partial class ImmutableStack<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable, System.Collections.Immutable.IImmutableStack<T>
    {
        internal ImmutableStack() { }
        public static System.Collections.Immutable.ImmutableStack<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Collections.Immutable.ImmutableStack<T> Clear() { throw null; }
        public System.Collections.Immutable.ImmutableStack<T>.Enumerator GetEnumerator() { throw null; }
        public T Peek() { throw null; }
        public ref readonly T PeekRef() { throw null; }
        public System.Collections.Immutable.ImmutableStack<T> Pop() { throw null; }
        public System.Collections.Immutable.ImmutableStack<T> Pop(out T value) { throw null; }
        public System.Collections.Immutable.ImmutableStack<T> Push(T value) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.Collections.Immutable.IImmutableStack<T> System.Collections.Immutable.IImmutableStack<T>.Clear() { throw null; }
        System.Collections.Immutable.IImmutableStack<T> System.Collections.Immutable.IImmutableStack<T>.Pop() { throw null; }
        System.Collections.Immutable.IImmutableStack<T> System.Collections.Immutable.IImmutableStack<T>.Push(T value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
}
namespace System.Linq
{
    public static partial class ImmutableArrayExtensions
    {
        public static T? Aggregate<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, T, T> func) { throw null; }
        public static TAccumulate Aggregate<TAccumulate, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, TAccumulate seed, System.Func<TAccumulate, T, TAccumulate> func) { throw null; }
        public static TResult Aggregate<TAccumulate, TResult, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, TAccumulate seed, System.Func<TAccumulate, T, TAccumulate> func, System.Func<TAccumulate, TResult> resultSelector) { throw null; }
        public static bool All<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static bool Any<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder) { throw null; }
        public static T? ElementAtOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, int index) { throw null; }
        public static T ElementAt<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, int index) { throw null; }
        public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T? FirstOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder) { throw null; }
        public static T First<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T First<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T First<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder) { throw null; }
        public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T? LastOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder) { throw null; }
        public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T Last<T>(this System.Collections.Immutable.ImmutableArray<T>.Builder builder) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(this System.Collections.Immutable.ImmutableArray<TSource> immutableArray, System.Func<TSource, System.Collections.Generic.IEnumerable<TCollection>> collectionSelector, System.Func<TSource, TCollection, TResult> resultSelector) { throw null; }
        public static System.Collections.Generic.IEnumerable<TResult> Select<T, TResult>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, TResult> selector) { throw null; }
        public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, System.Collections.Generic.IEnumerable<TDerived> items, System.Collections.Generic.IEqualityComparer<TBase>? comparer = null) where TDerived : TBase { throw null; }
        public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, System.Collections.Immutable.ImmutableArray<TDerived> items, System.Collections.Generic.IEqualityComparer<TBase>? comparer = null) where TDerived : TBase { throw null; }
        public static bool SequenceEqual<TDerived, TBase>(this System.Collections.Immutable.ImmutableArray<TBase> immutableArray, System.Collections.Immutable.ImmutableArray<TDerived> items, System.Func<TBase, TBase, bool> predicate) where TDerived : TBase { throw null; }
        public static T? SingleOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T? SingleOrDefault<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T Single<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static T Single<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
        public static T[] ToArray<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray) { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, T> ToDictionary<TKey, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, TKey> keySelector) where TKey : notnull { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, T> ToDictionary<TKey, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, TKey> keySelector, System.Collections.Generic.IEqualityComparer<TKey>? comparer) where TKey : notnull { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, TKey> keySelector, System.Func<T, TElement> elementSelector) where TKey : notnull { throw null; }
        public static System.Collections.Generic.Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, TKey> keySelector, System.Func<T, TElement> elementSelector, System.Collections.Generic.IEqualityComparer<TKey>? comparer) where TKey : notnull { throw null; }
        public static System.Collections.Generic.IEnumerable<T> Where<T>(this System.Collections.Immutable.ImmutableArray<T> immutableArray, System.Func<T, bool> predicate) { throw null; }
    }
}
