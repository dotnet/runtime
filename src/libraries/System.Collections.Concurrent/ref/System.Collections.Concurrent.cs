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
        public int BoundedCapacity { get { throw null; } }
        public int Count { get { throw null; } }
        public bool IsAddingCompleted { get { throw null; } }
        public bool IsCompleted { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void Add(T item) { }
        public void Add(T item, System.Threading.CancellationToken cancellationToken) { }
        public static int AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item) { throw null; }
        public static int AddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.Threading.CancellationToken cancellationToken) { throw null; }
        public void CompleteAdding() { }
        public void CopyTo(T[] array, int index) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable() { throw null; }
        public System.Collections.Generic.IEnumerable<T> GetConsumingEnumerable(System.Threading.CancellationToken cancellationToken) { throw null; }
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public T Take() { throw null; }
        public T Take(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static int TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item) { throw null; }
        public static int TakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, System.Threading.CancellationToken cancellationToken) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryAdd(T item) { throw null; }
        public bool TryAdd(T item, int millisecondsTimeout) { throw null; }
        public bool TryAdd(T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TryAdd(T item, System.TimeSpan timeout) { throw null; }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item) { throw null; }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, int millisecondsTimeout) { throw null; }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static int TryAddToAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, T item, System.TimeSpan timeout) { throw null; }
        public bool TryTake(out T item) { throw null; }
        public bool TryTake(out T item, int millisecondsTimeout) { throw null; }
        public bool TryTake(out T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TryTake(out T item, System.TimeSpan timeout) { throw null; }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item) { throw null; }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, int millisecondsTimeout) { throw null; }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, System.Threading.CancellationToken cancellationToken) { throw null; }
        public static int TryTakeFromAny(System.Collections.Concurrent.BlockingCollection<T>[] collections, out T item, System.TimeSpan timeout) { throw null; }
    }
    public partial class ConcurrentBag<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentBag() { }
        public ConcurrentBag(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void Add(T item) { }
        public void Clear() { }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryPeek(out T result) { throw null; }
        public bool TryTake(out T result) { throw null; }
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
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public TValue this[TKey key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<TKey> Keys { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.IsReadOnly { get { throw null; } }
        System.Collections.Generic.IEnumerable<TKey> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Keys { get { throw null; } }
        System.Collections.Generic.IEnumerable<TValue> System.Collections.Generic.IReadOnlyDictionary<TKey,TValue>.Values { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        public System.Collections.Generic.ICollection<TValue> Values { get { throw null; } }
        public TValue AddOrUpdate(TKey key, System.Func<TKey, TValue> addValueFactory, System.Func<TKey, TValue, TValue> updateValueFactory) { throw null; }
        public TValue AddOrUpdate(TKey key, TValue addValue, System.Func<TKey, TValue, TValue> updateValueFactory) { throw null; }
        public TValue AddOrUpdate<TArg>(TKey key, System.Func<TKey, TArg, TValue> addValueFactory, System.Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument) { throw null; }
        public void Clear() { }
        public bool ContainsKey(TKey key) { throw null; }
        public System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<TKey, TValue>> GetEnumerator() { throw null; }
        public TValue GetOrAdd(TKey key, System.Func<TKey, TValue> valueFactory) { throw null; }
        public TValue GetOrAdd(TKey key, TValue value) { throw null; }
        public TValue GetOrAdd<TArg>(TKey key, System.Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Add(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Contains(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.CopyTo(System.Collections.Generic.KeyValuePair<TKey, TValue>[] array, int index) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<TKey,TValue>>.Remove(System.Collections.Generic.KeyValuePair<TKey, TValue> keyValuePair) { throw null; }
        void System.Collections.Generic.IDictionary<TKey,TValue>.Add(TKey key, TValue value) { }
        bool System.Collections.Generic.IDictionary<TKey,TValue>.Remove(TKey key) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public System.Collections.Generic.KeyValuePair<TKey, TValue>[] ToArray() { throw null; }
        public bool TryAdd(TKey key, TValue value) { throw null; }
        public bool TryGetValue(TKey key, out TValue value) { throw null; }
        public bool TryRemove(TKey key, out TValue value) { throw null; }
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue) { throw null; }
    }
    public partial class ConcurrentQueue<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentQueue() { }
        public ConcurrentQueue(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void Clear() { }
        public void CopyTo(T[] array, int index) { }
        public void Enqueue(T item) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { throw null; }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryTake(out T item) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryDequeue(out T result) { throw null; }
        public bool TryPeek(out T result) { throw null; }
    }
    public partial class ConcurrentStack<T> : System.Collections.Concurrent.IProducerConsumerCollection<T>, System.Collections.Generic.IEnumerable<T>, System.Collections.Generic.IReadOnlyCollection<T>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConcurrentStack() { }
        public ConcurrentStack(System.Collections.Generic.IEnumerable<T> collection) { }
        public int Count { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void Clear() { }
        public void CopyTo(T[] array, int index) { }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        public void Push(T item) { }
        public void PushRange(T[] items) { }
        public void PushRange(T[] items, int startIndex, int count) { }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryAdd(T item) { throw null; }
        bool System.Collections.Concurrent.IProducerConsumerCollection<T>.TryTake(out T item) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryPeek(out T result) { throw null; }
        public bool TryPop(out T result) { throw null; }
        public int TryPopRange(T[] items) { throw null; }
        public int TryPopRange(T[] items, int startIndex, int count) { throw null; }
    }
    [System.FlagsAttribute]
    public enum EnumerablePartitionerOptions
    {
        None = 0,
        NoBuffering = 1,
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
        public bool KeysNormalized { get { throw null; } }
        public bool KeysOrderedAcrossPartitions { get { throw null; } }
        public bool KeysOrderedInEachPartition { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions() { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long, TSource>> GetOrderableDynamicPartitions() { throw null; }
        public abstract System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);
        public override System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(int partitionCount) { throw null; }
    }
    public static partial class Partitioner
    {
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>> Create(int fromInclusive, int toExclusive) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>> Create(long fromInclusive, long toExclusive) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<System.Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Collections.Concurrent.EnumerablePartitionerOptions partitionerOptions) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(System.Collections.Generic.IList<TSource> list, bool loadBalance) { throw null; }
        public static System.Collections.Concurrent.OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance) { throw null; }
    }
    public abstract partial class Partitioner<TSource>
    {
        protected Partitioner() { }
        public virtual bool SupportsDynamicPartitions { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<TSource> GetDynamicPartitions() { throw null; }
        public abstract System.Collections.Generic.IList<System.Collections.Generic.IEnumerator<TSource>> GetPartitions(int partitionCount);
    }
}
