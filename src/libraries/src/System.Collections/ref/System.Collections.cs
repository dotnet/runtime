// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Collections
{
    public sealed partial class BitArray : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public BitArray(bool[] values) { }
        public BitArray(byte[] bytes) { }
        public BitArray(System.Collections.BitArray bits) { }
        public BitArray(int length) { }
        public BitArray(int length, bool defaultValue) { }
        public BitArray(int[] values) { }
        public bool this[int index] { get { return default(bool); } set { } }
        public int Length { get { return default(int); } set { } }
        int System.Collections.ICollection.Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Collections.BitArray And(System.Collections.BitArray value) { return default(System.Collections.BitArray); }
        public bool Get(int index) { return default(bool); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public System.Collections.BitArray Not() { return default(System.Collections.BitArray); }
        public System.Collections.BitArray Or(System.Collections.BitArray value) { return default(System.Collections.BitArray); }
        public void Set(int index, bool value) { }
        public void SetAll(bool value) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        public System.Collections.BitArray Xor(System.Collections.BitArray value) { return default(System.Collections.BitArray); }
    }
    public static partial class StructuralComparisons
    {
        public static System.Collections.IComparer StructuralComparer { get { return default(System.Collections.IComparer); } }
        public static System.Collections.IEqualityComparer StructuralEqualityComparer { get { return default(System.Collections.IEqualityComparer); } }
    }
}
namespace System.Collections.Generic
{
    public abstract partial class Comparer<T> : System.Collections.Generic.IComparer<T>, System.Collections.IComparer
    {
        protected Comparer() { }
        public static System.Collections.Generic.Comparer<T> Default { get { return default(System.Collections.Generic.Comparer<T>); } }
        public abstract int Compare(T x, T y);
        public static System.Collections.Generic.Comparer<T> Create(System.Comparison<T> comparison) { return default(System.Collections.Generic.Comparer<T>); }
        int System.Collections.IComparer.Compare(object x, object y) { return default(int); }
    }
    public partial class Dictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public Dictionary() { }
        public Dictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public Dictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public Dictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public Dictionary(int capacity) { }
        public Dictionary(int capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public System.Collections.Generic.IEqualityComparer<TKey> Comparer { get { return default(System.Collections.Generic.IEqualityComparer<TKey>); } }
        public int Count { get { return default(int); } }
        public TValue this[TKey key] { get { return default(TValue); } set { } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection Keys { get { return default(System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection); } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { return default(bool); } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.ICollection<TKey>); } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.ICollection<TValue>); } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.IEnumerable<TKey>); } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.IEnumerable<TValue>); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { return default(System.Collections.ICollection); } }
        public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection Values { get { return default(System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection); } }
        public void Add(TKey key, TValue value) { }
        public void Clear() { }
        public bool ContainsKey(TKey key) { return default(bool); }
        public bool ContainsValue(TValue value) { return default(bool); }
        public System.Collections.Generic.Dictionary<TKey, TValue>.Enumerator GetEnumerator() { return default(System.Collections.Generic.Dictionary<TKey, TValue>.Enumerator); }
        public bool Remove(TKey key) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IDictionaryEnumerator, System.Collections.IEnumerator, System.IDisposable
        {
            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get { return default(System.Collections.Generic.KeyValuePair<TKey, TValue>); } }
            System.Collections.DictionaryEntry System.Collections.IDictionaryEnumerator.Entry { get { return default(System.Collections.DictionaryEntry); } }
            object System.Collections.IDictionaryEnumerator.Key { get { return default(object); } }
            object System.Collections.IDictionaryEnumerator.Value { get { return default(object); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public KeyCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary) { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TKey[] array, int index) { }
            public System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator() { return default(System.Collections.Generic.Dictionary<TKey, TValue>.KeyCollection.Enumerator); }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { return default(bool); }
            System.Collections.Generic.IEnumerator<TKey> System.Collections.Generic.IEnumerable<TKey>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TKey>); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TKey>, System.Collections.IEnumerator, System.IDisposable
            {
                public TKey Current { get { return default(TKey); } }
                object System.Collections.IEnumerator.Current { get { return default(object); } }
                public void Dispose() { }
                public bool MoveNext() { return default(bool); }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public ValueCollection(System.Collections.Generic.Dictionary<TKey, TValue> dictionary) { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TValue[] array, int index) { }
            public System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator() { return default(System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.Enumerator); }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { return default(bool); }
            System.Collections.Generic.IEnumerator<TValue> System.Collections.Generic.IEnumerable<TValue>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TValue>); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TValue>, System.Collections.IEnumerator, System.IDisposable
            {
                public TValue Current { get { return default(TValue); } }
                object System.Collections.IEnumerator.Current { get { return default(object); } }
                public void Dispose() { }
                public bool MoveNext() { return default(bool); }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
    }
    public abstract partial class EqualityComparer<T> : System.Collections.Generic.IEqualityComparer<T>, System.Collections.IEqualityComparer
    {
        protected EqualityComparer() { }
        public static System.Collections.Generic.EqualityComparer<T> Default { get { return default(System.Collections.Generic.EqualityComparer<T>); } }
        public abstract bool Equals(T x, T y);
        public abstract int GetHashCode(T obj);
        bool System.Collections.IEqualityComparer.Equals(object x, object y) { return default(bool); }
        int System.Collections.IEqualityComparer.GetHashCode(object obj) { return default(int); }
    }
    public partial class HashSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.IEnumerable
    {
        public HashSet() { }
        public HashSet(System.Collections.Generic.IEnumerable<T> collection) { }
        public HashSet(System.Collections.Generic.IEnumerable<T> collection, System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public HashSet(System.Collections.Generic.IEqualityComparer<T> comparer) { }
        public System.Collections.Generic.IEqualityComparer<T> Comparer { get { return default(System.Collections.Generic.IEqualityComparer<T>); } }
        public int Count { get { return default(int); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        public bool Add(T item) { return default(bool); }
        public void Clear() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int arrayIndex) { }
        public void CopyTo(T[] array, int arrayIndex, int count) { }
        public void ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        public System.Collections.Generic.HashSet<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.HashSet<T>.Enumerator); }
        public void IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
        public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool Remove(T item) { return default(bool); }
        public int RemoveWhere(System.Predicate<T> match) { return default(int); }
        public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public void TrimExcess() { }
        public void UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class LinkedList<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public LinkedList() { }
        public LinkedList(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { return default(int); } }
        public System.Collections.Generic.LinkedListNode<T> First { get { return default(System.Collections.Generic.LinkedListNode<T>); } }
        public System.Collections.Generic.LinkedListNode<T> Last { get { return default(System.Collections.Generic.LinkedListNode<T>); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public System.Collections.Generic.LinkedListNode<T> AddAfter(System.Collections.Generic.LinkedListNode<T> node, T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public void AddAfter(System.Collections.Generic.LinkedListNode<T> node, System.Collections.Generic.LinkedListNode<T> newNode) { }
        public System.Collections.Generic.LinkedListNode<T> AddBefore(System.Collections.Generic.LinkedListNode<T> node, T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public void AddBefore(System.Collections.Generic.LinkedListNode<T> node, System.Collections.Generic.LinkedListNode<T> newNode) { }
        public System.Collections.Generic.LinkedListNode<T> AddFirst(T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public void AddFirst(System.Collections.Generic.LinkedListNode<T> node) { }
        public System.Collections.Generic.LinkedListNode<T> AddLast(T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public void AddLast(System.Collections.Generic.LinkedListNode<T> node) { }
        public void Clear() { }
        public bool Contains(T value) { return default(bool); }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.LinkedListNode<T> Find(T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public System.Collections.Generic.LinkedListNode<T> FindLast(T value) { return default(System.Collections.Generic.LinkedListNode<T>); }
        public System.Collections.Generic.LinkedList<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.LinkedList<T>.Enumerator); }
        public bool Remove(T value) { return default(bool); }
        public void Remove(System.Collections.Generic.LinkedListNode<T> node) { }
        public void RemoveFirst() { }
        public void RemoveLast() { }
        void System.Collections.Generic.ICollection<T>.Add(T value) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public sealed partial class LinkedListNode<T>
    {
        public LinkedListNode(T value) { }
        public System.Collections.Generic.LinkedList<T> List { get { return default(System.Collections.Generic.LinkedList<T>); } }
        public System.Collections.Generic.LinkedListNode<T> Next { get { return default(System.Collections.Generic.LinkedListNode<T>); } }
        public System.Collections.Generic.LinkedListNode<T> Previous { get { return default(System.Collections.Generic.LinkedListNode<T>); } }
        public T Value { get { return default(T); } set { } }
    }
    public partial class List<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IList<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.IReadOnlyList<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public List() { }
        public List(System.Collections.Generic.IEnumerable<T> collection) { }
        public List(int capacity) { }
        public int Capacity { get { return default(int); } set { } }
        public int Count { get { return default(int); } }
        public T this[int index] { get { return default(T); } set { } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public void Add(T item) { }
        public void AddRange(System.Collections.Generic.IEnumerable<T> collection) { }
        public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly() { return default(System.Collections.ObjectModel.ReadOnlyCollection<T>); }
        public int BinarySearch(T item) { return default(int); }
        public int BinarySearch(T item, System.Collections.Generic.IComparer<T> comparer) { return default(int); }
        public int BinarySearch(int index, int count, T item, System.Collections.Generic.IComparer<T> comparer) { return default(int); }
        public void Clear() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int arrayIndex) { }
        public void CopyTo(int index, T[] array, int arrayIndex, int count) { }
        public bool Exists(System.Predicate<T> match) { return default(bool); }
        public T Find(System.Predicate<T> match) { return default(T); }
        public System.Collections.Generic.List<T> FindAll(System.Predicate<T> match) { return default(System.Collections.Generic.List<T>); }
        public int FindIndex(int startIndex, int count, System.Predicate<T> match) { return default(int); }
        public int FindIndex(int startIndex, System.Predicate<T> match) { return default(int); }
        public int FindIndex(System.Predicate<T> match) { return default(int); }
        public T FindLast(System.Predicate<T> match) { return default(T); }
        public int FindLastIndex(int startIndex, int count, System.Predicate<T> match) { return default(int); }
        public int FindLastIndex(int startIndex, System.Predicate<T> match) { return default(int); }
        public int FindLastIndex(System.Predicate<T> match) { return default(int); }
        public void ForEach(System.Action<T> action) { }
        public System.Collections.Generic.List<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.List<T>.Enumerator); }
        public System.Collections.Generic.List<T> GetRange(int index, int count) { return default(System.Collections.Generic.List<T>); }
        public int IndexOf(T item) { return default(int); }
        public int IndexOf(T item, int index) { return default(int); }
        public int IndexOf(T item, int index, int count) { return default(int); }
        public void Insert(int index, T item) { }
        public void InsertRange(int index, System.Collections.Generic.IEnumerable<T> collection) { }
        public int LastIndexOf(T item) { return default(int); }
        public int LastIndexOf(T item, int index) { return default(int); }
        public int LastIndexOf(T item, int index, int count) { return default(int); }
        public bool Remove(T item) { return default(bool); }
        public int RemoveAll(System.Predicate<T> match) { return default(int); }
        public void RemoveAt(int index) { }
        public void RemoveRange(int index, int count) { }
        public void Reverse() { }
        public void Reverse(int index, int count) { }
        public void Sort() { }
        public void Sort(System.Collections.Generic.IComparer<T> comparer) { }
        public void Sort(System.Comparison<T> comparison) { }
        public void Sort(int index, int count, System.Collections.Generic.IComparer<T> comparer) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        int System.Collections.IList.Add(object item) { return default(int); }
        bool System.Collections.IList.Contains(object item) { return default(bool); }
        int System.Collections.IList.IndexOf(object item) { return default(int); }
        void System.Collections.IList.Insert(int index, object item) { }
        void System.Collections.IList.Remove(object item) { }
        public T[] ToArray() { return default(T[]); }
        public void TrimExcess() { }
        public bool TrueForAll(System.Predicate<T> match) { return default(bool); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class Queue<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public Queue() { }
        public Queue(System.Collections.Generic.IEnumerable<T> collection) { }
        public Queue(int capacity) { }
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Clear() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array, int arrayIndex) { }
        public T Dequeue() { return default(T); }
        public void Enqueue(T item) { }
        public System.Collections.Generic.Queue<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.Queue<T>.Enumerator); }
        public T Peek() { return default(T); }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T[] ToArray() { return default(T[]); }
        public void TrimExcess() { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class SortedDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public SortedDictionary() { }
        public SortedDictionary(System.Collections.Generic.IComparer<TKey> comparer) { }
        public SortedDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public SortedDictionary(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IComparer<TKey> comparer) { }
        public System.Collections.Generic.IComparer<TKey> Comparer { get { return default(System.Collections.Generic.IComparer<TKey>); } }
        public int Count { get { return default(int); } }
        public TValue this[TKey key] { get { return default(TValue); } set { } }
        public System.Collections.Generic.SortedDictionary<TKey, TValue>.KeyCollection Keys { get { return default(System.Collections.Generic.SortedDictionary<TKey, TValue>.KeyCollection); } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { return default(bool); } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.ICollection<TKey>); } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.ICollection<TValue>); } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.IEnumerable<TKey>); } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.IEnumerable<TValue>); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { return default(System.Collections.ICollection); } }
        public System.Collections.Generic.SortedDictionary<TKey, TValue>.ValueCollection Values { get { return default(System.Collections.Generic.SortedDictionary<TKey, TValue>.ValueCollection); } }
        public void Add(TKey key, TValue value) { }
        public void Clear() { }
        public bool ContainsKey(TKey key) { return default(bool); }
        public bool ContainsValue(TValue value) { return default(bool); }
        public void CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index) { }
        public System.Collections.Generic.SortedDictionary<TKey, TValue>.Enumerator GetEnumerator() { return default(System.Collections.Generic.SortedDictionary<TKey, TValue>.Enumerator); }
        public bool Remove(TKey key) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.IDictionaryEnumerator, System.Collections.IEnumerator, System.IDisposable
        {
            public System.Collections.Generic.KeyValuePair<TKey, TValue> Current { get { return default(System.Collections.Generic.KeyValuePair<TKey, TValue>); } }
            System.Collections.DictionaryEntry System.Collections.IDictionaryEnumerator.Entry { get { return default(System.Collections.DictionaryEntry); } }
            object System.Collections.IDictionaryEnumerator.Key { get { return default(object); } }
            object System.Collections.IDictionaryEnumerator.Value { get { return default(object); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
        public sealed partial class KeyCollection : System.Collections.Generic.ICollection<TKey>, System.Collections.Generic.IEnumerable<TKey>, System.Collections.Generic.IReadOnlyCollection<TKey>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public KeyCollection(System.Collections.Generic.SortedDictionary<TKey, TValue> dictionary) { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TKey>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TKey[] array, int index) { }
            public System.Collections.Generic.SortedDictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator() { return default(System.Collections.Generic.SortedDictionary<TKey, TValue>.KeyCollection.Enumerator); }
            void System.Collections.Generic.ICollection<TKey>.Add(TKey item) { }
            void System.Collections.Generic.ICollection<TKey>.Clear() { }
            bool System.Collections.Generic.ICollection<TKey>.Contains(TKey item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TKey>.Remove(TKey item) { return default(bool); }
            System.Collections.Generic.IEnumerator<TKey> System.Collections.Generic.IEnumerable<TKey>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TKey>); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TKey>, System.Collections.IEnumerator, System.IDisposable
            {
                public TKey Current { get { return default(TKey); } }
                object System.Collections.IEnumerator.Current { get { return default(object); } }
                public void Dispose() { }
                public bool MoveNext() { return default(bool); }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        public sealed partial class ValueCollection : System.Collections.Generic.ICollection<TValue>, System.Collections.Generic.IEnumerable<TValue>, System.Collections.Generic.IReadOnlyCollection<TValue>, System.Collections.ICollection, System.Collections.IEnumerable
        {
            public ValueCollection(System.Collections.Generic.SortedDictionary<TKey, TValue> dictionary) { }
            public int Count { get { return default(int); } }
            bool System.Collections.Generic.ICollection<TValue>.IsReadOnly { get { return default(bool); } }
            bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
            object System.Collections.ICollection.SyncRoot { get { return default(object); } }
            public void CopyTo(TValue[] array, int index) { }
            public System.Collections.Generic.SortedDictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator() { return default(System.Collections.Generic.SortedDictionary<TKey, TValue>.ValueCollection.Enumerator); }
            void System.Collections.Generic.ICollection<TValue>.Add(TValue item) { }
            void System.Collections.Generic.ICollection<TValue>.Clear() { }
            bool System.Collections.Generic.ICollection<TValue>.Contains(TValue item) { return default(bool); }
            bool System.Collections.Generic.ICollection<TValue>.Remove(TValue item) { return default(bool); }
            System.Collections.Generic.IEnumerator<TValue> System.Collections.Generic.IEnumerable<TValue>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<TValue>); }
            void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
            [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<TValue>, System.Collections.IEnumerator, System.IDisposable
            {
                public TValue Current { get { return default(TValue); } }
                object System.Collections.IEnumerator.Current { get { return default(object); } }
                public void Dispose() { }
                public bool MoveNext() { return default(bool); }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
    }
    public partial class SortedList<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public SortedList() { }
        public SortedList(System.Collections.Generic.IComparer<TKey> comparer) { }
        public SortedList(System.Collections.Generic.IDictionary<TKey, TValue> dictionary) { }
        public SortedList(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IComparer<TKey> comparer) { }
        public SortedList(int capacity) { }
        public SortedList(int capacity, System.Collections.Generic.IComparer<TKey> comparer) { }
        public int Capacity { get { return default(int); } set { } }
        public System.Collections.Generic.IComparer<TKey> Comparer { get { return default(System.Collections.Generic.IComparer<TKey>); } }
        public int Count { get { return default(int); } }
        public TValue this[TKey key] { get { return default(TValue); } set { } }
        public System.Collections.Generic.IList<TKey> Keys { get { return default(System.Collections.Generic.IList<TKey>); } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { return default(bool); } }
        System.Collections.Generic.ICollection<TKey> System.Collections.Generic.IDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.ICollection<TKey>); } }
        System.Collections.Generic.ICollection<TValue> System.Collections.Generic.IDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.ICollection<TValue>); } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.IEnumerable<TKey>); } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.IEnumerable<TValue>); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { return default(System.Collections.ICollection); } }
        public System.Collections.Generic.IList<TValue> Values { get { return default(System.Collections.Generic.IList<TValue>); } }
        public void Add(TKey key, TValue value) { }
        public void Clear() { }
        public bool ContainsKey(TKey key) { return default(bool); }
        public bool ContainsValue(TValue value) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        public int IndexOfKey(TKey key) { return default(int); }
        public int IndexOfValue(TValue value) { return default(int); }
        public bool Remove(TKey key) { return default(bool); }
        public void RemoveAt(int index) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public void TrimExcess() { }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
    }
    public partial class SortedSet<T> : System.Collections.Generic.ICollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.Generic.ISet<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public SortedSet() { }
        public SortedSet(System.Collections.Generic.IComparer<T> comparer) { }
        public SortedSet(System.Collections.Generic.IEnumerable<T> collection) { }
        public SortedSet(System.Collections.Generic.IEnumerable<T> collection, System.Collections.Generic.IComparer<T> comparer) { }
        public System.Collections.Generic.IComparer<T> Comparer { get { return default(System.Collections.Generic.IComparer<T>); } }
        public int Count { get { return default(int); } }
        public T Max { get { return default(T); } }
        public T Min { get { return default(T); } }
        bool System.Collections.Generic.ICollection<T>.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public bool Add(T item) { return default(bool); }
        public virtual void Clear() { }
        public virtual bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array) { }
        public void CopyTo(T[] array, int index) { }
        public void CopyTo(T[] array, int index, int count) { }
        public void ExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        public System.Collections.Generic.SortedSet<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.SortedSet<T>.Enumerator); }
        public virtual System.Collections.Generic.SortedSet<T> GetViewBetween(T lowerValue, T upperValue) { return default(System.Collections.Generic.SortedSet<T>); }
        public virtual void IntersectWith(System.Collections.Generic.IEnumerable<T> other) { }
        public bool IsProperSubsetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsProperSupersetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsSubsetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool IsSupersetOf(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool Overlaps(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public bool Remove(T item) { return default(bool); }
        public int RemoveWhere(System.Predicate<T> match) { return default(int); }
        public System.Collections.Generic.IEnumerable<T> Reverse() { return default(System.Collections.Generic.IEnumerable<T>); }
        public bool SetEquals(System.Collections.Generic.IEnumerable<T> other) { return default(bool); }
        public void SymmetricExceptWith(System.Collections.Generic.IEnumerable<T> other) { }
        void System.Collections.Generic.ICollection<T>.Add(T item) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public void UnionWith(System.Collections.Generic.IEnumerable<T> other) { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class Stack<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public Stack() { }
        public Stack(System.Collections.Generic.IEnumerable<T> collection) { }
        public Stack(int capacity) { }
        public int Count { get { return default(int); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Clear() { }
        public bool Contains(T item) { return default(bool); }
        public void CopyTo(T[] array, int arrayIndex) { }
        public System.Collections.Generic.Stack<T>.Enumerator GetEnumerator() { return default(System.Collections.Generic.Stack<T>.Enumerator); }
        public T Peek() { return default(T); }
        public T Pop() { return default(T); }
        public void Push(T item) { }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int arrayIndex) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T[] ToArray() { return default(T[]); }
        public void TrimExcess() { }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<T>, System.Collections.IEnumerator, System.IDisposable
        {
            public T Current { get { return default(T); } }
            object System.Collections.IEnumerator.Current { get { return default(object); } }
            public void Dispose() { }
            public bool MoveNext() { return default(bool); }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
}
