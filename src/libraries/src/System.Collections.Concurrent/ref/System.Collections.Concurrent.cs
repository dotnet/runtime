// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Collections.Concurrent
{
    public partial class BlockingCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.IDisposable
    {
        public BlockingCollection() { }
        public BlockingCollection(System.Collections.Concurrent.IProducerConsumerCollection<T> collection) { }
        public BlockingCollection(System.Collections.Concurrent.IProducerConsumerCollection<T> collection, int boundedCapacity) { }
        public BlockingCollection(int boundedCapacity) { }
        public int BoundedCapacity { get { return default(int); } }
        public int Count { get { return default(int); } }
        public bool IsAddingCompleted { get { return default(bool); } }
        public bool IsCompleted { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Add(T item) { }
        public void Add(T item, System.Threading.CancellationToken cancellationToken) { }
        public static int AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item) { return default(int); }
        public static int AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.Threading.CancellationToken cancellationToken) { return default(int); }
        public void CompleteAdding() { }
        public void CopyTo(T[] array, int index) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable() { return default(System.Collections.Generic.IEnumerable<T>); }
        public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable(System.Threading.CancellationToken cancellationToken) { return default(System.Collections.Generic.IEnumerable<T>); }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T Take() { return default(T); }
        public T Take(System.Threading.CancellationToken cancellationToken) { return default(T); }
        public static int TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item) { item = default(T); return default(int); }
        public static int TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, System.Threading.CancellationToken cancellationToken) { item = default(T); return default(int); }
        public T[] ToArray() { return default(T[]); }
        public bool TryAdd(T item) { return default(bool); }
        public bool TryAdd(T item, int millisecondsTimeout) { return default(bool); }
        public bool TryAdd(T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(bool); }
        public bool TryAdd(T item, System.TimeSpan timeout) { return default(bool); }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item) { return default(int); }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, int millisecondsTimeout) { return default(int); }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { return default(int); }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.TimeSpan timeout) { return default(int); }
        public bool TryTake(out T item) { item = default(T); return default(bool); }
        public bool TryTake(out T item, int millisecondsTimeout) { item = default(T); return default(bool); }
        public bool TryTake(out T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { item = default(T); return default(bool); }
        public bool TryTake(out T item, System.TimeSpan timeout) { item = default(T); return default(bool); }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item) { item = default(T); return default(int); }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, int millisecondsTimeout) { item = default(T); return default(int); }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { item = default(T); return default(int); }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, System.TimeSpan timeout) { item = default(T); return default(int); }
    }
    public partial class ConcurrentBag<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentBag() { }
        public ConcurrentBag(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { return default(int); } }
        public bool IsEmpty { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Add(T item) { }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T[] ToArray() { return default(T[]); }
        public bool TryPeek(out T result) { result = default(T); return default(bool); }
        public bool TryTake(out T result) { result = default(T); return default(bool); }
    }
    public partial class ConcurrentDictionary<TKey, TValue> : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IDictionary<TKey, TValue>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>, System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>, System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public ConcurrentDictionary() { }
        public ConcurrentDictionary(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection) { }
        public ConcurrentDictionary(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public ConcurrentDictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public ConcurrentDictionary(int concurrencyLevel, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<TKey, TValue>> collection, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public ConcurrentDictionary(int concurrencyLevel, int capacity) { }
        public ConcurrentDictionary(int concurrencyLevel, int capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer) { }
        public int Count { get { return default(int); } }
        public bool IsEmpty { get { return default(bool); } }
        public TValue this[TKey key] { get { return default(TValue); } set { } }
        public System.Collections.Generic.ICollection<TKey> Keys { get { return default(System.Collections.Generic.ICollection<TKey>); } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.IsReadOnly { get { return default(bool); } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Keys { get { return default(System.Collections.Generic.IEnumerable<TKey>); } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.Values { get { return default(System.Collections.Generic.IEnumerable<TValue>); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { return default(System.Collections.ICollection); } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { return default(System.Collections.ICollection); } }
        public System.Collections.Generic.ICollection<TValue> Values { get { return default(System.Collections.Generic.ICollection<TValue>); } }
        public TValue AddOrUpdate(TKey key, TValue addValue, System.Func<TKey, TValue, TValue> updateValueFactory) { return default(TValue); }
        public TValue AddOrUpdate(TKey key, System.Func<TKey, TValue> addValueFactory, System.Func<TKey, TValue, TValue> updateValueFactory) { return default(TValue); }
        public void Clear() { }
        public bool ContainsKey(TKey key) { return default(bool); }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>>); }
        public TValue GetOrAdd(TKey key, TValue value) { return default(TValue); }
        public TValue GetOrAdd(TKey key, System.Func<TKey, TValue> valueFactory) { return default(TValue); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey, TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { return default(bool); }
        void System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey key) { return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public System.Collections.Generic.KeyValuePair<TKey, TValue>[] ToArray() { return default(System.Collections.Generic.KeyValuePair<TKey, TValue>[]); }
        public bool TryAdd(TKey key, TValue value) { return default(bool); }
        public bool TryGetValue(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        public bool TryRemove(TKey key, out TValue value) { value = default(TValue); return default(bool); }
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue) { return default(bool); }
    }
    public partial class ConcurrentQueue<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentQueue() { }
        public ConcurrentQueue(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { return default(int); } }
        public bool IsEmpty { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void CopyTo(T[] array, int index) { }
        public void Enqueue(T item) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { return default(bool); }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryTake(out T item) { item = default(T); return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T[] ToArray() { return default(T[]); }
        public bool TryDequeue(out T result) { result = default(T); return default(bool); }
        public bool TryPeek(out T result) { result = default(T); return default(bool); }
    }
    public partial class ConcurrentStack<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentStack() { }
        public ConcurrentStack(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { return default(int); } }
        public bool IsEmpty { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Clear() { }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<T>); }
        public void Push(T item) { }
        public void PushRange(T[] items) { }
        public void PushRange(T[] items, int startIndex, int count) { }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { return default(bool); }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryTake(out T item) { item = default(T); return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public T[] ToArray() { return default(T[]); }
        public bool TryPeek(out T result) { result = default(T); return default(bool); }
        public bool TryPop(out T result) { result = default(T); return default(bool); }
        public int TryPopRange(T[] items) { return default(int); }
        public int TryPopRange(T[] items, int startIndex, int count) { return default(int); }
    }
    [System.FlagsAttribute]
    public enum EnumerablePartitionerOptions
    {
        NoBuffering = 1,
        None = 0,
    }
    public partial interface IProducerConsumerCollection<T> : System.Collections.Generic.IEnumerable<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        void CopyTo(T[] array, int index);
        T[] ToArray();
        bool TryAdd(T item);
        bool TryTake(out T item);
    }
    public abstract partial class OrderablePartitioner<TSource> : System.Collections.Concurrent.Partitioner<TSource>
    {
        protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized) { }
        public bool KeysNormalized { get { return default(bool); } }
        public bool KeysOrderedAcrossPartitions { get { return default(bool); } }
        public bool KeysOrderedInEachPartition { get { return default(bool); } }
        public override System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions() { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public virtual System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long, TSource>> GetOrderableDynamicPartitions() { return default(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long, TSource>>); }
        public abstract System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);
        public override System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(int partitionCount) { return default(System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>>); }
    }
    public static partial class Partitioner
    {
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>> Create(int fromInclusive, int toExclusive) { return default(System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>>); }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize) { return default(System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>>); }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>> Create(long fromInclusive, long toExclusive) { return default(System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>>); }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize) { return default(System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>>); }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance) { return default(System.Collections.Concurrent.OrderablePartitioner<TSource>); }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source) { return default(System.Collections.Concurrent.OrderablePartitioner<TSource>); }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Concurrent.EnumerablePartitionerOptions partitionerOptions) { return default(System.Collections.Concurrent.OrderablePartitioner<TSource>); }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IList<TSource> list, bool loadBalance) { return default(System.Collections.Concurrent.OrderablePartitioner<TSource>); }
    }
    public abstract partial class Partitioner<TSource>
    {
        protected Partitioner() { }
        public virtual bool SupportsDynamicPartitions { get { return default(bool); } }
        public virtual System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions() { return default(System.Collections.Generic.IEnumerable<TSource>); }
        public abstract System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(int partitionCount);
    }
}
