// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Generic
{
    // Implementation notes:
    // ---------------------
    // Ideally, all of the following would be O(1):
    // - Lookup by key
    // - Indexing by position
    // - Adding
    // - Inserting
    // - Removing
    //
    // There's not a good way to achieve all of those, e.g.
    // - A map for lookups with an array list achieves O(1) lookups, indexing, and adding, but O(N) insert and removal.
    // - A map for lookups with a linked list achieves O(1) lookups, adding, removal, and insert, but O(N) indexing.
    //
    // There are also layout and memory consumption tradeoffs. For example, a map to nodes containing keys and values
    // means lots of indirections as part of enumerating. Alternatively, the keys and values can be duplicated in both
    // a map and a list, leading to larger memory consumption, but optimizing for speed of data access. Or the keys
    // and values can be stored in the map with only the key stored in the list.
    //
    // This implementation currently employs the simple strategy of using both a dictionary and a list, with the
    // dictionary as the source of truth for the key/value pairs, and the list storing just the keys in order. This
    // provides O(1) lookups, adding, and indexing, with O(N) insert and removal. Keys are duplicated in memory,
    // but lookups are optimized to be simple dictionary accesses. Enumeration is O(N), and involves enumerating
    // the list for order and performing a lookup on each element to get its value. This is the same approach taken
    // by the non-generic OrderedDictionary and thus keeps algorithmic complexity consistent for someone upgrading
    // from the non-generic to generic types. It's also important for consumption via the interfaces, in particular
    // I{ReadOnly}List<T>, where it's common to iterate through a list with an indexer, and if indexing were O(N)
    // instead of O(1), it would turn such loops into O(N^2) instead of O(N).
    //
    // Currently the implementation is optimized for simplicity and correctness, choosing to wrap a Dictionary<>
    // and a List<> rather than implementing a custom data structure. They could be flattened to partially
    // deduped in the future if the extra overhead is deemed prohibitive.

    /// <summary>
    /// Represents a collection of key/value pairs that are accessible by the key or index.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    [DebuggerTypeProxy(typeof(IDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public class OrderedDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary,
        IList<KeyValuePair<TKey, TValue>>, IReadOnlyList<KeyValuePair<TKey, TValue>>, IList
        where TKey : notnull
    {
        /// <summary>Store for the key/value pairs in the dictionary.</summary>
        private readonly Dictionary<TKey, TValue> _dictionary;
        /// <summary>List storing the keys in order.</summary>
        private readonly List<TKey> _list;

        /// <summary>Lazily-initialized wrapper collection that serves up only the keys, in order.</summary>
        private KeyCollection? _keys;
        /// <summary>Lazily-initialized wrapper collection that serves up only the values, in order.</summary>
        private ValueCollection? _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that is empty,
        /// has the default initial capacity, and uses the default equality comparer for the key type.
        /// </summary>
        public OrderedDictionary()
        {
            _dictionary = [];
            _list = [];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that is empty,
        /// has the specified initial capacity, and uses the default equality comparer for the key type.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="OrderedDictionary{TKey, TValue}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
        public OrderedDictionary(int capacity)
        {
            _dictionary = new(capacity);
            _list = new(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that is empty,
        /// has the default initial capacity, and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys,
        /// or null to use the default <see cref="EqualityComparer{TKey}"/> for the type of the key.
        /// </param>
        public OrderedDictionary(IEqualityComparer<TKey>? comparer)
        {
            _dictionary = new(comparer);
            _list = [];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that is empty,
        /// has the specified initial capacity, and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="OrderedDictionary{TKey, TValue}"/> can contain.</param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys,
        /// or null to use the default <see cref="EqualityComparer{TKey}"/> for the type of the key.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">capacity is less than 0.</exception>
        public OrderedDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        {
            _dictionary = new(capacity, comparer);
            _list = new(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that contains elements copied from
        /// the specified <see cref="IDictionary{TKey, TValue}"/> and uses the default equality comparer for the key type.
        /// </summary>
        /// <param name="dictionary">
        /// The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="OrderedDictionary{TKey, TValue}"/>.
        /// The initial order of the elements in the new collection is the order the elements are enumerated from the supplied dictionary.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public OrderedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            int capacity = dictionary.Count;

            _dictionary = new(capacity);
            _list = new(capacity);

            AddRange(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that contains elements copied from
        /// the specified <see cref="IDictionary{TKey, TValue}"/> and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="dictionary">
        /// The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="OrderedDictionary{TKey, TValue}"/>.
        /// The initial order of the elements in the new collection is the order the elements are enumerated from the supplied dictionary.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys,
        /// or null to use the default <see cref="EqualityComparer{TKey}"/> for the type of the key.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public OrderedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            int capacity = dictionary.Count;
            _dictionary = new(capacity, comparer);
            _list = new(capacity);

            AddRange(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that contains elements copied
        /// from the specified <see cref="IEnumerable{T}"/> and uses the default equality comparer for the key type.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="OrderedDictionary{TKey, TValue}"/>.
        /// The initial order of the elements in the new collection is the order the elements are enumerated from the supplied collection.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            int capacity = collection is ICollection<KeyValuePair<TKey, TValue>> c ? c.Count : 0;
            _dictionary = new(capacity);
            _list = new(capacity);

            AddRange(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDictionary{TKey, TValue}"/> class that contains elements copied
        /// from the specified <see cref="IEnumerable{T}"/> and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="IEnumerable{T}"/> whose elements are copied to the new <see cref="OrderedDictionary{TKey, TValue}"/>.
        /// The initial order of the elements in the new collection is the order the elements are enumerated from the supplied collection.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys,
        /// or null to use the default <see cref="EqualityComparer{TKey}"/> for the type of the key.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer)
        {
            ArgumentNullException.ThrowIfNull(collection);

            int capacity = collection is ICollection<KeyValuePair<TKey, TValue>> c ? c.Count : 0;
            _dictionary = new(capacity, comparer);
            _list = new(capacity);

            AddRange(collection);
        }

        /// <summary>Gets the <see cref="IEqualityComparer{TKey}"/> that is used to determine equality of keys for the dictionary.</summary>
        public IEqualityComparer<TKey> Comparer => _dictionary.Comparer;

        /// <summary>Gets the number of key/value pairs contained in the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public int Count => _dictionary.Count;

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <inheritdoc/>
        bool IDictionary.IsReadOnly => false;

        /// <inheritdoc/>
        bool IList.IsReadOnly => false;

        /// <inheritdoc/>
        bool IDictionary.IsFixedSize => false;

        /// <inheritdoc/>
        bool IList.IsFixedSize => false;

        /// <summary>Gets a collection containing the keys in the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public KeyCollection Keys => _keys ??= new(this);

        /// <inheritdoc/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc/>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        /// <inheritdoc/>
        ICollection IDictionary.Keys => Keys;

        /// <summary>Gets a collection containing the values in the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public ValueCollection Values => _values ??= new(this);

        /// <inheritdoc/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc/>
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        /// <inheritdoc/>
        ICollection IDictionary.Values => Values;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

        /// <inheritdoc/>
        object? IList.this[int index]
        {
            get => GetAt(index);
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (value is not KeyValuePair<TKey, TValue> tpair)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(KeyValuePair<TKey, TValue>)), nameof(value));
                }

                SetAt(index, tpair.Key, tpair.Value);
            }
        }

        /// <inheritdoc/>
        object? IDictionary.this[object key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);

                if (key is TKey tkey && TryGetValue(tkey, out TValue? value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                if (default(TValue) is not null)
                {
                    ArgumentNullException.ThrowIfNull(value);
                }

                if (key is not TKey tkey)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_WrongType, key, typeof(TKey)), nameof(key));
                }

                TValue tvalue = default!;
                if (value is not null)
                {
                    if (value is not TValue temp)
                    {
                        throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
                    }

                    tvalue = temp;
                }

                this[tkey] = tvalue;
            }
        }

        /// <inheritdoc/>
        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
        {
            get => GetAt(index);
            set => SetAt(index, value.Key, value.Value);
        }

        /// <inheritdoc/>
        KeyValuePair<TKey, TValue> IReadOnlyList<KeyValuePair<TKey, TValue>>.this[int index] => GetAt(index);

        /// <summary>Gets or sets the value associated with the specified key.</summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key. If the specified key is not found, a get operation throws a <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection.</exception>
        /// <remarks>Setting the value of an existing key does not impact its order in the collection.</remarks>
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                ref TValue? valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, key, out bool keyExists);

                valueRef = value;
                if (!keyExists)
                {
                    _list.Add(key);
                }
            }
        }

        /// <summary>Adds the specified key and value to the dictionary.</summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <exception cref="ArgumentNullException">key is null.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="OrderedDictionary{TKey, TValue}"/>.</exception>
        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _list.Add(key);
        }

        /// <summary>Adds each element of the enumerable to the dictionary.</summary>
        private void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            Debug.Assert(collection is not null);

            if (collection is KeyValuePair<TKey, TValue>[] array)
            {
                foreach (KeyValuePair<TKey, TValue> pair in array)
                {
                    Add(pair.Key, pair.Value);
                }
            }
            else
            {
                foreach (KeyValuePair<TKey, TValue> pair in collection)
                {
                    Add(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>Removes all keys and values from the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
        }

        /// <summary>Determines whether the <see cref="OrderedDictionary{TKey, TValue}"/> contains the specified key.</summary>
        /// <param name="key">The key to locate in the <see cref="OrderedDictionary{TKey, TValue}"/>.</param>
        /// <returns>true if the <see cref="OrderedDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false.</returns>
        public bool ContainsKey(TKey key) =>
            _dictionary.ContainsKey(key);

        /// <summary>Determines whether the <see cref="OrderedDictionary{TKey, TValue}"/> contains a specific value.</summary>
        /// <param name="value">The value to locate in the <see cref="OrderedDictionary{TKey, TValue}"/>. The value can be null for reference types.</param>
        /// <returns>true if the <see cref="OrderedDictionary{TKey, TValue}"/> contains an element with the specified value; otherwise, false.</returns>
        public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

        /// <summary>Gets the key/value pair at the specified index.</summary>
        /// <param name="index">The zero-based index of the pair to get.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.</exception>
        public KeyValuePair<TKey, TValue> GetAt(int index)
        {
            TKey key = _list[index];
            return new(key, _dictionary[key]);
        }

        /// <summary>Determines the index of a specific key in the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The index of <paramref name="key"/> if found; otherwise, -1.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public int IndexOf(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return _list.IndexOf(key);
        }

        /// <summary>Inserts an item into the collection at the specified index.</summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value to insert.</param>
        /// <exception cref="ArgumentNullException">key is null.</exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="OrderedDictionary{TKey, TValue}"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.</exception>
        public void Insert(int index, TKey key, TValue value)
        {
            if ((uint)index > (uint)_list.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_IndexMustBeLessOrEqual);
            }

            _dictionary.Add(key, value);
            _list.Insert(index, key);
        }

        /// <summary>Removes the value with the specified key from the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            if (_dictionary.Remove(key))
            {
                _list.Remove(key);
                return true;
            }

            return false;
        }

        /// <summary>Removes the value with the specified key from the <see cref="OrderedDictionary{TKey, TValue}"/> and copies the element to the value parameter.</summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <param name="value">The removed element.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_dictionary.Remove(key, out value))
            {
                _list.Remove(key);
                return true;
            }

            return false;
        }

        /// <summary>Removes the key/value pair at the specified index.</summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            TKey key = _list[index];
            _list.RemoveAt(index);
            _dictionary.Remove(key);
        }

        /// <summary>Sets the value for the key at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <param name="value">The value to store at the specified index.</param>
        public void SetAt(int index, TValue value) => _dictionary[_list[index]] = value;

        /// <summary>Sets the key/value pair at the specified index.</summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <param name="key">The key to store at the specified index.</param>
        /// <param name="value">The value to store at the specified index.</param>
        /// <exception cref="ArgumentException"></exception>
        public void SetAt(int index, TKey key, TValue value)
        {
            TKey existing = _list[index];

            if (_dictionary.ContainsKey(key))
            {
                throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            }

            _dictionary.Remove(existing);
            _dictionary.Add(key, value);

            _list[index] = key;
        }

        /// <summary>Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries.</summary>
        public void TrimExcess()
        {
            _dictionary.TrimExcess();
            _list.TrimExcess();
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key, if the key is found;
        /// otherwise, the default value for the type of the value parameter.
        /// </param>
        /// <returns>true if the <see cref="OrderedDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);

        /// <summary>Returns an enumerator that iterates through the <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        /// <returns>A <see cref="OrderedDictionary{TKey, TValue}.Enumerator"/> structure for the <see cref="OrderedDictionary{TKey, TValue}"/>.</returns>
        public Enumerator GetEnumerator()
        {
            AssertInvariants();

            return new(this, useDictionaryEntry: false);
        }

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() =>
            Count == 0 ? EnumerableHelpers.GetEmptyEnumerator<KeyValuePair<TKey, TValue>>() :
            GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();

        /// <inheritdoc/>
        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(this, useDictionaryEntry: true);

        /// <inheritdoc/>
        int IList<KeyValuePair<TKey, TValue>>.IndexOf(KeyValuePair<TKey, TValue> item)
        {
            ArgumentNullException.ThrowIfNull(item.Key, nameof(item));

            if (_dictionary.TryGetValue(item.Key, out TValue? value) &&
                EqualityComparer<TValue>.Default.Equals(value, item.Value))
            {
                return _list.IndexOf(item.Key);
            }

            return -1;
        }

        /// <inheritdoc/>
        void IList<KeyValuePair<TKey, TValue>>.Insert(int index, KeyValuePair<TKey, TValue> item) => Insert(index, item.Key, item.Value);

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            ArgumentNullException.ThrowIfNull(item.Key, nameof(item));

            return
                _dictionary.TryGetValue(item.Key, out TValue? value) &&
                EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            foreach (TKey key in _list)
            {
                array[arrayIndex++] = new(key, _dictionary[key]);
            }
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
            TryGetValue(item.Key, out TValue? value) &&
            EqualityComparer<TValue>.Default.Equals(value, item.Value) &&
            Remove(item.Key);

        /// <inheritdoc/>
        void IDictionary.Add(object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (default(TValue) != null)
            {
                ArgumentNullException.ThrowIfNull(value);
            }

            if (key is not TKey tkey)
            {
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, key, typeof(TKey)), nameof(key));
            }

            if (default(TValue) is not null)
            {
                ArgumentNullException.ThrowIfNull(value);
            }

            TValue tvalue = default!;
            if (value is not null)
            {
                if (value is not TValue temp)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
                }

                tvalue = temp;
            }

            Add(tkey, tvalue);
        }

        /// <inheritdoc/>
        bool IDictionary.Contains(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return key is TKey tkey && ContainsKey(tkey);
        }

        /// <inheritdoc/>
        void IDictionary.Remove(object key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key is TKey tkey)
            {
                Remove(tkey);
            }
        }

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Length - index < _dictionary.Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            if (array is KeyValuePair<TKey, TValue>[] tarray)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)this).CopyTo(tarray, index);
            }
            else
            {
                try
                {
                    if (array is not object[] objects)
                    {
                        throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                    }

                    foreach (KeyValuePair<TKey, TValue> pair in this)
                    {
                        objects[index++] = pair;
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                }
            }
        }

        /// <inheritdoc/>
        int IList.Add(object? value)
        {
            if (value is not KeyValuePair<TKey, TValue> pair)
            {
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(KeyValuePair<TKey, TValue>)), nameof(value));
            }

            Add(pair.Key, pair.Value);
            return Count - 1;
        }

        /// <inheritdoc/>
        bool IList.Contains(object? value) =>
            value is KeyValuePair<TKey, TValue> pair &&
            _dictionary.TryGetValue(pair.Key, out TValue? v) &&
            EqualityComparer<TValue>.Default.Equals(v, pair.Value);

        /// <inheritdoc/>
        int IList.IndexOf(object? value)
        {
            if (value is KeyValuePair<TKey, TValue> pair)
            {
                return ((IList<KeyValuePair<TKey, TValue>>)this).IndexOf(pair);
            }

            return -1;
        }

        /// <inheritdoc/>
        void IList.Insert(int index, object? value)
        {
            if (value is not KeyValuePair<TKey, TValue> pair)
            {
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(KeyValuePair<TKey, TValue>)), nameof(value));
            }

            Insert(index, pair.Key, pair.Value);
        }

        /// <inheritdoc/>
        void IList.Remove(object? value)
        {
            if (value is KeyValuePair<TKey, TValue> pair)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)this).Remove(pair);
            }
        }

        /// <summary>Provides debug validation of the consistency of the collection.</summary>
        [Conditional("DEBUG")]
        private void AssertInvariants()
        {
            Debug.Assert(_dictionary.Count == _list.Count, $"Expected dictionary count {_dictionary.Count} to equal list count {_list.Count}");
            foreach (TKey key in _list)
            {
                Debug.Assert(_dictionary.ContainsKey(key), $"Expected dictionary to contain key {key}");
            }
        }

        /// <summary>Enumerates the elements of a <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            /// <summary>The dictionary being enumerated.</summary>
            private readonly OrderedDictionary<TKey, TValue> _dictionary;
            /// <summary>The wrapped ordered enumerator.</summary>
            private List<TKey>.Enumerator _keyEnumerator;
            /// <summary>Whether Current should be a DictionaryEntry.</summary>
            private bool _useDictionaryEntry;

            /// <summary>Initialize the enumerator.</summary>
            internal Enumerator(OrderedDictionary<TKey, TValue> dictionary, bool useDictionaryEntry)
            {
                _dictionary = dictionary;
                _keyEnumerator = dictionary._list.GetEnumerator();
                _useDictionaryEntry = useDictionaryEntry;
            }

            /// <inheritdoc/>
            public KeyValuePair<TKey, TValue> Current { get; private set; }

            /// <inheritdoc/>
            readonly object IEnumerator.Current => _useDictionaryEntry ?
                new DictionaryEntry(Current.Key, Current.Value) :
                Current;

            /// <inheritdoc/>
            readonly DictionaryEntry IDictionaryEnumerator.Entry => new(Current.Key, Current.Value);

            /// <inheritdoc/>
            readonly object IDictionaryEnumerator.Key => Current.Key;

            /// <inheritdoc/>
            readonly object? IDictionaryEnumerator.Value => Current.Value;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                if (_keyEnumerator.MoveNext())
                {
                    Current = new(_keyEnumerator.Current, _dictionary._dictionary[_keyEnumerator.Current]);
                    return true;
                }

                Current = default!;
                return false;
            }

            /// <inheritdoc/>
            void IEnumerator.Reset() => EnumerableHelpers.Reset(ref _keyEnumerator);

            /// <inheritdoc/>
            readonly void IDisposable.Dispose() { }
        }

        /// <summary>Represents the collection of keys in a <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public sealed class KeyCollection : IList<TKey>, IReadOnlyList<TKey>, IList
        {
            /// <summary>The dictionary whose keys are being exposed.</summary>
            private readonly OrderedDictionary<TKey, TValue> _dictionary;

            /// <summary>Initialize the collection wrapper.</summary>
            internal KeyCollection(OrderedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <inheritdoc/>
            bool ICollection<TKey>.IsReadOnly => true;

            /// <inheritdoc/>
            bool IList.IsReadOnly => true;

            /// <inheritdoc/>
            bool IList.IsFixedSize => false;

            /// <inheritdoc/>
            bool ICollection.IsSynchronized => false;

            /// <inheritdoc/>
            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            /// <inheritdoc/>
            public bool Contains(TKey key) => _dictionary.ContainsKey(key);

            /// <inheritdoc/>
            bool IList.Contains(object? value) => value is TKey key && Contains(key);

            /// <inheritdoc/>
            public void CopyTo(TKey[] array, int arrayIndex) => _dictionary._list.CopyTo(array, arrayIndex);

            /// <inheritdoc/>
            void ICollection.CopyTo(Array array, int index) =>
                ((ICollection)_dictionary._list).CopyTo(array, index);

            /// <inheritdoc/>
            TKey IList<TKey>.this[int index]
            {
                get => _dictionary._list[index];
                set => throw new NotSupportedException();
            }

            /// <inheritdoc/>
            object? IList.this[int index]
            {
                get => _dictionary._list[index];
                set => throw new NotSupportedException();
            }

            /// <inheritdoc/>
            TKey IReadOnlyList<TKey>.this[int index] => _dictionary._list[index];

            /// <summary>Returns an enumerator that iterates through the <see cref="OrderedDictionary{TKey, TValue}.KeyCollection"/>.</summary>
            /// <returns>A <see cref="OrderedDictionary{TKey, TValue}.KeyCollection.Enumerator"/> for the <see cref="OrderedDictionary{TKey, TValue}.KeyCollection"/>.</returns>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() =>
                Count == 0 ? EnumerableHelpers.GetEmptyEnumerator<TKey>() :
                GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TKey>)this).GetEnumerator();

            /// <inheritdoc/>
            int IList<TKey>.IndexOf(TKey item) => _dictionary.IndexOf(item);

            /// <inheritdoc/>
            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

            /// <inheritdoc/>
            void ICollection<TKey>.Clear() => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList<TKey>.Insert(int index, TKey item) => throw new NotSupportedException();

            /// <inheritdoc/>
            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList<TKey>.RemoveAt(int index) => throw new NotSupportedException();

            /// <inheritdoc/>
            int IList.Add(object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.Clear() => throw new NotSupportedException();

            /// <inheritdoc/>
            int IList.IndexOf(object? value) => value is TKey key ? _dictionary.IndexOf(key) : -1;

            /// <inheritdoc/>
            void IList.Insert(int index, object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.Remove(object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.RemoveAt(int index) => throw new NotSupportedException();

            /// <summary>Enumerates the elements of a <see cref="OrderedDictionary{TKey, TValue}.KeyCollection"/>.</summary>
            public struct Enumerator : IEnumerator<TKey>
            {
                /// <summary>The dictionary whose keys are being enumerated.</summary>
                private readonly OrderedDictionary<TKey, TValue> _dictionary;
                /// <summary>The wrapped ordered enumerator.</summary>
                private List<TKey>.Enumerator _keyEnumerator;

                /// <summary>Initialize the enumerator.</summary>
                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _keyEnumerator = dictionary._list.GetEnumerator();
                }

                /// <inheritdoc/>
                public TKey Current { get; private set; } = default!;

                /// <inheritdoc/>
                readonly object IEnumerator.Current => Current;

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    if (_keyEnumerator.MoveNext())
                    {
                        Current = _keyEnumerator.Current;
                        return true;
                    }

                    Current = default!;
                    return false;
                }

                /// <inheritdoc/>
                void IEnumerator.Reset() => EnumerableHelpers.Reset(ref _keyEnumerator);

                /// <inheritdoc/>
                readonly void IDisposable.Dispose() { }
            }
        }

        /// <summary>Represents the collection of values in a <see cref="OrderedDictionary{TKey, TValue}"/>.</summary>
        public sealed class ValueCollection : IList<TValue>, IReadOnlyList<TValue>, IList
        {
            /// <summary>The dictionary whose values are being exposed.</summary>
            private readonly OrderedDictionary<TKey, TValue> _dictionary;

            /// <summary>Initialize the collection wrapper.</summary>
            internal ValueCollection(OrderedDictionary<TKey, TValue> dictionary) => _dictionary = dictionary;

            /// <inheritdoc/>
            public int Count => _dictionary.Count;

            /// <inheritdoc/>
            bool ICollection<TValue>.IsReadOnly => true;

            /// <inheritdoc/>
            bool IList.IsReadOnly => true;

            /// <inheritdoc/>
            bool IList.IsFixedSize => false;

            /// <inheritdoc/>
            bool ICollection.IsSynchronized => false;

            /// <inheritdoc/>
            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            /// <inheritdoc/>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                ArgumentNullException.ThrowIfNull(array);
                ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
                if (array.Length - arrayIndex < Count)
                {
                    throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall, nameof(array));
                }

                for (int i = 0; i < _dictionary.Count; i++)
                {
                    array[arrayIndex++] = _dictionary._dictionary[_dictionary._list[i]];
                }
            }

            /// <summary>Returns an enumerator that iterates through the <see cref="OrderedDictionary{TKey, TValue}.ValueCollection"/>.</summary>
            /// <returns>A <see cref="OrderedDictionary{TKey, TValue}.ValueCollection.Enumerator"/> for the <see cref="OrderedDictionary{TKey, TValue}.ValueCollection"/>.</returns>
            public Enumerator GetEnumerator() => new(_dictionary);

            /// <inheritdoc/>
            TValue IList<TValue>.this[int index]
            {
                get => _dictionary[_dictionary._list[index]];
                set => throw new NotSupportedException();
            }

            /// <inheritdoc/>
            TValue IReadOnlyList<TValue>.this[int index] => _dictionary._dictionary[_dictionary._list[index]];

            /// <inheritdoc/>
            object? IList.this[int index]
            {
                get => _dictionary[_dictionary._list[index]];
                set => throw new NotSupportedException();
            }

            /// <inheritdoc/>
            bool ICollection<TValue>.Contains(TValue item) => _dictionary._dictionary.ContainsValue(item);

            /// <inheritdoc/>
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() =>
                Count == 0 ? EnumerableHelpers.GetEmptyEnumerator<TValue>() :
                GetEnumerator();

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TValue>)this).GetEnumerator();

            /// <inheritdoc/>
            int IList<TValue>.IndexOf(TValue item)
            {
                for (int i = 0; i < _dictionary.Count; i++)
                {
                    if (EqualityComparer<TValue>.Default.Equals(_dictionary._dictionary[_dictionary._list[i]], item))
                    {
                        return i;
                    }
                }

                return -1;
            }

            /// <inheritdoc/>
            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

            /// <inheritdoc/>
            void ICollection<TValue>.Clear() => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList<TValue>.Insert(int index, TValue item) => throw new NotSupportedException();

            /// <inheritdoc/>
            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList<TValue>.RemoveAt(int index) => throw new NotSupportedException();

            /// <inheritdoc/>
            int IList.Add(object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.Clear() => throw new NotSupportedException();

            /// <inheritdoc/>
            bool IList.Contains(object? value) =>
                value is null && default(TValue) is null ? _dictionary.ContainsValue(default!) :
                value is TValue tvalue && _dictionary.ContainsValue(tvalue);

            /// <inheritdoc/>
            int IList.IndexOf(object? value)
            {
                if (value is null && default(TValue) is null)
                {
                    for (int i = 0; i < _dictionary.Count; i++)
                    {
                        if (_dictionary[_dictionary._list[i]] is null)
                        {
                            return i;
                        }
                    }
                }
                else if (value is TValue tvalue)
                {
                    for (int i = 0; i < _dictionary.Count; i++)
                    {
                        if (EqualityComparer<TValue>.Default.Equals(tvalue, _dictionary[_dictionary._list[i]]))
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }

            /// <inheritdoc/>
            void IList.Insert(int index, object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.Remove(object? value) => throw new NotSupportedException();

            /// <inheritdoc/>
            void IList.RemoveAt(int index) => throw new NotSupportedException();

            /// <inheritdoc/>
            void ICollection.CopyTo(Array array, int index)
            {
                ArgumentNullException.ThrowIfNull(array);

                if (array.Rank != 1)
                {
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
                }

                ArgumentOutOfRangeException.ThrowIfNegative(index);

                if (array.Length - index < _dictionary.Count)
                {
                    throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
                }

                if (array is TValue[] values)
                {
                    CopyTo(values, index);
                }
                else
                {
                    try
                    {
                        if (array is not object?[] objects)
                        {
                            throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                        }

                        foreach (TValue value in this)
                        {
                            objects[index++] = value;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                    }
                }
            }

            /// <summary>Enumerates the elements of a <see cref="OrderedDictionary{TKey, TValue}.ValueCollection"/>.</summary>
            public struct Enumerator : IEnumerator<TValue>
            {
                /// <summary>The dictionary whose keys are being enumerated.</summary>
                private readonly OrderedDictionary<TKey, TValue> _dictionary;
                /// <summary>The wrapped ordered enumerator.</summary>
                private List<TKey>.Enumerator _keyEnumerator;

                /// <summary>Initialize the enumerator.</summary>
                internal Enumerator(OrderedDictionary<TKey, TValue> dictionary)
                {
                    _dictionary = dictionary;
                    _keyEnumerator = dictionary._list.GetEnumerator();
                }

                /// <inheritdoc/>
                public TValue Current { get; private set; } = default!;

                /// <inheritdoc/>
                readonly object? IEnumerator.Current => Current;

                /// <inheritdoc/>
                public bool MoveNext()
                {
                    if (_keyEnumerator.MoveNext())
                    {
                        Current = _dictionary._dictionary[_keyEnumerator.Current];
                        return true;
                    }

                    Current = default!;
                    return false;
                }

                /// <inheritdoc/>
                void IEnumerator.Reset() => EnumerableHelpers.Reset(ref _keyEnumerator);

                /// <inheritdoc/>
                readonly void IDisposable.Dispose() { }
            }
        }
    }
}
