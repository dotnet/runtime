// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

// No one of these warnings actual for this class.
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

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
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        // Lock-Free hash table implementation. CAS operations are used only
        // to sync buckets, which allows threads working with different buckets
        // to have less friction. Wait-Free count is also used, which allowed to
        // abandon Interlocked.Add. All public methods are thread-safe. The
        // enumerator does not lock the hash table while reading.
        // See for more information https://www.linkedin.com/pulse/lock-free-hash-table-maksim-burtsev/

        // The default value that is used if the user has not specified a capacity.
        private const int DEFAULT_CAPACITY = 127;
        // The multiplier is used when expanding the array.
        private const int GROW_MULTIPLIER = 2;
        // The number by which the multiplier increases in an unsuccessful attempt.
        private const int GROW_MULTIPLIER_STEP = 1;
        // The default array size of counts
        private const int COUNTS_SIZE = 16;
        // The thread Id
        [ThreadStatic] private static int t_id;
        // All current data is collected here.
        private HashTableData _data;
        // Initial table size
        private readonly int _capacity;
        // To compare keys
        private readonly IEqualityComparer<TKey> _keysComparer;
        // To compare values
        private readonly IEqualityComparer<TValue> _valuesComparer;

        #region ' Public Interface '

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the default concurrency level, has the default initial capacity, and
        /// uses the default comparer for the key type.
        /// </summary>
        public ConcurrentDictionary()
            : this(DEFAULT_CAPACITY, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the default
        /// comparer for the key type.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <exception cref="System.ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        public ConcurrentDictionary(int capacity)
            : this(capacity, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level and capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
            : this(DEFAULT_CAPACITY, comparer, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : this(capacity, comparer, EqualityComparer<TValue>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/>, has the default concurrency
        /// level, has the default initial capacity, and uses the default comparer for the key type.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentException"><paramref name="collection"/> contains one or more
        /// duplicate keys.</exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, EqualityComparer<TKey>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see
        /// cref="System.Collections.IEnumerable"/>, has the default concurrency level, has the default
        /// initial capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="collection">The <see
        /// cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to
        /// the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="collection"/> is a null reference
        /// (Nothing in Visual Basic). -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(DEFAULT_CAPACITY, comparer, EqualityComparer<TValue>.Default)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            InitializeFromCollection(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="keysComparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <param name="valuesComparer">The <see cref="System.Collections.Generic.IEqualityComparer{TValue}"/>
        /// implementation to use when comparing values.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="keysComparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valuesComparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public ConcurrentDictionary(int capacity, IEqualityComparer<TKey> keysComparer, IEqualityComparer<TValue> valuesComparer)
        {
            if (valuesComparer == null)
            {
                throw new ArgumentNullException(nameof(valuesComparer));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ConcurrentDictionary_CapacityMustNotBeNegative);
            }

            // save capacity value for Clear() method
            _capacity       = capacity;
            _keysComparer   = keysComparer ?? EqualityComparer<TKey>.Default;
            _valuesComparer = valuesComparer;
            _data = new HashTableData(capacity, COUNTS_SIZE);

            // Counts initialization
            for (var i = 0; i < _data.Counts.Length; ++i)
            {
                _data.Counts[i] = new ConcurrentDictionaryCounter();
            }
        }

        #region ' Deprecated '

        // This constructors are deprecated because concurrencyLevel not existed in current realization
        // ------------------------------------------

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
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is
        /// less than 1.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"> <paramref name="capacity"/> is less than
        /// 0.</exception>
        [Obsolete ("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(int capacity)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, int capacity)
            : this(capacity)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that is empty, has the specified concurrency level, has the specified initial capacity, and
        /// uses the specified <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="capacity">The initial number of elements that the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// can contain.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>
        /// implementation to use when comparing keys.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1. -or-
        /// <paramref name="capacity"/> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="comparer"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        [Obsolete("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(int capacity, IEqualityComparer<TKey> comparer)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(capacity, comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// class that contains elements copied from the specified <see cref="System.Collections.IEnumerable"/>,
        /// has the specified concurrency level, has the specified initial capacity, and uses the specified
        /// <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> concurrently.</param>
        /// <param name="collection">The <see cref="System.Collections.Generic.IEnumerable{T}"/> whose elements are copied to the new
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{TKey}"/> implementation to use
        /// when comparing keys.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="collection"/> is a null reference (Nothing in Visual Basic).
        /// -or-
        /// <paramref name="comparer"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="concurrencyLevel"/> is less than 1.
        /// </exception>
        /// <exception cref="System.ArgumentException"><paramref name="collection"/> contains one or more duplicate keys.</exception>
        [Obsolete("This constructor will soon be deprecated. Use 'ConcurrentDictionary<TKey,TValue>(IEnumerable<KeyValuePair<TKey, TValue>> collection, int capacity)' instead")]
        public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(collection, comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel), SR.ConcurrentDictionary_ConcurrencyLevelMustBePositive);
            }
        }

        #endregion

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <value>The number of key/value paris contained in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <remarks>Count has snapshot semantics and represents the number of items in the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>
        /// at the moment when Count was accessed.</remarks>
        public int Count
        {
            get
            {
                var count = 0L;

                unchecked
                {
                    var counts = _data.Counts;

                    for (var i = 0; i < counts.Length; ++i)
                    {
#if TARGET_32BIT
                        // The Read method is unnecessary on 64-bit systems, because 64-bit
                        // read operations are already atomic. On 32-bit systems, 64-bit read
                        // operations are not atomic unless performed using Read.
                        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked.read?view=netframework-4.8
                        count += Interlocked.Read(ref counts[i].Count);
#else
                        count += counts[i].Count;
#endif
                    }
                }

                return (int)count;
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
                var count = 0L;

                unchecked
                {
                    var counts = _data.Counts;

                    for (var i = 0; i < counts.Length; ++i)
                    {
#if TARGET_32BIT
                        count += Interlocked.Read(ref counts[i].Count);
#else
                        count += counts[i].Count;
#endif
                    }
                }

                return count == 0L;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key. If the specified key is not found, a get
        /// operation throws a
        /// <see cref="System.Collections.Generic.KeyNotFoundException"/>, and a set operation creates a new
        /// element with the specified key.</value>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The property is retrieved and
        /// <paramref name="key"/>
        /// does not exist in the collection.</exception>
        public TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    ThrowKeyNotFoundException(key);
                }

                return value;
            }
            set
            {
                if (key == null)
                {
                    ThrowKeyNullException();
                }

                AddOrUpdate(key, value, value);
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ConcurrentDictionary{TKey,TValue}"/> contains the specified
        /// key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="ConcurrentDictionary{TKey,TValue}"/> contains an element with
        /// the specified key; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var comp   = _keysComparer;
            var frame  = _data.Frame;
            var index  = (comp.GetHashCode(key) & 0x7fffffff) % frame.HashMaster;
            var sync   = frame.SyncTable[index];

            // return if empty
            if (sync == (int)RecordStatus.Empty)
            {
                return false;
            }

            var keys = frame.KeysTable[index];

            // check exist
            if (
                    (sync & (int)RecordStatus.HasValue0) != 0
                                    &&
                    comp.Equals(key, keys.Key0)
                                    ||
                    (sync & (int)RecordStatus.HasValue1) != 0
                                    &&
                    comp.Equals(key, keys.Key1)
                                    ||
                    (sync & (int)RecordStatus.HasValue2) != 0
                                    &&
                    comp.Equals(key, keys.Key2)
                                    ||
                    (sync & (int)RecordStatus.HasValue3) != 0
                                    &&
                    comp.Equals(key, keys.Key3)
                )
            {
                return true;
            }

            return false;
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
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            unchecked
            {
                var comp   = _keysComparer;
                var frame  = _data.Frame;
                var index  = (comp.GetHashCode(key) & 0x7fffffff) % frame.HashMaster;
                var sync   = frame.SyncTable[index];

                // return if empty
                if (sync == (int)RecordStatus.Empty)
                {
                    value = default;

                    return false;
                }

                var vals = frame.ValuesTable[index];
                var keys = frame.KeysTable[index];

                // check cell 0
                if (
                        (sync & (int)RecordStatus.HasValue0) != 0
                                    &&
                        comp.Equals(key, keys.Key0)
                   )
                {
                    value = vals.Value0;

                    return true;
                }

                // check cell 1
                if (
                        (sync & (int)RecordStatus.HasValue1) != 0
                                    &&
                        comp.Equals(key, keys.Key1)
                   )
                {
                    value = vals.Value1;

                    return true;
                }

                // check cell 2
                if (
                        (sync & (int)RecordStatus.HasValue2) != 0
                                    &&
                        comp.Equals(key, keys.Key2)
                   )
                {
                    value = vals.Value2;

                    return true;
                }

                // check cell 3
                if (
                        (sync & (int)RecordStatus.HasValue3) != 0
                                    &&
                        comp.Equals(key, keys.Key3)
                   )
                {
                    value = vals.Value3;

                    return true;
                }

                // not exist
                value = default;

                return false;
            }
        }

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference (Nothing
        /// in Visual Basic) for reference types.</param>
        /// <returns>true if the key/value pair was added to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// successfully; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        public bool TryAdd(TKey key, TValue value)
        {
            // TODO Remove here. To pass EtwTests.
            if (CDSCollectionETWBCLProvider.Log.IsEnabled())
            {
                CDSCollectionETWBCLProvider.Log.ConcurrentDictionary_AcquiringAllLocks(3);
            }

            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        var flag = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // check exist
                                    if (
                                            (sync & (int)RecordStatus.HasValue1) != 0
                                                            &&
                                            comp.Equals(key, keys.Key1)
                                                            ||
                                            (sync & (int)RecordStatus.HasValue2) != 0
                                                            &&
                                            comp.Equals(key, keys.Key2)
                                                            ||
                                            (sync & (int)RecordStatus.HasValue3) != 0
                                                            &&
                                            comp.Equals(key, keys.Key3)
                                        )
                                    {
                                        syncs[index] = sync;

                                        return false;
                                    }

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // check exist
                                    if (
                                            comp.Equals(key, keys.Key0)
                                                        ||
                                            (sync & (int)RecordStatus.HasValue2) != 0
                                                        &&
                                            comp.Equals(key, keys.Key2)
                                                        ||
                                            (sync & (int)RecordStatus.HasValue3) != 0
                                                        &&
                                            comp.Equals(key, keys.Key3)
                                        )
                                    {
                                        syncs[index] = sync;

                                        return false;
                                    }

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = value;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // check exist
                                    if (
                                            comp.Equals(key, keys.Key0)
                                                        ||
                                            comp.Equals(key, keys.Key1)
                                                        ||
                                            (sync & (int)RecordStatus.HasValue3) != 0
                                                        &&
                                            comp.Equals(key, keys.Key3)
                                        )
                                    {
                                        syncs[index] = sync;

                                        return false;
                                    }

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // check exist
                                    if (
                                            comp.Equals(key, keys.Key0)
                                                        ||
                                            comp.Equals(key, keys.Key1)
                                                        ||
                                            comp.Equals(key, keys.Key2)
                                        )
                                    {
                                        syncs[index] = sync;

                                        return false;
                                    }

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return true;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var keys = keyst[index];

                        // check exist
                        if (
                                comp.Equals(key, keys.Key0)
                                            ||
                                comp.Equals(key, keys.Key1)
                                            ||
                                comp.Equals(key, keys.Key2)
                                            ||
                                comp.Equals(key, keys.Key3)
                            )
                        {
                            return false;
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>Removes a key and value from the dictionary.</summary>
        /// <param name="item">The <see cref="KeyValuePair{TKey,TValue}"/> representing the key and value to remove.</param>
        /// <returns>
        /// true if the key and value represented by <paramref name="item"/> are successfully
        /// found and removed; otherwise, false.
        /// </returns>
        /// <remarks>
        /// Both the specifed key and value must match the entry in the dictionary for it to be removed.
        /// The key is compared using the dictionary's comparer (or the default comparer for <typeparamref name="TKey"/>
        /// if no comparer was provided to the dictionary when it was constructed).  The value is compared using the
        /// default comparer for <typeparamref name="TValue"/>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// The <see cref="KeyValuePair{TKey, TValue}.Key"/> property of <paramref name="item"/> is a null reference.
        /// </exception>
        public bool TryRemove(KeyValuePair<TKey, TValue> item)
        {
            var val = item.Value;
            var key = item.Key;

            if (key is null)
            {
                throw new ArgumentNullException(nameof(item), SR.ConcurrentDictionary_ItemKeyIsNull);
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // return if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        return false;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;
                    var flag  = sync | (int)RecordStatus.Removing;

                    // try to get lock
                    if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                    {
                        try
                        {
                            var keys = keyst[index];

                            // check cell 0
                            if ((sync & (int)RecordStatus.HasValue0) != 0 && comp.Equals(key, keys.Key0))
                            {
                                if (_valuesComparer.Equals(val, vals[index].Value0))
                                {
                                    keyst[index].Key0  = default;
                                    vals[index].Value0 = default;
                                    syncs[index]       = sync ^ (int)RecordStatus.HasValue0;
                                }
                                else
                                {
                                    syncs[index] = sync;

                                    return false;
                                }
                            }
                            // check cell 1
                            else if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                            {
                                if (_valuesComparer.Equals(val, vals[index].Value1))
                                {
                                    keyst[index].Key1  = default;
                                    vals[index].Value1 = default;
                                    syncs[index]       = sync ^ (int)RecordStatus.HasValue1;
                                }
                                else
                                {
                                    syncs[index] = sync;

                                    return false;
                                }
                            }
                            // check cell 2
                            else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                            {
                                if (_valuesComparer.Equals(val, vals[index].Value2))
                                {
                                    keyst[index].Key2  = default;
                                    vals[index].Value2 = default;
                                    syncs[index]       = sync ^ (int)RecordStatus.HasValue2;
                                }
                                else
                                {
                                    syncs[index] = sync;

                                    return false;
                                }
                            }
                            // check cell 3
                            else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                            {
                                if (_valuesComparer.Equals(val, vals[index].Value3))
                                {
                                    keyst[index].Key3  = default;
                                    vals[index].Value3 = default;
                                    syncs[index]       = sync ^ (int)RecordStatus.HasValue3;
                                }
                                else
                                {
                                    syncs[index] = sync;

                                    return false;
                                }
                            }
                            // key not exist
                            else
                            {
                                syncs[index] = sync;

                                return false;
                            }

                            DecrementCount(data);

                            return true;
                        }
                        catch
                        {
                            syncs[index] = sync;

                            throw;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Attempts to remove and return the the value with the specified key from the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, <paramref name="value"/> contains the object removed from the
        /// <see cref="ConcurrentDictionary{TKey,TValue}"/> or the default value of <typeparamref
        /// name="TValue"/>
        /// if the operation failed.</param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // return if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        value = default;

                        return false;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;
                    var flag  = sync | (int)RecordStatus.Removing;

                    // try to get lock
                    if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                    {
                        try
                        {
                            var keys = keyst[index];

                            // check cell 0
                            if ((sync & (int)RecordStatus.HasValue0) != 0 && comp.Equals(key, keys.Key0))
                            {
                                value = vals[index].Value0;

                                keyst[index].Key0  = default;
                                vals[index].Value0 = default;
                                syncs[index]       = sync ^ (int)RecordStatus.HasValue0;
                            }
                            // check cell 1
                            else if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                            {
                                value = vals[index].Value1;

                                keyst[index].Key1  = default;
                                vals[index].Value1 = default;
                                syncs[index]       = sync ^ (int)RecordStatus.HasValue1;
                            }
                            // check cell 2
                            else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                            {
                                value = vals[index].Value2;

                                keyst[index].Key2  = default;
                                vals[index].Value2 = default;
                                syncs[index]       = sync ^ (int)RecordStatus.HasValue2;
                            }
                            // check cell 3
                            else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                            {
                                value = vals[index].Value3;

                                keyst[index].Key3  = default;
                                vals[index].Value3 = default;
                                syncs[index]       = sync ^ (int)RecordStatus.HasValue3;
                            }
                            // key not exist
                            else
                            {
                                syncs[index] = sync;

                                value = default;

                                return false;
                            }

                            DecrementCount(data);

                            return true;
                        }
                        catch
                        {
                            syncs[index] = sync;

                            throw;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Compares the existing value for the specified key with a specified value, and if they're equal,
        /// updates the key with a third value.
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
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null
        /// reference.</exception>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // return if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        return false;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;
                    var flag  = sync | (int)RecordStatus.Updating;

                    // try to get lock
                    if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                    {
                        try
                        {
                            var keys = keyst[index];

                            // check cell 0
                            if ((sync & (int)RecordStatus.HasValue0) != 0 && comp.Equals(key, keys.Key0))
                            {
                                if (_valuesComparer.Equals(vals[index].Value0, comparisonValue))
                                {
                                    vals[index].Value0 = newValue;
                                    syncs[index] = sync;

                                    return true;
                                }
                            }
                            // check cell 1
                            else if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                            {
                                if (_valuesComparer.Equals(vals[index].Value1, comparisonValue))
                                {
                                    vals[index].Value1 = newValue;
                                    syncs[index] = sync;

                                    return true;
                                }
                            }
                            // check cell 2
                            else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                            {
                                if (_valuesComparer.Equals(vals[index].Value2, comparisonValue))
                                {
                                    vals[index].Value2 = newValue;
                                    syncs[index] = sync;

                                    return true;
                                }
                            }
                            // check cell 3
                            else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                            {
                                if (_valuesComparer.Equals(vals[index].Value3, comparisonValue))
                                {
                                    vals[index].Value3 = newValue;
                                    syncs[index] = sync;

                                    return true;
                                }
                            }

                            syncs[index] = sync;

                            return false;
                        }
                        catch
                        {
                            syncs[index] = sync;

                            throw;
                        }
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">the value to be added, if the key does not already exist</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        var flag = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // get from cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = value;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var cache = vals[index];
                        var keys  = keyst[index];

                        // get from cell 0
                        if (comp.Equals(key, keys.Key0))
                        {
                            return cache.Value0;
                        }
                        // get from cell 1
                        else if (comp.Equals(key, keys.Key1))
                        {
                            return cache.Value1;
                        }
                        // get from cell 2
                        else if (comp.Equals(key, keys.Key2))
                        {
                            return cache.Value2;
                        }
                        // get from cell 3
                        else if (comp.Equals(key, keys.Key3))
                        {
                            return cache.Value3;
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        TValue value;
                        var flag  = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // get from cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key);

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp =vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key);

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = value;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key);

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key);

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = value;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var cache = vals[index];
                        var keys  = keyst[index];

                        // get from cell 0
                        if (comp.Equals(key, keys.Key0))
                        {
                            return cache.Value0;
                        }
                        // get from cell 1
                        else if (comp.Equals(key, keys.Key1))
                        {
                            return cache.Value1;
                        }
                        // get from cell 2
                        else if (comp.Equals(key, keys.Key2))
                        {
                            return cache.Value2;
                        }
                        // get from cell 3
                        else if (comp.Equals(key, keys.Key3))
                        {
                            return cache.Value3;
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
                }
            }
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey,TValue}"/>
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The value for the key.  This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by valueFactory
        /// if the key was not in the dictionary.</returns>
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        TValue value;
                        var flag  = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // get from cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key, factoryArgument);

                                    keyst[index].Key0  = key;
                                    vals[index].Value0 = value;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key, factoryArgument);

                                    keyst[index].Key1 = key;
                                    vals[index].Value1 = value;
                                    syncs[index] = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var tmp = vals[index].Value3;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key, factoryArgument);

                                    keyst[index].Key2 = key;
                                    vals[index].Value2 = value;
                                    syncs[index] = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // get from cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var tmp = vals[index].Value0;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var tmp = vals[index].Value1;

                                        syncs[index] = sync;

                                        return tmp;
                                    }
                                    // get from cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var tmp = vals[index].Value2;

                                        syncs[index] = sync;

                                        return tmp;
                                    }

                                    value = valueFactory(key, factoryArgument);

                                    keyst[index].Key3 = key;
                                    vals[index].Value3 = value;
                                    syncs[index] = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return value;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var cache = vals[index];
                        var keys  = keyst[index];

                        // get from cell 0
                        if (comp.Equals(key, keys.Key0))
                        {
                            return cache.Value0;
                        }
                        // get from cell 1
                        else if (comp.Equals(key, keys.Key1))
                        {
                            return cache.Value1;
                        }
                        // get from cell 2
                        else if (comp.Equals(key, keys.Key2))
                        {
                            return cache.Value2;
                        }
                        // get from cell 3
                        else if (comp.Equals(key, keys.Key3))
                        {
                            return cache.Value3;
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
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
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (addValueFactory == null)
            {
                throw new ArgumentNullException(nameof(addValueFactory));
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        TValue addValue;

                        var flag = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // update cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1, factoryArgument);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2, factoryArgument);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3, factoryArgument);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key, factoryArgument);

                                    keyst[index].Key0  = key;
                                    vals[index].Value0 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0, factoryArgument);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2, factoryArgument);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3, factoryArgument);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key, factoryArgument);

                                    keyst[index].Key1 = key;
                                    vals[index].Value1 = addValue;
                                    syncs[index] = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0, factoryArgument);

                                        vals[index].Value0 = value;
                                        syncs[index]       = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1, factoryArgument);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3, factoryArgument);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key, factoryArgument);

                                    keyst[index].Key2  = key;
                                    vals[index].Value2 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0, factoryArgument);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1, factoryArgument);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2, factoryArgument);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key, factoryArgument);

                                    keyst[index].Key3  = key;
                                    vals[index].Value3 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var keys = keyst[index];

                        // check exist
                        if (comp.Equals(key, keys.Key0))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 0
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value0, factoryArgument);

                                    vals[index].Value0 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key1))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 1
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value1, factoryArgument);

                                    vals[index].Value1 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key2))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 2
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value2, factoryArgument);

                                    vals[index].Value2 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key3))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 3
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value3, factoryArgument);

                                    vals[index].Value3 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
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
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="addValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (addValueFactory == null)
            {
                throw new ArgumentNullException(nameof(addValueFactory));
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        var flag = sync | (int)RecordStatus.Adding;
                        TValue addValue;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // update cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key);

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key);

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key);

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    addValue = addValueFactory(key);

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var keys = keyst[index];

                        // check exist
                        if (comp.Equals(key, keys.Key0))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 0
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value0);

                                    vals[index].Value0 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key1))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 1
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value1);

                                    vals[index].Value1 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key2))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 2
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value2);

                                    vals[index].Value2 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key3))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 3
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value3);

                                    vals[index].Value3 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
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
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="updateValueFactory"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (updateValueFactory == null)
            {
                throw new ArgumentNullException(nameof(updateValueFactory));
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        var flag = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // update cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value3);

                                        vals[index].Value3 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value0);

                                        vals[index].Value0 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value1);

                                        vals[index].Value1 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }
                                    // update cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        var value = updateValueFactory(key, vals[index].Value2);

                                        vals[index].Value2 = value;
                                        syncs[index] = sync;

                                        return value;
                                    }

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var keys = keyst[index];

                        // check exist
                        if (comp.Equals(key, keys.Key0))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 0
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value0);

                                    vals[index].Value0 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key1))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 1
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value1);

                                    vals[index].Value1 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key2))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 2
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value2);

                                    vals[index].Value2 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key3))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 3
                                try
                                {
                                    var value = updateValueFactory(key, vals[index].Value3);

                                    vals[index].Value3 = value;

                                    return value;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
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
        /// <param name="updateValue">The new value for an existing key</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <returns>The new value for the key.  This will be either be the result of addValueFactory (if the key was
        /// absent) or the result of updateValueFactory (if the key was present).</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            var data  = _data;
            var frame = data.Frame;
            var comp  = _keysComparer;
            var hash  = comp.GetHashCode(key) & 0x7fffffff;

            unchecked
            {
                // search empty space
                while (true)
                {
                    var syncs  = frame.SyncTable;
                    var index  = hash % frame.HashMaster;
                    var sync   = syncs[index];

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full)
                    {
                        frame = Volatile.Read(ref data.Frame);

                        continue;
                    }

                    var keyst = frame.KeysTable;
                    var vals  = frame.ValuesTable;

                    // Check that there is at least one free space
                    if ((sync & (int)RecordStatus.Full) != (int)RecordStatus.Full)
                    {
                        var flag = sync | (int)RecordStatus.Adding;

                        // try to get lock
                        if (sync == Interlocked.CompareExchange(ref syncs[index], flag, sync))
                        {
                            try
                            {
                                var keys = keyst[index];

                                // add to cell 0
                                if ((sync & (int)RecordStatus.HasValue0) == 0)
                                {
                                    // update cell 1
                                    if ((sync & (int)RecordStatus.HasValue1) != 0 && comp.Equals(key, keys.Key1))
                                    {
                                        vals[index].Value1 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        vals[index].Value2 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        vals[index].Value3 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }

                                    keyst[index].Key0   = key;
                                    vals[index].Value0  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue0;
                                }
                                // add to cell 1
                                else if ((sync & (int)RecordStatus.HasValue1) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        vals[index].Value0 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 2
                                    else if ((sync & (int)RecordStatus.HasValue2) != 0 && comp.Equals(key, keys.Key2))
                                    {
                                        vals[index].Value2 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        vals[index].Value3 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }

                                    keyst[index].Key1  = key;
                                    vals[index].Value1 = addValue;
                                    syncs[index]       = sync | (int)RecordStatus.HasValue1;
                                }
                                // add to cell 2
                                else if ((sync & (int)RecordStatus.HasValue2) == 0)
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        vals[index].Value0 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        vals[index].Value1 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 3
                                    else if ((sync & (int)RecordStatus.HasValue3) != 0 && comp.Equals(key, keys.Key3))
                                    {
                                        vals[index].Value3 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }

                                    keyst[index].Key2   = key;
                                    vals[index].Value2  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue2;
                                }
                                // add to cell 3
                                else
                                {
                                    // update cell 0
                                    if (comp.Equals(key, keys.Key0))
                                    {
                                        vals[index].Value0 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 1
                                    else if (comp.Equals(key, keys.Key1))
                                    {
                                        vals[index].Value1 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }
                                    // update cell 2
                                    else if (comp.Equals(key, keys.Key2))
                                    {
                                        vals[index].Value2 = updateValue;
                                        syncs[index] = sync;

                                        return updateValue;
                                    }

                                    keyst[index].Key3   = key;
                                    vals[index].Value3  = addValue;
                                    syncs[index]        = sync | (int)RecordStatus.HasValue3;
                                }

                                IncrementCount(data);

                                return addValue;
                            }
                            catch
                            {
                                syncs[index] = sync;

                                throw;
                            }
                        }
                    }
                    // growing
                    else
                    {
                        var keys = keyst[index];

                        // check exist
                        if (comp.Equals(key, keys.Key0))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 0
                                try
                                {
                                    vals[index].Value0 = updateValue;

                                    return updateValue;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key1))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 1
                                try
                                {
                                    vals[index].Value1 = updateValue;

                                    return updateValue;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key2))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 2
                                try
                                {
                                    vals[index].Value2 = updateValue;

                                    return updateValue;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }
                        else if (comp.Equals(key, keys.Key3))
                        {
                            if (sync == Interlocked.CompareExchange(ref syncs[index], sync | (int)RecordStatus.Updating, sync))
                            {
                                // update cell 3
                                try
                                {
                                    vals[index].Value3 = updateValue;

                                    return updateValue;
                                }
                                finally
                                {
                                    syncs[index] = sync;
                                }
                            }
                            else
                            {
                                frame = Volatile.Read(ref data.Frame);

                                continue;
                            }
                        }

                        GrowTable(data, hash);
                    }

                    frame = Volatile.Read(ref data.Frame);
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
            return new Enumerator(this);
        }

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ConcurrentDictionary{TKey,TValue}"/> to a
        /// new array.
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            var index = 0;
            var arr = new KeyValuePair<TKey, TValue>[Count];

            // reads all values
            foreach (var current in this)
            {
                // grow array
                if (index == arr.Length)
                {
                    var len = Count;

                    if (len < arr.Length)
                    {
                        len = arr.Length + 10_000;
                    }
                    else
                    {
                        len += 10_000;
                    }

                    var tmp = new KeyValuePair<TKey, TValue>[len];

                    Array.Copy(arr, tmp, arr.Length);

                    arr = tmp;
                }

                arr[index++] = current;
            }

            // trim array
            if (index != arr.Length)
            {
                var tmp = new KeyValuePair<TKey, TValue>[index];

                Array.Copy(arr, tmp, index);

                arr = tmp;
            }

            return arr;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ConcurrentDictionary{TKey,TValue}"/>.
        /// </summary>
        public void Clear()
        {
            var tmp = new HashTableData(_capacity, COUNTS_SIZE);

            // counts initialization
            for (var i = 0; i < tmp.Counts.Length; ++i)
            {
                tmp.Counts[i] = new ConcurrentDictionaryCounter();
            }

            // write empty data
            _data = tmp;
        }

        #endregion

        #region ' Private Area '

        // Expand table function
        private void GrowTable(HashTableData data, int hash)
        {
            var len  = data.Frame.HashMaster;

            // get growing lock
            while (Interlocked.CompareExchange(ref data.SyncGrowing, 1, 0) != 0)
            {
                while (Volatile.Read(ref data.SyncGrowing) != 0)
                {
                    Thread.Yield();
                }

                if (len != Volatile.Read(ref data.Frame).HashMaster)
                {
                    return;
                }
            }

            unchecked
            {
                int sync;
                // This assignment is necessary to calm the compiler.
                // In fact, this variable is initialized anyway.
                // HashTableData new_data;
                var new_data   = new HashTableDataFrame(0);
                var cur_data   = data.Frame;
                var repeat     = true;
                var mul        = GROW_MULTIPLIER;
                var new_master = cur_data.HashMaster;

                while (repeat)
                {
                    if (new_master == int.MaxValue)
                    {
                        data.SyncGrowing = 0;

                        throw new OverflowException();
                    }

                    new_master = cur_data.HashMaster * mul + 1;

                    if (new_master < 0 || new_master <= cur_data.HashMaster)
                    {
                        new_master = int.MaxValue;
                    }

                    repeat = false;
                    new_data = new HashTableDataFrame(new_master);

                    for (var i = 0; i < cur_data.HashMaster; ++i)
                    {
                        Thread.MemoryBarrier();

                        sync = cur_data.SyncTable[i];

                        var flag = sync | (int)RecordStatus.Growing;

                        // set a bucket lock
                        if (
                               (sync & (int)RecordStatus.Growing) != 0
                                                ||
                                sync <= (int)RecordStatus.Full
                                                &&
                                sync == Interlocked.CompareExchange(ref cur_data.SyncTable[i], flag, sync)
                           )
                        {
                            if ((sync & (int)RecordStatus.HasValue0) != 0)
                            {
                                var key = cur_data.KeysTable[i].Key0;
                                var val = cur_data.ValuesTable[i].Value0;

                                if (!GrowingAdd(new_data, key, val))
                                {
                                    repeat = true;

                                    break;
                                }
                            }

                            if ((sync & (int)RecordStatus.HasValue1) != 0)
                            {
                                var key = cur_data.KeysTable[i].Key1;
                                var val = cur_data.ValuesTable[i].Value1;

                                if (!GrowingAdd(new_data, key, val))
                                {
                                    repeat = true;

                                    break;
                                }
                            }

                            if ((sync & (int)RecordStatus.HasValue2) != 0)
                            {
                                var key = cur_data.KeysTable[i].Key2;
                                var val = cur_data.ValuesTable[i].Value2;

                                if (!GrowingAdd(new_data, key, val))
                                {
                                    repeat = true;

                                    break;
                                }
                            }

                            if ((sync & (int)RecordStatus.HasValue3) != 0)
                            {
                                var key = cur_data.KeysTable[i].Key3;
                                var val = cur_data.ValuesTable[i].Value3;

                                if (!GrowingAdd(new_data, key, val))
                                {
                                    repeat = true;

                                    break;
                                }
                            }
                        }
                        // wait another thread complate oparation
                        else
                        {
                            i--;

                            Thread.Yield();
                        }
                    }

                    if (!repeat)
                    {
                        // check new value
                        var new_index = hash % new_master;

                        sync = new_data.SyncTable[new_index];

                        // check at least one empty space
                        if ((sync & (int)RecordStatus.Full) == (int)RecordStatus.Full)
                        {
                            repeat = true;
                        }
                    }

                    mul += GROW_MULTIPLIER_STEP;
                }

                // write new data frame
                data.Frame = new_data;

                // unlock growing
                data.SyncGrowing = 0;
            }
        }

        // Add on growing
        private bool GrowingAdd(HashTableDataFrame data, TKey key, TValue val)
        {
            var comp  = _keysComparer;
            int index = (comp.GetHashCode(key) & 0x7fffffff) % data.HashMaster;
            int sync  = data.SyncTable[index];

            if ((sync & (int)RecordStatus.HasValue0) == 0)
            {
                data.KeysTable[index].Key0     = key;
                data.ValuesTable[index].Value0 = val;
                data.SyncTable[index]          = sync | (int)RecordStatus.HasValue0;

                return true;
            }
            else if ((sync & (int)RecordStatus.HasValue1) == 0)
            {
                data.KeysTable[index].Key1     = key;
                data.ValuesTable[index].Value1 = val;
                data.SyncTable[index]          = sync | (int)RecordStatus.HasValue1;

                return true;
            }
            else if ((sync & (int)RecordStatus.HasValue2) == 0)
            {
                data.KeysTable[index].Key2     = key;
                data.ValuesTable[index].Value2 = val;
                data.SyncTable[index]          = sync | (int)RecordStatus.HasValue2;

                return true;
            }
            else if ((sync & (int)RecordStatus.HasValue3) == 0)
            {
                data.KeysTable[index].Key3     = key;
                data.ValuesTable[index].Value3 = val;
                data.SyncTable[index]          = sync | (int)RecordStatus.HasValue3;

                return true;
            }

            return false;
        }

        // Constructor initialization
        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key == null)
                {
                    ThrowKeyNullException();
                }

                if (!TryAdd(pair.Key, pair.Value))
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_SourceContainsDuplicateKeys);
                }
            }
        }

        // Increaze Count
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementCount(HashTableData data)
        {
            // A situation may arise when some threads only add, while others only delete.
            // Then a reasonable question may arise whether this will lead to overflow of
            // a variable of type long. To answer this question, it is necessary to make
            // calculations. As you know, long is 2 ^ 63. According to tests, the rate of
            // addition is about 30 million operations per second. Let's calculate how many
            // years it will take to overflow this variable.
            // years = 9223372036854775807 / (30_000_000 * 60 * 60 * 24 * 365) = 9740.
            // Almost 10 thousand years. In addition, in the application that uses the
            // hash table, there is another business logic that reduces the number of
            // operations per second. In general, I am sure that overflow can be considered
            // impossible. If in the future the core performance will increase significantly,
            // then it will be necessary to switch to using 128-bit variables.

            var id     = t_id;
            var counts = data.Counts;

            if (id == 0 || id >= counts.Length)
            {
                id = t_id = Thread.CurrentThread.ManagedThreadId;

                if (id >= counts.Length)
                {
                    counts = CountsResize(data);
                }
            }

            counts[id].Count++;
        }

        // Decrement Count
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementCount(HashTableData data)
        {
            var id     = t_id;
            var counts = data.Counts;

            if (id == 0 || id >= counts.Length)
            {
                id = t_id = Thread.CurrentThread.ManagedThreadId;

                if (id >= counts.Length)
                {
                    counts = CountsResize(data);
                }
            }

            counts[id].Count--;
        }

        // Function to increase the size of the array counts
        private ConcurrentDictionaryCounter[] CountsResize(HashTableData data)
        {
            var id = t_id;

            while (Interlocked.CompareExchange(ref data.SyncCounts, 1, 0) != 0)
            {
                while (Volatile.Read(ref data.SyncCounts) != 0)
                {
                    Thread.Yield();
                }

                var counts = Volatile.Read(ref data.Counts);

                if (id > counts.Length)
                {
                    return counts;
                }
            }

            var len = data.Counts.Length;

            if (id > len)
            {
                len = id;
            }

            var tmp_counts = new ConcurrentDictionaryCounter[len * 2];

            Array.Copy(data.Counts, tmp_counts, data.Counts.Length);

            // fill with empty counts
            for (var i = data.Counts.Length; i < tmp_counts.Length; ++i)
            {
                tmp_counts[i] = new ConcurrentDictionaryCounter();
            }

            // write new counts link
            data.Counts = tmp_counts;

            // unlock counts
            data.SyncCounts = 0;

            return tmp_counts;
        }

        #endregion

        #region ' IEnumerable Members '

        /// <summary>Returns an enumerator that iterates through the <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>.</summary>
        /// <returns>An enumerator for the <see cref="ConcurrentDictionary{TKey,TValue}"/>.</returns>
        /// <remarks>
        /// The enumerator returned from the dictionary is safe to use concurrently with
        /// reads and writes to the dictionary, however it does not represent a moment-in-time snapshot
        /// of the dictionary.  The contents exposed through the enumerator may contain modifications
        /// made to the dictionary after <see cref="GetEnumerator"/> was called.
        /// </remarks>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
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
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ' IDictionary Members '

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The object to use as the key.</param>
        /// <param name="value">The object to use as the value.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="key"/> is of a type that is not assignable to the key type <typeparamref
        /// name="TKey"/> of the <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>. -or-
        /// <paramref name="value"/> is of a type that is not assignable to <typeparamref name="TValue"/>,
        /// the type of values in the <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// -or- A value with the same key already exists in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </exception>
        void IDictionary.Add(object key, object? value)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (!(key is TKey))
            {
                throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
            }

            ThrowIfInvalidObjectValue(value);

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, (TValue)value!);
        }

        /// <summary>
        /// Gets whether the <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> contains an
        /// element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the <see cref="System.Collections.Generic.IDictionary{TKey,TValue}"/> contains
        /// an element with the specified key; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException"> <paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary.Contains(object key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            return (key is TKey _key) && ContainsKey(_key);
        }

        /// <summary>Provides an <see cref="System.Collections.IDictionaryEnumerator"/> for the
        /// <see cref="System.Collections.IDictionary"/>.</summary>
        /// <returns>An <see cref="System.Collections.IDictionaryEnumerator"/> for the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.</returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="System.Collections.IDictionary"/> has a fixed size.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.IDictionary"/> has a
        /// fixed size; otherwise, false. For <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see
        /// cref="System.Collections.IDictionary"/> is read-only.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.IDictionary"/> is
        /// read-only; otherwise, false. For <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        void IDictionary.Remove(object key)
        {
            if (key == null)
            {
                ThrowKeyNullException();
            }

            if (key is TKey _key)
            {
                TryRemove(_key, out TValue throwAwayValue);
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.ICollection{TKey}"/> containing the keys in the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TKey> Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.ICollection{TValue}"/> containing the values in
        /// the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</value>
        public ICollection<TValue> Values
        {
            get
            {
                return GetValues();
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <value>The value associated with the specified key, or a null reference (Nothing in Visual Basic)
        /// if <paramref name="key"/> is not in the dictionary or <paramref name="key"/> is of a type that is
        /// not assignable to the key type <typeparamref name="TKey"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.</value>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentException">
        /// A value is being assigned, and <paramref name="key"/> is of a type that is not assignable to the
        /// key type <typeparamref name="TKey"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>. -or- A value is being
        /// assigned, and <paramref name="key"/> is of a type that is not assignable to the value type
        /// <typeparamref name="TValue"/> of the <see
        /// cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>
        /// </exception>
        object? IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                {
                    ThrowKeyNullException();
                }

                if (key is TKey _key && TryGetValue(_key, out TValue value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (key == null)
                {
                    ThrowKeyNullException();
                }

                if (!(key is TKey))
                {
                    throw new ArgumentException(SR.ConcurrentDictionary_TypeOfKeyIncorrect);
                }

                ThrowIfInvalidObjectValue(value);

                this[(TKey)key] = (TValue)value;
            }
        }

        #endregion

        #region ' IDictionary<TKey,TValue> members '

        /// <summary>
        /// Adds the specified key and value to the <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.OverflowException">The dictionary contains too many
        /// elements.</exception>
        /// <exception cref="System.ArgumentException">
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
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the element is successfully remove; otherwise false. This method also returns
        /// false if
        /// <paramref name="key"/> was not found in the original <see
        /// cref="System.Collections.Generic.IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return TryRemove(key, out TValue throwAwayValue);
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.IEnumerable{TKey}"/> containing the keys of
        /// the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.Generic.IEnumerable{TValue}"/> containing the values
        /// in the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.Generic.IEnumerable{TValue}"/> containing the
        /// values in the <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>.</value>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return GetValues();
            }
        }

        #endregion

        #region ' ICollection Members '

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.ICollection"/> to an array, starting
        /// at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from
        /// the <see cref="System.Collections.ICollection"/>. The array must have zero-based
        /// indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ConcurrentDictionary_IndexIsNegative);
            }

            var offset = 0;
            var len = array.Length - index;

            // +++++++++++++++++++++++++++++++++
            //
            // The following condition has been set to pass some tests. However,
            // it is not entirely successful. Let's look at a two-thread scenario.
            //
            // var dict = new ConcurrentDictionary<int, int>();
            //
            // thread #1: dict.TryAdd(1, -1);
            // thread #1: var count = dict.Count;
            // thread #1: var array = new int[count];
            // thread #2: dict.TryAdd(2, -2);
            // thread #1: dict.CopyTo(array, 0);
            //
            // Such a scenario is guaranteed to lead to an exception that makes no sense.
            // I recommend commented-out condition instead.

            var count = Count;

            if (array.Length - count < index)
            {
                throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
            }

            //if (index >= array.Length)
            //{
            //    throw new ArgumentException(SR.ConcurrentDictionary_ArrayNotLargeEnough);
            //}

            // +++++++++++++++++++++++++++++++++


            // To be consistent with the behavior of ICollection.CopyTo() in Dictionary<TKey,TValue>,
            // we recognize three types of target arrays:
            //    - an array of KeyValuePair<TKey, TValue> structs
            //    - an array of DictionaryEntry structs
            //    - an array of objects

            if (array is KeyValuePair<TKey, TValue>[] pairs)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    pairs[index + offset++] = current;
                }

                return;
            }

            if (array is DictionaryEntry[] entries)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    entries[index + offset++] = new DictionaryEntry { Key = current.Key, Value = current.Value };
                }

                return;
            }

            if (array is object[] objects)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    objects[index + offset++] = current;
                }

                return;
            }

            if (array is TKey[] keys)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    keys[index + offset++] = current.Key;
                }

                return;
            }

            if (array is TValue[] values)
            {
                foreach (var current in this)
                {
                    if (offset == len)
                    {
                        break;
                    }

                    values[index + offset++] = current.Value;
                }

                return;
            }

            throw new ArgumentException(SR.ConcurrentDictionary_ArrayIncorrectType, nameof(array));
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="System.Collections.ICollection"/> is
        /// synchronized with the SyncRoot.
        /// </summary>
        /// <value>true if access to the <see cref="System.Collections.ICollection"/> is synchronized
        /// (thread safe); otherwise, false. For <see
        /// cref="ConcurrentDictionary{TKey,TValue}"/>, this property always
        /// returns false.</value>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see
        /// cref="System.Collections.ICollection"/>. This property is not supported.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The SyncRoot property is not supported.</exception>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(SR.ConcurrentCollection_SyncRoot_NotSupported);
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.ICollection"/> containing the keys of the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.ICollection"/> containing the keys of the <see
        /// cref="System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        /// <summary>
        /// Gets an <see cref="System.Collections.ICollection"/> containing the values in the <see
        /// cref="System.Collections.IDictionary"/>.
        /// </summary>
        /// <value>An <see cref="System.Collections.ICollection"/> containing the values in the <see
        /// cref="System.Collections.IDictionary"/>.</value>
        ICollection IDictionary.Values
        {
            get
            {
                return GetValues();
            }
        }

        #endregion

        #region ' ICollection<KeyValuePair<TKey,TValue>> Members '

        /// <summary>
        /// Adds the specified value to the <see cref="System.Collections.Generic.ICollection{TValue}"/>
        /// with the specified key.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to add to the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="keyValuePair"/> of <paramref
        /// name="keyValuePair"/> is null.</exception>
        /// <exception cref="System.OverflowException">The <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>
        /// contains too many elements.</exception>
        /// <exception cref="System.ArgumentException">An element with the same key already exists in the
        /// <see cref="System.Collections.Generic.Dictionary{TKey,TValue}"/></exception>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
        }

        /// <summary>
        /// Determines whether the <see cref="System.Collections.Generic.ICollection{T}"/>
        /// contains a specific key and value.
        /// </summary>
        /// <param name="keyValuePair">The <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure to locate in the <see
        /// cref="System.Collections.Generic.ICollection{TValue}"/>.</param>
        /// <returns>true if the <paramref name="keyValuePair"/> is found in the <see
        /// cref="System.Collections.Generic.ICollection{T}"/>; otherwise, false.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (TryGetValue(keyValuePair.Key, out TValue value))
            {
                return _valuesComparer.Equals(value, keyValuePair.Value);
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <value>true if the <see cref="System.Collections.Generic.ICollection{T}"/> is
        /// read-only; otherwise, false. For <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>, this property always returns
        /// false.</value>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a key and value from the dictionary.
        /// </summary>
        /// <param name="keyValuePair">The <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// structure representing the key and value to remove from the <see
        /// cref="System.Collections.Generic.Dictionary{TKey,TValue}"/>.</param>
        /// <returns>true if the key and value represented by <paramref name="keyValuePair"/> is successfully
        /// found and removed; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">The Key property of <paramref
        /// name="keyValuePair"/> is a null reference (Nothing in Visual Basic).</exception>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            return TryRemove(keyValuePair);
        }

        /// <summary>
        /// Copies the elements of the <see cref="System.Collections.Generic.ICollection{T}"/> to an array of
        /// type <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>, starting at the
        /// specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array of type <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/>
        /// that is the destination of the <see
        /// cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/> elements copied from the <see
        /// cref="System.Collections.ICollection"/>. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying
        /// begins.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is a null reference
        /// (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is less than
        /// 0.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="index"/> is equal to or greater than
        /// the length of the <paramref name="array"/>. -or- The number of elements in the source <see
        /// cref="System.Collections.ICollection"/>
        /// is greater than the available space from <paramref name="index"/> to the end of the destination
        /// <paramref name="array"/>.</exception>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        #endregion

        #region ' Structures '

        // Reflects the current status of each basket.
        // Warning: When changing, carefully look at the use in more><less conditions.
        private enum RecordStatus
        {
            Empty           = 000,
            HasValue0       = 001,
            HasValue1       = 002,
            HasValue2       = 004,
            HasValue3       = 008,
            Full            = 015,
            Adding          = 016,
            Removing        = 032,
            Updating        = 064,
            Growing         = 128
        }

        // Bucket of keys for the table
        [DebuggerDisplay("Key0 = {Key0}, Key1 = {Key1}, Key2 = {Key2}, Key3 = {Key3}")]
        private struct KeyBucket
        {
            public TKey Key0;
            public TKey Key1;
            public TKey Key2;
            public TKey Key3;
        }

        // Buckets of values for the table
        [DebuggerDisplay("Value0 = {Value0}, Value1 = {Value1}, Value2 = {Value2}, Value3 = {Value3}")]
        private struct ValueBucket
        {
            public TValue Value0;
            public TValue Value1;
            public TValue Value2;
            public TValue Value3;
        }

        /// <summary>
        /// In a hashtable, there is a need to replace multiple references and some
        /// data atomically.Since in .Net the link changes atomically, this wrapper
        /// class exists for these purposes.
        /// </summary>
        private class HashTableData
        {
            public HashTableData(int hashMaster, int countsSize)
            {
                Frame  = new HashTableDataFrame(hashMaster);
                Counts = new ConcurrentDictionaryCounter[countsSize];
            }

            public HashTableData(int hashMaster): this(hashMaster, 0)
            {
            }

            // To synchronize threads when growing table
            public int SyncGrowing;

            // To synchronize threads when expanding the counters array
            public int SyncCounts;

            // Data frame
            public HashTableDataFrame Frame;

            // Array of counters
            public ConcurrentDictionaryCounter[] Counts;
        }

        /// <summary>
        /// A data frame is always created when a table grows.
        /// </summary>
        private class HashTableDataFrame
        {
            public HashTableDataFrame(int hashMaster)
            {
                HashMaster  = hashMaster;
                SyncTable   = new int[hashMaster];
                KeysTable   = new KeyBucket[hashMaster];
                ValuesTable = new ValueBucket[hashMaster];
            }

            // Divider for hash function
            public readonly int HashMaster;

            // State array. Used to synchronize threads. See RecordStatus
            // Other threads make changes only when deleting or updating.
            public readonly int[] SyncTable;

            // Array of keys
            // Other threads make changes only when deleting.
            public readonly KeyBucket[] KeysTable;

            // Array of values
            // Other threads make changes only when deleting or updating.
            public readonly ValueBucket[] ValuesTable;
        }

        /// <summary>
        /// A private class that implements the ability to enumerate.
        /// </summary>
        private class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            // owner dictionary
            private readonly ConcurrentDictionary<TKey, TValue> _dictionary;
            // bucket index
            private int _index;
            // current cell in bucket
            private int _cell;

            // constructor
            public Enumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;

                Reset();
            }

            // get next value
            public bool MoveNext()
            {
                while (true)
                {
                    var data = Volatile.Read(ref _dictionary._data).Frame;

                    if (_index >= data.HashMaster)
                    {
                        return false;
                    }

                    var vals = data.ValuesTable[_index];
                    var keys = data.KeysTable[_index];
                    var sync = data.SyncTable[_index];

                    // skip if empty
                    if (sync == (int)RecordStatus.Empty)
                    {
                        _index++;

                        continue;
                    }

                    // wait if another thread doing something
                    if (sync > (int)RecordStatus.Full && sync < (int)RecordStatus.Growing)
                    {
                        continue;
                    }

                    // check cell 0
                    if (_cell < 1 && (sync & (int)RecordStatus.HasValue0) != 0)
                    {
                        _cell = 1;
                        Current = new KeyValuePair<TKey, TValue>(keys.Key0, vals.Value0);

                        return true;
                    }

                    // check cell 1
                    if (_cell < 2 && (sync & (int)RecordStatus.HasValue1) != 0)
                    {
                        _cell = 2;
                        Current = new KeyValuePair<TKey, TValue>(keys.Key1, vals.Value1);

                        return true;
                    }

                    // check cell 2
                    if (_cell < 3 && (sync & (int)RecordStatus.HasValue2) != 0)
                    {
                        _cell = 3;
                        Current = new KeyValuePair<TKey, TValue>(keys.Key2, vals.Value2);

                        return true;
                    }

                    _cell = 0;
                    _index++;

                    // check cell 3
                    if ((sync & (int)RecordStatus.HasValue3) != 0)
                    {
                        Current = new KeyValuePair<TKey, TValue>(keys.Key3, vals.Value3);

                        return true;
                    }
                }
            }

            // reset for reuse
            public void Reset()
            {
                _cell    = 0;
                _index   = 0;
                Current = default;
            }

            // current value
            public KeyValuePair<TKey, TValue> Current { get; private set; }

            // current value
            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            // free up resources
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// A private class to represent enumeration over the dictionary that implements the
        /// IDictionaryEnumerator interface.
        /// </summary>
        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            // Enumerator over the dictionary.
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                _enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value);
                }
            }

            public object Key
            {
                get
                {
                    return _enumerator.Current.Key;
                }
            }

            public object? Value
            {
                get
                {
                    return _enumerator.Current.Value;
                }
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
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

        #endregion

        #region ' Helpers '

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TKey> GetKeys()
        {
            var keys = new List<TKey>(Count);

            foreach (var current in this)
            {
                keys.Add(current.Key);
            }

            return new ReadOnlyCollection<TKey>(keys);
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        private ReadOnlyCollection<TValue> GetValues()
        {
            var vals = new List<TValue>(Count);

            foreach (var current in this)
            {
                vals.Add(current.Value);
            }

            return new ReadOnlyCollection<TValue>(vals);
        }

        // These exception throwing sites have been extracted into their own NoInlining methods
        // as these are uncommonly needed and when inlined are observed to prevent the inlining
        // of important methods like TryGetValue and ContainsKey.
        [DoesNotReturn]
        private static void ThrowKeyNotFoundException(object key)
        {
            throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
        }

        //
        [DoesNotReturn]
        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }

        //
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
            else if (default(TValue) != null)
            {
                ThrowValueNullException();
            }
        }

        //
        private static void ThrowValueNullException()
        {
            throw new ArgumentException(SR.ConcurrentDictionary_TypeOfValueIncorrect);
        }

        #endregion
    }

    [DebuggerDisplay("Count = {Count}")]
    [StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE)]
    internal class ConcurrentDictionaryCounter
    {
        [FieldOffset(0)]
        public long Count;
    }

    /// <summary>
    ///     A size greater than or equal to the size of the most common CPU cache lines.
    /// </summary>
    internal static class PaddingHelpers
    {
#if TARGET_ARM64
        internal const int CACHE_LINE_SIZE = 128;
#elif TARGET_32BIT
        internal const int CACHE_LINE_SIZE = 32;
#else
        internal const int CACHE_LINE_SIZE = 64;
#endif
    }

    internal sealed class IDictionaryDebugView<K, V> where K : notnull
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
