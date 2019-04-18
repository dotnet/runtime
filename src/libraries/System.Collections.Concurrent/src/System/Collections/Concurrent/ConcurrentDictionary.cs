// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:   ConcurrentDictionary
**
**
** Purpose: A scalable dictionary for concurrent access
**
**
===========================================================*/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Represents a thread-safe collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentDictionary{TKey,TValue}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue> where TKey : object
    {
        /// <summary>
        /// Tables that hold the internal state of the ConcurrentDictionary
        ///
        /// Wrapping the three tables in a single object allows us to atomically
        /// replace all tables at once.
        /// </summary>
        private sealed class Tables
        {
            internal readonly Node[] _buckets; // A singly-linked list for each bucket.
            internal readonly object[] _locks; // A set of locks, each guarding a section of the table.
            internal volatile int[] _countPerLock; // The number of elements guarded by each lock.

            internal Tables(Node[] buckets, object[] locks, int[] countPerLock)
            {
                _buckets = buckets;
                _locks = locks;
                _countPerLock = countPerLock;
            }
        }

        private volatile Tables _tables; // Internal tables of the dictionary
        private IEqualityComparer<TKey> _comparer; // Key equality comparer
        private readonly bool _growLockArray; // Whether to dynamically increase the size of the striped lock
        private int _budget; // The maximum number of elements per lock before a resize operation is triggered

        // The default capacity, i.e. the initial # of buckets. When choosing this value, we are making
        // a trade-off between the size of a very small dictionary, and the number of resizes when
        // constructing a large dictionary. Also, the capacity should not be divisible by a small prime.
        private const int DefaultCapacity = 31;

        // The maximum size of the striped lock that will not be exceeded when locks are automatically
        // added as the dictionary grows. However, the user is allowed to exceed this limit by passing
        // a concurrency level larger than MaxLockNumber into the constructor.
        private const int MaxLockNumber = 1024;

        // Whether TValue is a type that can be written atomically (i.e., with no danger of torn reads)
        private static readonly bool s_isValueWriteAtomic = IsValueWriteAtomic();

        /// <summary>
        /// Determines whether type TValue can be written atomically
        /// </summary>
        private static bool IsValueWriteAtomic()
        {
            //
            // Section 12.6.6 of ECMA CLI explains which types can be read and written atomically without
            // the risk of tearing.
            //
            // See http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-335.pdf
            //
            Type valueType = typeof(TValue);
            if (!valueType.IsValueType)
            {
                return true;
            }

            switch (Type.GetTypeCode(valueType))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    return true;
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.UInt64:
                    return IntPtr.Size == 8;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the default concurrency level, has the default initial capacity, and
        /// uses the default comparer for the key type.
        /// </summary>
        public ConcurrentDictionary() : this(DefaultConcurrencyLevel, DefaultCapacity, true, null) { }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the default
        /// comparer for the key type.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is
        /// less than 1.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        public ConcurrentDictionary(int concurrencyLevel, int capacity) : this(concurrencyLevel, capacity, false, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/>, has the default concurrency
        /// level, has the default initial capacity, and uses the default comparer for the key type.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more
        /// duplicate keys.</exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this(collection, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        public ConcurrentDictionary(IEqualityComparer<TKey>? comparer) : this(DefaultConcurrencyLevel, DefaultCapacity, true, comparer) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="T:System.Collections.IEnumerable"/>, has the default concurrency level, has the default
        /// initial capacity, and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
            : this(comparer)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see cref="T:System.Collections.IEnumerable"/>,
        /// has the specified concurrency level, has the specified initial capacity, and uses the specified
        /// <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="collection">The <see cref="T:System.Collections.IEnumerable{KeyValuePair{TKey,TValue}}"/> whose elements are copied to the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/> implementation to use
        /// when comparing keys.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="collection"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1.
        /// </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        public ConcurrentDictionary(
            int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
            : this(concurrencyLevel, DefaultCapacity, false, comparer)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            InitializeFromCollection(collection);
        }

        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            TValue dummy;
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key == null) ThrowKeyNullException();

                if (!TryAddInternal(pair.Key, _comparer.GetHashCode(pair.Key), pair.Value, false, false, out dummy))
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
                }
            }

            if (_budget == 0)
            {
                _budget = _tables._buckets.Length / _tables._locks.Length;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1. -or-
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey>? comparer)
            : this(concurrencyLevel, capacity, false, comparer)
        {
        }

        internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey>? comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ConcurrentDictionary_CapacityMustNotBeNegative);
            }

            // The capacity should be at least as large as the concurrency level. Otherwise, we would have locks that don't guard
            // any buckets.
            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            object[] locks = new object[concurrencyLevel];
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i] = new object();
            }

            int[] countPerLock = new int[locks.Length];
            Node[] buckets = new Node[capacity];
            _tables = new Tables(buckets, locks, countPerLock);

            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _growLockArray = growLockArray;
            _budget = buckets.Length / locks.Length;
        }

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey,
        /// TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
        /// in Visual Basic) for reference types.</param>
        /// <returns>true if the key/value pair was added to the <see cref="ConcurrentDictionary{TKey,
        /// TValue}"/>
        /// successfully; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The <see cref="ConcurrentDictionary{TKey, TValue}"/>
        /// contains too many elements.</exception>
        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null) ThrowKeyNullException();
            TValue dummy;
            return TryAddInternal(key, _comparer.GetHashCode(key), value, false, true, out dummy);
        }

        /// <summary>
        /// Determines whether the <see cref="ConcurrentDictionary{TKey, TValue}"/> contains the specified
        /// key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ConcurrentDictionary{TKey,
        /// TValue}"/>.</param>
        /// <returns>true if the <see cref="ConcurrentDictionary{TKey, TValue}"/> contains an element with
        /// the specified key; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool ContainsKey(TKey key)
        {
            if (key == null) ThrowKeyNullException();

            TValue throwAwayValue;
            return TryGetValue(key, out throwAwayValue);
        }

        /// <summary>
        /// Attempts to remove and return the value with the specified key from the
        /// <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object removed from the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> or the default value of <typeparamref
        /// name="TValue"/>
        /// if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryRemove(TKey key, out TValue value) // TODO-NULLABLE-GENERIC
        {
            if (key == null) ThrowKeyNullException();

            return TryRemoveInternal(key, out value, false, default(TValue)!); // TODO-NULLABLE-GENERIC
        }

        /// <summary>
        /// Removes the specified key from the dictionary if it exists and returns its associated value.
        /// If matchValue flag is set, the key will be removed only if is associated with a particular
        /// value.
        /// </summary>
        /// <param name="key">The key to search for and remove if it exists.</param>
        /// <param name="value">The variable into which the removed value, if found, is stored.</param>
        /// <param name="matchValue">Whether removal of the key is conditional on its value.</param>
        /// <param name="oldValue">The conditional value to compare against if <paramref name="matchValue"/> is true</param>
        /// <returns></returns>
        private bool TryRemoveInternal(TKey key, out TValue value, bool matchValue, TValue oldValue) // TODO-NULLABLE-GENERIC
        {
            int hashcode = _comparer.GetHashCode(key);
            while (true)
            {
                Tables tables = _tables;

                int bucketNo, lockNo;
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables._buckets.Length, tables._locks.Length);

                lock (tables._locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurrence.
                    if (tables != _tables)
                    {
                        continue;
                    }

                    Node? prev = null;
                    for (Node curr = tables._buckets[bucketNo]; curr != null; curr = curr._next)
                    {
                        Debug.Assert((prev == null && curr == tables._buckets[bucketNo]) || prev!._next == curr);

                        if (hashcode == curr._hashcode && _comparer.Equals(curr._key, key))
                        {
                            if (matchValue)
                            {
                                bool valuesMatch = EqualityComparer<TValue>.Default.Equals(oldValue, curr._value);
                                if (!valuesMatch)
                                {
                                    value = default(TValue)!; // TODO-NULLABLE-GENERIC
                                    return false;
                                }
                            }

                            if (prev == null)
                            {
                                Volatile.Write<Node>(ref tables._buckets[bucketNo], curr._next);
                            }
                            else
                            {
                                prev._next = curr._next;
                            }

                            value = curr._value;
                            tables._countPerLock[lockNo]--;
                            return true;
                        }
                        prev = curr;
                    }
                }

                value = default(TValue)!; // TODO-NULLABLE-GENERIC
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object from
        /// the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> with the specified key or the default value of
        /// <typeparamref name="TValue"/>, if the operation failed.</param>
        /// <returns>true if the key was found in the <see cref="ConcurrentDictionary{TKey,TValue}"/>;
        /// otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryGetValue(TKey key, out TValue value) // TODO-NULLABLE-GENERIC
        {
            if (key == null) ThrowKeyNullException();
            return TryGetValueInternal(key, _comparer.GetHashCode(key), out value);
        }

        private bool TryGetValueInternal(TKey key, int hashcode, out TValue value) // TODO-NULLABLE-GENERIC
        {
            Debug.Assert(_comparer.GetHashCode(key) == hashcode);

            // We must capture the _buckets field in a local variable. It is set to a new table on each table resize.
            Tables tables = _tables;

            int bucketNo = GetBucket(hashcode, tables._buckets.Length);

            // We can get away w/out a lock here.
            // The Volatile.Read ensures that we have a copy of the reference to tables._buckets[bucketNo].
            // This protects us from reading fields ('_hashcode', '_key', '_value' and '_next') of different instances.
            Node n = Volatile.Read<Node>(ref tables._buckets[bucketNo]);

            while (n != null)
            {
                if (hashcode == n._hashcode && _comparer.Equals(n._key, key))
                {
                    value = n._value;
                    return true;
                }
                n = n._next;
            }

            value = default(TValue)!; // TODO-NULLABLE-GENERIC
            return false;
        }

        /// <summary>
        /// Updates the value associated with <paramref name="key"/> to <paramref name="newValue"/> if the existing value is equal
        /// to <paramref name="comparisonValue"/>.
        /// </summary>
        /// <param name="key">The key whose value is compared with <paramref name="comparisonValue"/> and
        /// possibly replaced.</param>
        /// <param name="newValue">The value that replaces the value of the element with <paramref
        /// name="key"/> if the comparison results in equality.</param>
        /// <param name="comparisonValue">The value that is compared to the value of the element with
        /// <paramref name="key"/>.</param>
        /// <returns>true if the value with <paramref name="key"/> was equal to <paramref
        /// name="comparisonValue"/> and replaced with <paramref name="newValue"/>; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null) ThrowKeyNullException();
            return TryUpdateInternal(key, _comparer.GetHashCode(key), newValue, comparisonValue);
        }

        /// <summary>
        /// Updates the value associated with <paramref name="key"/> to <paramref name="newValue"/> if the existing value is equal
        /// to <paramref name="comparisonValue"/>.
        /// </summary>
        /// <param name="key">The key whose value is compared with <paramref name="comparisonValue"/> and
        /// possibly replaced.</param>
        /// <param name="hashcode">The hashcode computed for <paramref name="key"/>.</param>
        /// <param name="newValue">The value that replaces the value of the element with <paramref
        /// name="key"/> if the comparison results in equality.</param>
        /// <param name="comparisonValue">The value that is compared to the value of the element with
        /// <paramref name="key"/>.</param>
        /// <returns>true if the value with <paramref name="key"/> was equal to <paramref
        /// name="comparisonValue"/> and replaced with <paramref name="newValue"/>; otherwise,
        /// false.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        private bool TryUpdateInternal(TKey key, int hashcode, TValue newValue, TValue comparisonValue)
        {
            Debug.Assert(_comparer.GetHashCode(key) == hashcode);

            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;

            while (true)
            {
                int bucketNo;
                int lockNo;

                Tables tables = _tables;
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables._buckets.Length, tables._locks.Length);

                lock (tables._locks[lockNo])
                {
                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurrence.
                    if (tables != _tables)
                    {
                        continue;
                    }

                    // Try to find this key in the bucket
                    Node? prev = null;
                    for (Node node = tables._buckets[bucketNo]; node != null; node = node._next)
                    {
                        Debug.Assert((prev == null && node == tables._buckets[bucketNo]) || prev!._next == node);
                        if (hashcode == node._hashcode && _comparer.Equals(node._key, key))
                        {
                            if (valueComparer.Equals(node._value, comparisonValue))
                            {
                                if (s_isValueWriteAtomic)
                                {
                                    node._value = newValue;
                                }
                                else
                                {
                                    Node newNode = new Node(node._key, newValue, hashcode, node._next);

                                    if (prev == null)
                                    {
                                        Volatile.Write(ref tables._buckets[bucketNo], newNode);
                                    }
                                    else
                                    {
                                        prev._next = newNode;
                                    }
                                }

                                return true;
                            }

                            return false;
                        }

                        prev = node;
                    }

                    //didn't find the key
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        public void Clear()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                Tables newTables = new Tables(new Node[DefaultCapacity], _tables._locks, new int[_tables._countPerLock.Length]);
                _tables = newTables;
                _budget = Math.Max(1, newTables._buckets.Length / newTables._locks.Length);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection"/> to an array of
        /// type <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>, starting at the
        /// specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array of type <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// that is the destination of the <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/> elements copied from the <see
        /// cref="T:System.Collections.ICollection"/>. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), SR.ConcurrentDictionary_IndexIsNegative);

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = 0;

                for (int i = 0; i < _tables._locks.Length && count >= 0; i++)
                {
                    count += _tables._countPerLock[i];
                }

                if (array.Length - count < index || count < 0) //"count" itself or "count + index" can overflow
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
                }

                CopyToPairs(array, index);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ConcurrentDictionary{TKey,TValue}"/> to a
        /// new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                int count = 0;
                checked
                {
                    for (int i = 0; i < _tables._locks.Length; i++)
                    {
                        count += _tables._countPerLock[i];
                    }
                }

                if (count == 0)
                {
                    return Array.Empty<KeyValuePair<TKey, TValue>>();
                }

                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];
                CopyToPairs(array, 0);
                return array;
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        ///
        /// Important: the caller must hold all locks in _locks before calling CopyToPairs.
        /// </summary>
        private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
        {
            Node[] buckets = _tables._buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current._next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current._key, current._value);
                    index++; //this should never flow, CopyToPairs is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        ///
        /// Important: the caller must hold all locks in _locks before calling CopyToEntries.
        /// </summary>
        private void CopyToEntries(DictionaryEntry[] array, int index)
        {
            Node[] buckets = _tables._buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current._next)
                {
                    array[index] = new DictionaryEntry(current._key, current._value);
                    index++;  //this should never flow, CopyToEntries is only called when there's no overflow risk
                }
            }
        }

        /// <summary>
        /// Copy dictionary contents to an array - shared implementation between ToArray and CopyTo.
        ///
        /// Important: the caller must hold all locks in _locks before calling CopyToObjects.
        /// </summary>
        private void CopyToObjects(object[] array, int index)
        {
            Node[] buckets = _tables._buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current._next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current._key, current._value);
                    index++; //this should never flow, CopyToObjects is only called when there's no overflow risk
                }
            }
        }

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Node[] buckets = _tables._buckets;

            for (int i = 0; i < buckets.Length; i++)
            {
                // The Volatile.Read ensures that we have a copy of the reference to buckets[i].
                // This protects us from reading fields ('_key', '_value' and '_next') of different instances.
                Node current = Volatile.Read<Node>(ref buckets[i]);

                while (current != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current._key, current._value);
                    current = current._next;
                }
            }
        }

        /// <summary>
        /// Shared internal implementation for inserts and updates.
        /// If key exists, we always return false; and if updateIfExists == true we force update with value;
        /// If key doesn't exist, we always add value and return true;
        /// </summary>
        private bool TryAddInternal(TKey key, int hashcode, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
        {
            Debug.Assert(_comparer.GetHashCode(key) == hashcode);

            while (true)
            {
                int bucketNo, lockNo;

                Tables tables = _tables;
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables._buckets.Length, tables._locks.Length);

                bool resizeDesired = false;
                bool lockTaken = false;
                try
                {
                    if (acquireLock)
                        Monitor.Enter(tables._locks[lockNo], ref lockTaken);

                    // If the table just got resized, we may not be holding the right lock, and must retry.
                    // This should be a rare occurrence.
                    if (tables != _tables)
                    {
                        continue;
                    }

                    // Try to find this key in the bucket
                    Node? prev = null;
                    for (Node node = tables._buckets[bucketNo]; node != null; node = node._next)
                    {
                        Debug.Assert((prev == null && node == tables._buckets[bucketNo]) || prev!._next == node);
                        if (hashcode == node._hashcode && _comparer.Equals(node._key, key))
                        {
                            // The key was found in the dictionary. If updates are allowed, update the value for that key.
                            // We need to create a new node for the update, in order to support TValue types that cannot
                            // be written atomically, since lock-free reads may be happening concurrently.
                            if (updateIfExists)
                            {
                                if (s_isValueWriteAtomic)
                                {
                                    node._value = value;
                                }
                                else
                                {
                                    Node newNode = new Node(node._key, value, hashcode, node._next);
                                    if (prev == null)
                                    {
                                        Volatile.Write(ref tables._buckets[bucketNo], newNode);
                                    }
                                    else
                                    {
                                        prev._next = newNode;
                                    }
                                }
                                resultingValue = value;
                            }
                            else
                            {
                                resultingValue = node._value;
                            }
                            return false;
                        }
                        prev = node;
                    }

                    // The key was not found in the bucket. Insert the key-value pair.
                    Volatile.Write<Node>(ref tables._buckets[bucketNo], new Node(key, value, hashcode, tables._buckets[bucketNo]));
                    checked
                    {
                        tables._countPerLock[lockNo]++;
                    }

                    //
                    // If the number of elements guarded by this lock has exceeded the budget, resize the bucket table.
                    // It is also possible that GrowTable will increase the budget but won't resize the bucket table.
                    // That happens if the bucket table is found to be poorly utilized due to a bad hash function.
                    //
                    if (tables._countPerLock[lockNo] > _budget)
                    {
                        resizeDesired = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(tables._locks[lockNo]);
                }

                //
                // The fact that we got here means that we just performed an insertion. If necessary, we will grow the table.
                //
                // Concurrency notes:
                // - Notice that we are not holding any locks at when calling GrowTable. This is necessary to prevent deadlocks.
                // - As a result, it is possible that GrowTable will be called unnecessarily. But, GrowTable will obtain lock 0
                //   and then verify that the table we passed to it as the argument is still the current table.
                //
                if (resizeDesired)
                {
                    GrowTable(tables);
                }

                resultingValue = value;
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a
        /// <see cref="T:System.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new
        /// element with the specified key.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        /// <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                {
                    ThrowKeyNotFoundException(key);
                }
                return value;
            }
            set
            {
                if (key == null) ThrowKeyNullException();
                TValue dummy;
                TryAddInternal(key, _comparer.GetHashCode(key), value, true, true, out dummy);
            }
        }

        // These exception throwing sites have been extracted into their own NoInlining methods
        // as these are uncommonly needed and when inlined are observed to prevent the inlining
        // of important methods like TryGetValue and ContainsKey.

        private static void ThrowKeyNotFoundException(object key)
        {
            throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
        }

        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfInvalidObjectValue(object? value)
        {
            if (value != null)
            {
                if (!(value is TValue))
                {
                    ThrowValueNullException();
                }
            }
            else if (default(TValue)! != null) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34757
            {
                ThrowValueNullException();
            }
        }

        private static void ThrowValueNullException()
        {
            throw new ArgumentException(SR.ConcurrentDictionary_TypeOfValueIncorrect);
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value>The number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        public int Count
        {
            get
            {
                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    return GetCountInternal();
                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }
            }
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>. Should only be used after all locks
        /// have been acquired.
        /// </summary>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value>The number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        private int GetCountInternal()
        {
            int count = 0;

            // Compute the count, we allow overflow
            for (int i = 0; i < _tables._countPerLock.Length; i++)
            {
                count += _tables._countPerLock[i];
            }

            return count;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null) ThrowKeyNullException();
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

            int hashcode = _comparer.GetHashCode(key);

            TValue resultingValue;
            if (!TryGetValueInternal(key, hashcode, out resultingValue))
            {
                TryAddInternal(key, hashcode, valueFactory(key), false, true, out resultingValue);
            }
            return resultingValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (key == null) ThrowKeyNullException();
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

            int hashcode = _comparer.GetHashCode(key);

            TValue resultingValue;
            if (!TryGetValueInternal(key, hashcode, out resultingValue))
            {
                TryAddInternal(key, hashcode, valueFactory(key, factoryArgument), false, true, out resultingValue);
            }
            return resultingValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">the value to be added, if the key does not already exist</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null) ThrowKeyNullException();

            int hashcode = _comparer.GetHashCode(key);

            TValue resultingValue;
            if (!TryGetValueInternal(key, hashcode, out resultingValue))
            {
                TryAddInternal(key, hashcode, value, false, true, out resultingValue);
            }
            return resultingValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key
        /// based on the key's existing value</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate<TArg>(
            TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            if (key == null) ThrowKeyNullException();
            if (addValueFactory == null) throw new ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            int hashcode = _comparer.GetHashCode(key);

            while (true)
            {
                TValue oldValue;
                if (TryGetValueInternal(key, hashcode, out oldValue))
                {
                    // key exists, try to update
                    TValue newValue = updateValueFactory(key, oldValue, factoryArgument);
                    if (TryUpdateInternal(key, hashcode, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else
                {
                    // key doesn't exist, try to add
                    TValue resultingValue;
                    if (TryAddInternal(key, hashcode, addValueFactory(key, factoryArgument), false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key
        /// based on the key's existing value</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) ThrowKeyNullException();
            if (addValueFactory == null) throw new ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            int hashcode = _comparer.GetHashCode(key);

            while (true)
            {
                TValue oldValue;
                if (TryGetValueInternal(key, hashcode, out oldValue))
                {
                    // key exists, try to update
                    TValue newValue = updateValueFactory(key, oldValue);
                    if (TryUpdateInternal(key, hashcode, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else
                {
                    // key doesn't exist, try to add
                    TValue resultingValue;
                    if (TryAddInternal(key, hashcode, addValueFactory(key), false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key does not already
        /// exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey,TValue}"/> if the key
        /// already exists.
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on
        /// the key's existing value</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either the value of addValue (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null) ThrowKeyNullException();
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            int hashcode = _comparer.GetHashCode(key);

            while (true)
            {
                TValue oldValue;
                if (TryGetValueInternal(key, hashcode, out oldValue))
                {
                    // key exists, try to update
                    TValue newValue = updateValueFactory(key, oldValue);
                    if (TryUpdateInternal(key, hashcode, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else
                {
                    // key doesn't exist, try to add
                    TValue resultingValue;
                    if (TryAddInternal(key, hashcode, addValue, false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentDictionary{TKey,TValue}"/> is empty.
        /// </summary>
        /// <value>true if the <see cref="ConcurrentDictionary{TKey,TValue}"/> is empty; otherwise,
        /// false.</value>
        public bool IsEmpty
        {
            get
            {
                // Check if any buckets are non-empty, without acquiring any locks.
                // This fast path should generally suffice as collections are usually not empty.
                if (!AreAllBucketsEmpty())
                {
                    return false;
                }

                // We didn't see any buckets containing items, however we can't be sure
                // the collection was actually empty at any point in time as items may have been
                // added and removed while iterating over the buckets such that we never saw an
                // empty bucket, but there was always an item present in at least one bucket.
                int acquiredLocks = 0;
                try
                {
                    // Acquire all locks
                    AcquireAllLocks(ref acquiredLocks);

                    return AreAllBucketsEmpty();
                }
                finally
                {
                    // Release locks that have been acquired earlier
                    ReleaseLocks(0, acquiredLocks);
                }

                bool AreAllBucketsEmpty()
                {
                    int[] countPerLock = _tables._countPerLock;

                    for (int i = 0; i < countPerLock.Length; i++)
                    {
                        if (countPerLock[i] != 0)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        #region IDictionary<TKey,TValue> members

        /// <summary>
        /// Adds the specified key and value to the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// An element with the same key already exists in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</exception>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException(SR.ConcurrentDictionary_KeyAlreadyExisted);
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully remove; otherwise false. This method also returns
        /// false if
        /// <paramref name="key"/> was not found in the original <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            TValue throwAwayValue;
            return TryRemove(key, out throwAwayValue);
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TKey}"/> containing the keys in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TKey> Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.ICollection{TValue}"/> containing the values in
        /// the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TValue> Values
        {
            get { return GetValues(); }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IEnumerable{TValue}"/> containing the values
        /// in the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.Generic.IEnumerable{TValue}"/> containing the
        /// values in the <see cref="T:System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get { return GetValues(); }
        }
        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <summary>
        /// Adds the specified value to the <see cref="T:System.Collections.Generic.ICollection{TValue}"/>
        /// with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to add to the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="keyValuePair"/> of <paramref
        /// name="keyValuePair"/> is null.</exception>
        /// <exception cref="T:System.OverflowException">The <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the
        /// <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/></exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>
        /// contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure to locate in the <see
        /// cref="T:System.Collections.Generic.ICollection{TValue}"/>.</param>
        /// <returns>true if the <paramref name="keyValuePair"/> is found in the <see
        /// cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/>; otherwise, false.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TValue value;
            if (!TryGetValue(keyValuePair.Key, out value))
            {
                return false;
            }
            return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.ICollection{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>, this property always returns
        /// false.</value>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes a key and value from the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see
        /// cref="T:System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to remove from the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the key and value represented by <paramref name="keyValuePair"/> is successfully
        /// found and removed; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">The Key property of <paramref
        /// name="keyValuePair"/> is a null reference (Nothing in Visual Basic).</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (keyValuePair.Key == null) throw new ArgumentNullException(nameof(keyValuePair), SR.ConcurrentDictionary_ItemKeyIsNull);

            TValue throwAwayValue;
            return TryRemoveInternal(keyValuePair.Key, out throwAwayValue, true, keyValuePair.Value);
        }

        #endregion

        #region IEnumerable Members

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ConcurrentDictionary<TKey, TValue>)this).GetEnumerator();
        }

        #endregion

        #region IDictionary Members

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key.</param>
        /// <param name="value">The object to use as the value.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="T:System.ArgumentException">
        /// <paramref name="key"/> is of a type that is not assignable to the key type <typeparamref
        /// name="TKey"/> of the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>. -or-
        /// <paramref name="value"/> is of a type that is not assignable to <typeparamref name="TValue"/>,
        /// the type of values in the <see cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// -or- A value with the same key already exists in the <see
        /// cref="T:System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </exception>
        void IDictionary.Add(object key, object? value)
        {
            if (key == null) ThrowKeyNullException();
            if (!(key is TKey)) throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
            ThrowIfInvalidObjectValue(value);

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, (TValue)value!);
        }

        /// <summary>
        /// Gets whether the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains an
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> contains
        /// an element with the specified key; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException"> <paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary.Contains(object key)
        {
            if (key == null) ThrowKeyNullException();

            return (key is TKey) && this.ContainsKey((TKey)key);
        }

        /// <summary>Provides an <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the
        /// <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An <see cref="T:System.Collections.Generics.IDictionaryEnumerator"/> for the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a fixed size.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> has a
        /// fixed size; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is read-only.
        /// </summary>
        /// <value>true if the <see cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the keys of the <see
        /// cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>.</value>
        ICollection IDictionary.Keys
        {
            get { return GetKeys(); }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        void IDictionary.Remove(object key)
        {
            if (key == null) ThrowKeyNullException();

            TValue throwAwayValue;
            if (key is TKey)
            {
                TryRemove((TKey)key, out throwAwayValue);
            }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="T:System.Collections.ICollection"/> containing the values in the <see
        /// cref="T:System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Values
        {
            get { return GetValues(); }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key, or a null reference (Nothing in Visual Basic)
        /// if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is
        /// not assignable to the key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentException">
        /// A value is being assigned, and <paramref name="key"/> is of a type that is not assignable to the
        /// key type <typeparamref name="TKey"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>. -or- A value is being
        /// assigned, and <paramref name="key"/> is of a type that is not assignable to the value type
        /// <typeparamref name="TValue"/> of the <see
        /// cref="T:System.Collections.Generic.ConcurrentDictionary{TKey,TValue}"/>
        /// </exception>
        object? IDictionary.this[object key]
        {
            get
            {
                if (key == null) ThrowKeyNullException();

                TValue value;
                if (key is TKey && TryGetValue((TKey)key, out value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (key == null) ThrowKeyNullException();

                if (!(key is TKey)) throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
                ThrowIfInvalidObjectValue(value);

                ((ConcurrentDictionary<TKey, TValue>)this)[(TKey)key] = (TValue)value!; // TODO-NULLABLE: https://github.com/dotnet/csharplang/issues/538
            }
        }

        #endregion

        #region ICollection Members

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an array, starting
        /// at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from
        /// the <see cref="T:System.Collections.ICollection"/>. The array must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="T:System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), SR.ConcurrentDictionary_IndexIsNegative);

            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                Tables tables = _tables;

                int count = 0;

                for (int i = 0; i < tables._locks.Length && count >= 0; i++)
                {
                    count += tables._countPerLock[i];
                }

                if (array.Length - count < index || count < 0) //"count" itself or "count + index" can overflow
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
                }

                // To be consistent with the behavior of ICollection.CopyTo() in Dictionary<TKey,TValue>,
                // we recognize three types of target arrays:
                //    - an array of KeyValuePair<TKey, TValue> structs
                //    - an array of DictionaryEntry structs
                //    - an array of objects

                KeyValuePair<TKey, TValue>[]? pairs = array as KeyValuePair<TKey, TValue>[];
                if (pairs != null)
                {
                    CopyToPairs(pairs, index);
                    return;
                }

                DictionaryEntry[]? entries = array as DictionaryEntry[];
                if (entries != null)
                {
                    CopyToEntries(entries, index);
                    return;
                }

                object[]? objects = array as object[];
                if (objects != null)
                {
                    CopyToObjects(objects, index);
                    return;
                }

                throw new ArgumentException(SR.ConcurrentDictionary_ArrayIncorrectType, nameof(array));
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized
        /// (thread safe); otherwise, false. For <see
        /// cref="T:System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="T:System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
            }
        }

        #endregion

        /// <summary>
        /// Replaces the bucket table with a larger one. To prevent multiple threads from resizing the
        /// table as a result of races, the Tables instance that holds the table of buckets deemed too
        /// small is passed in as an argument to GrowTable(). GrowTable() obtains a lock, and then checks
        /// the Tables instance has been replaced in the meantime or not.
        /// </summary>
        private void GrowTable(Tables tables)
        {
            const int MaxArrayLength = 0X7FEFFFFF;
            int locksAcquired = 0;
            try
            {
                // The thread that first obtains _locks[0] will be the one doing the resize operation
                AcquireLocks(0, 1, ref locksAcquired);

                // Make sure nobody resized the table while we were waiting for lock 0:
                if (tables != _tables)
                {
                    // We assume that since the table reference is different, it was already resized (or the budget
                    // was adjusted). If we ever decide to do table shrinking, or replace the table for other reasons,
                    // we will have to revisit this logic.
                    return;
                }

                // Compute the (approx.) total size. Use an Int64 accumulation variable to avoid an overflow.
                long approxCount = 0;
                for (int i = 0; i < tables._countPerLock.Length; i++)
                {
                    approxCount += tables._countPerLock[i];
                }

                //
                // If the bucket array is too empty, double the budget instead of resizing the table
                //
                if (approxCount < tables._buckets.Length / 4)
                {
                    _budget = 2 * _budget;
                    if (_budget < 0)
                    {
                        _budget = int.MaxValue;
                    }
                    return;
                }


                // Compute the new table size. We find the smallest integer larger than twice the previous table size, and not divisible by
                // 2,3,5 or 7. We can consider a different table-sizing policy in the future.
                int newLength = 0;
                bool maximizeTableSize = false;
                try
                {
                    checked
                    {
                        // Double the size of the buckets table and add one, so that we have an odd integer.
                        newLength = tables._buckets.Length * 2 + 1;

                        // Now, we only need to check odd integers, and find the first that is not divisible
                        // by 3, 5 or 7.
                        while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0)
                        {
                            newLength += 2;
                        }

                        Debug.Assert(newLength % 2 != 0);

                        if (newLength > MaxArrayLength)
                        {
                            maximizeTableSize = true;
                        }
                    }
                }
                catch (OverflowException)
                {
                    maximizeTableSize = true;
                }

                if (maximizeTableSize)
                {
                    newLength = MaxArrayLength;

                    // We want to make sure that GrowTable will not be called again, since table is at the maximum size.
                    // To achieve that, we set the budget to int.MaxValue.
                    //
                    // (There is one special case that would allow GrowTable() to be called in the future:
                    // calling Clear() on the ConcurrentDictionary will shrink the table and lower the budget.)
                    _budget = int.MaxValue;
                }

                // Now acquire all other locks for the table
                AcquireLocks(1, tables._locks.Length, ref locksAcquired);

                object[] newLocks = tables._locks;

                // Add more locks
                if (_growLockArray && tables._locks.Length < MaxLockNumber)
                {
                    newLocks = new object[tables._locks.Length * 2];
                    Array.Copy(tables._locks, 0, newLocks, 0, tables._locks.Length);
                    for (int i = tables._locks.Length; i < newLocks.Length; i++)
                    {
                        newLocks[i] = new object();
                    }
                }

                Node[] newBuckets = new Node[newLength];
                int[] newCountPerLock = new int[newLocks.Length];

                // Copy all data into a new table, creating new nodes for all elements
                for (int i = 0; i < tables._buckets.Length; i++)
                {
                    Node current = tables._buckets[i];
                    while (current != null)
                    {
                        Node next = current._next;
                        int newBucketNo, newLockNo;
                        GetBucketAndLockNo(current._hashcode, out newBucketNo, out newLockNo, newBuckets.Length, newLocks.Length);

                        newBuckets[newBucketNo] = new Node(current._key, current._value, current._hashcode, newBuckets[newBucketNo]);

                        checked
                        {
                            newCountPerLock[newLockNo]++;
                        }

                        current = next;
                    }
                }

                // Adjust the budget
                _budget = Math.Max(1, newBuckets.Length / newLocks.Length);

                // Replace tables with the new versions
                _tables = new Tables(newBuckets, newLocks, newCountPerLock);
            }
            finally
            {
                // Release all locks that we took earlier
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Computes the bucket for a particular key.
        /// </summary>
        private static int GetBucket(int hashcode, int bucketCount)
        {
            int bucketNo = (hashcode & 0x7fffffff) % bucketCount;
            Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
            return bucketNo;
        }

        /// <summary>
        /// Computes the bucket and lock number for a particular key.
        /// </summary>
        private static void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount)
        {
            bucketNo = (hashcode & 0x7fffffff) % bucketCount;
            lockNo = bucketNo % lockCount;

            Debug.Assert(bucketNo >= 0 && bucketNo < bucketCount);
            Debug.Assert(lockNo >= 0 && lockNo < lockCount);
        }

        /// <summary>
        /// The number of concurrent writes for which to optimize by default.
        /// </summary>
        private static int DefaultConcurrencyLevel
        {
            get { return PlatformHelper.ProcessorCount; }
        }

        /// <summary>
        /// Acquires all locks for this hash table, and increments locksAcquired by the number
        /// of locks that were successfully acquired. The locks are acquired in an increasing
        /// order.
        /// </summary>
        private void AcquireAllLocks(ref int locksAcquired)
        {
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(_tables._buckets.Length);
            }

            // First, acquire lock 0
            AcquireLocks(0, 1, ref locksAcquired);

            // Now that we have lock 0, the _locks array will not change (i.e., grow),
            // and so we can safely read _locks.Length.
            AcquireLocks(1, _tables._locks.Length, ref locksAcquired);
            Debug.Assert(locksAcquired == _tables._locks.Length);
        }

        /// <summary>
        /// Acquires a contiguous range of locks for this hash table, and increments locksAcquired
        /// by the number of locks that were successfully acquired. The locks are acquired in an
        /// increasing order.
        /// </summary>
        private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
        {
            Debug.Assert(fromInclusive <= toExclusive);
            object[] locks = _tables._locks;

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(locks[i], ref lockTaken);
                }
                finally
                {
                    if (lockTaken)
                    {
                        locksAcquired++;
                    }
                }
            }
        }

        /// <summary>
        /// Releases a contiguous range of locks.
        /// </summary>
        private void ReleaseLocks(int fromInclusive, int toExclusive)
        {
            Debug.Assert(fromInclusive <= toExclusive);

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                Monitor.Exit(_tables._locks[i]);
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TKey> GetKeys()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = GetCountInternal();
                if (count < 0) throw new OutOfMemoryException();

                List<TKey> keys = new List<TKey>(count);
                for (int i = 0; i < _tables._buckets.Length; i++)
                {
                    Node current = _tables._buckets[i];
                    while (current != null)
                    {
                        keys.Add(current._key);
                        current = current._next;
                    }
                }

                return new ReadOnlyCollection<TKey>(keys);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TValue> GetValues()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);

                int count = GetCountInternal();
                if (count < 0) throw new OutOfMemoryException();

                List<TValue> values = new List<TValue>(count);
                for (int i = 0; i < _tables._buckets.Length; i++)
                {
                    Node current = _tables._buckets[i];
                    while (current != null)
                    {
                        values.Add(current._value);
                        current = current._next;
                    }
                }

                return new ReadOnlyCollection<TValue>(values);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        /// <summary>
        /// A node in a singly-linked list representing a particular hash table bucket.
        /// </summary>
        private sealed class Node
        {
            internal readonly TKey _key;
            internal TValue _value;
            internal volatile Node _next;
            internal readonly int _hashcode;

            internal Node(TKey key, TValue value, int hashcode, Node next)
            {
                _key = key;
                _value = value;
                _next = next;
                _hashcode = hashcode;
            }
        }

        /// <summary>
        /// A private class to represent enumeration over the dictionary that implements the
        /// IDictionaryEnumerator interface.
        /// </summary>
        private sealed class DictionaryEnumerator : IDictionaryEnumerator
        {
            IEnumerator<KeyValuePair<TKey, TValue>> _enumerator; // Enumerator over the dictionary.

            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get { return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value); }
            }

            public object Key
            {
                get { return _enumerator.Current.Key; }
            }

            public object? Value
            {
                get { return _enumerator.Current.Value; }
            }

#pragma warning disable CS8612 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268
            public object Current
#pragma warning restore CS8612
            {
                get { return Entry; }
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }
    }

    internal sealed class IDictionaryDebugView<K, V>
    {
        private readonly IDictionary<K, V> _dictionary;

        public IDictionaryDebugView(IDictionary<K, V> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            _dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[_dictionary.Count];
                _dictionary.CopyTo(items, 0);
                return items;
            }
        }
    }
}
