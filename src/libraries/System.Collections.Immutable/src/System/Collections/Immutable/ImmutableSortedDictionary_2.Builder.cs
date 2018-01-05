// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableSortedDictionary{TKey, TValue}.Builder"/> class.
    /// </content>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public sealed partial class ImmutableSortedDictionary<TKey, TValue>
    {
        /// <summary>
        /// A sorted dictionary that mutates with little or no memory allocations,
        /// can produce and/or build on immutable sorted dictionary instances very efficiently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This class allows multiple combinations of changes to be made to a set with equal efficiency.
        /// </para>
        /// <para>
        /// Instance members of this class are <em>not</em> thread-safe.
        /// </para>
        /// </remarks>
        [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Ignored")]
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ImmutableSortedDictionaryBuilderDebuggerProxy<,>))]
        public sealed class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            /// <summary>
            /// The binary tree used to store the contents of the map.  Contents are typically not entirely frozen.
            /// </summary>
            private Node _root = Node.EmptyNode;

            /// <summary>
            /// The key comparer.
            /// </summary>
            private IComparer<TKey> _keyComparer = Comparer<TKey>.Default;

            /// <summary>
            /// The value comparer.
            /// </summary>
            private IEqualityComparer<TValue> _valueComparer = EqualityComparer<TValue>.Default;

            /// <summary>
            /// The number of entries in the map.
            /// </summary>
            private int _count;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableSortedDictionary<TKey, TValue> _immutable;

            /// <summary>
            /// A number that increments every time the builder changes its contents.
            /// </summary>
            private int _version;

            /// <summary>
            /// The object callers may use to synchronize access to this collection.
            /// </summary>
            private object _syncRoot;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            /// <param name="map">A map to act as the basis for a new map.</param>
            internal Builder(ImmutableSortedDictionary<TKey, TValue> map)
            {
                Requires.NotNull(map, nameof(map));
                _root = map._root;
                _keyComparer = map.KeyComparer;
                _valueComparer = map.ValueComparer;
                _count = map.Count;
                _immutable = map;
            }

            #region IDictionary<TKey, TValue> Properties and Indexer

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            ICollection<TKey> IDictionary<TKey, TValue>.Keys
            {
                get { return this.Root.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TKey> Keys
            {
                get { return this.Root.Keys; }
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            ICollection<TValue> IDictionary<TKey, TValue>.Values
            {
                get { return this.Root.Values.ToArray(this.Count); }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TValue> Values
            {
                get { return this.Root.Values; }
            }

            /// <summary>
            /// Gets the number of elements in this map.
            /// </summary>
            public int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets a value indicating whether this instance is read-only.
            /// </summary>
            /// <value>Always <c>false</c>.</value>
            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
            {
                get { return false; }
            }

            #endregion

            /// <summary>
            /// Gets the current version of the contents of this builder.
            /// </summary>
            internal int Version
            {
                get { return _version; }
            }

            /// <summary>
            /// Gets or sets the root node that represents the data in this collection.
            /// </summary>
            private Node Root
            {
                get
                {
                    return _root;
                }

                set
                {
                    // We *always* increment the version number because some mutations
                    // may not create a new value of root, although the existing root
                    // instance may have mutated.
                    _version++;

                    if (_root != value)
                    {
                        _root = value;

                        // Clear any cached value for the immutable view since it is now invalidated.
                        _immutable = null;
                    }
                }
            }

            #region IDictionary<TKey, TValue> Indexer

            /// <summary>
            /// Gets or sets the value for a given key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>The value associated with the given key.</returns>
            public TValue this[TKey key]
            {
                get
                {
                    TValue value;
                    if (this.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
                }

                set
                {
                    bool replacedExistingValue, mutated;
                    this.Root = _root.SetItem(key, value, _keyComparer, _valueComparer, out replacedExistingValue, out mutated);
                    if (mutated && !replacedExistingValue)
                    {
                        _count++;
                    }
                }
            }

#if FEATURE_ITEMREFAPI
            /// <summary>
            /// Returns a read-only reference to the value associated with the provided key.
            /// </summary>
            /// <exception cref="KeyNotFoundException">If the key is not present.</exception>
            public ref readonly TValue ValueRef(TKey key)
            {
                Requires.NotNullAllowStructs(key, nameof(key));

                return ref _root.ValueRef(key, _keyComparer);
            }
#endif

            #endregion

            #region IDictionary Properties

            /// <summary>
            /// Gets a value indicating whether the <see cref="IDictionary"/> object has a fixed size.
            /// </summary>
            /// <returns>true if the <see cref="IDictionary"/> object has a fixed size; otherwise, false.</returns>
            bool IDictionary.IsFixedSize
            {
                get { return false; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
            ///   </returns>
            bool IDictionary.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <returns>
            /// An <see cref="ICollection{T}"/> containing the keys of the object that implements <see cref="IDictionary{TKey, TValue}"/>.
            ///   </returns>
            ICollection IDictionary.Keys
            {
                get { return this.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <returns>
            /// An <see cref="ICollection{T}"/> containing the values in the object that implements <see cref="IDictionary{TKey, TValue}"/>.
            ///   </returns>
            ICollection IDictionary.Values
            {
                get { return this.Values.ToArray(this.Count); }
            }

            #endregion

            #region ICollection Properties

            /// <summary>
            /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
            /// </summary>
            /// <returns>An object that can be used to synchronize access to the <see cref="ICollection"/>.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object ICollection.SyncRoot
            {
                get
                {
                    if (_syncRoot == null)
                    {
                        Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                    }

                    return _syncRoot;
                }
            }

            /// <summary>
            /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
            /// </summary>
            /// <returns>true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            /// <summary>
            /// Gets or sets the key comparer.
            /// </summary>
            /// <value>
            /// The key comparer.
            /// </value>
            public IComparer<TKey> KeyComparer
            {
                get
                {
                    return _keyComparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));
                    if (value != _keyComparer)
                    {
                        var newRoot = Node.EmptyNode;
                        int count = 0;
                        foreach (var item in this)
                        {
                            bool mutated;
                            newRoot = newRoot.Add(item.Key, item.Value, value, _valueComparer, out mutated);
                            if (mutated)
                            {
                                count++;
                            }
                        }

                        _keyComparer = value;
                        this.Root = newRoot;
                        _count = count;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the value comparer.
            /// </summary>
            /// <value>
            /// The value comparer.
            /// </value>
            public IEqualityComparer<TValue> ValueComparer
            {
                get
                {
                    return _valueComparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));
                    if (value != _valueComparer)
                    {
                        // When the key comparer is the same but the value comparer is different, we don't need a whole new tree
                        // because the structure of the tree does not depend on the value comparer.
                        // We just need a new root node to store the new value comparer.
                        _valueComparer = value;
                        _immutable = null; // invalidate cached immutable
                    }
                }
            }

            #endregion

            #region IDictionary Methods

            /// <summary>
            /// Adds an element with the provided key and value to the <see cref="IDictionary"/> object.
            /// </summary>
            /// <param name="key">The <see cref="object"/> to use as the key of the element to add.</param>
            /// <param name="value">The <see cref="object"/> to use as the value of the element to add.</param>
            void IDictionary.Add(object key, object value)
            {
                this.Add((TKey)key, (TValue)value);
            }

            /// <summary>
            /// Determines whether the <see cref="IDictionary"/> object contains an element with the specified key.
            /// </summary>
            /// <param name="key">The key to locate in the <see cref="IDictionary"/> object.</param>
            /// <returns>
            /// true if the <see cref="IDictionary"/> contains an element with the key; otherwise, false.
            /// </returns>
            bool IDictionary.Contains(object key)
            {
                return this.ContainsKey((TKey)key);
            }

            /// <summary>
            /// Returns an <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object.
            /// </summary>
            /// <returns>
            /// An <see cref="IDictionaryEnumerator"/> object for the <see cref="IDictionary"/> object.
            /// </returns>
            IDictionaryEnumerator IDictionary.GetEnumerator()
            {
                return new DictionaryEnumerator<TKey, TValue>(this.GetEnumerator());
            }

            /// <summary>
            /// Removes the element with the specified key from the <see cref="IDictionary"/> object.
            /// </summary>
            /// <param name="key">The key of the element to remove.</param>
            void IDictionary.Remove(object key)
            {
                this.Remove((TKey)key);
            }

            /// <summary>
            /// Gets or sets the element with the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns></returns>
            object IDictionary.this[object key]
            {
                get { return this[(TKey)key]; }
                set { this[(TKey)key] = (TValue)value; }
            }

            #endregion

            #region ICollection methods

            /// <summary>
            /// Copies the elements of the <see cref="ICollection"/> to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from <see cref="ICollection"/>. The <see cref="Array"/> must have zero-based indexing.</param>
            /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            void ICollection.CopyTo(Array array, int index)
            {
                this.Root.CopyTo(array, index, this.Count);
            }

            #endregion

            #region IDictionary<TKey, TValue> Methods

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public void Add(TKey key, TValue value)
            {
                bool mutated;
                this.Root = this.Root.Add(key, value, _keyComparer, _valueComparer, out mutated);
                if (mutated)
                {
                    _count++;
                }
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public bool ContainsKey(TKey key)
            {
                return this.Root.ContainsKey(key, _keyComparer);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public bool Remove(TKey key)
            {
                bool mutated;
                this.Root = this.Root.Remove(key, _keyComparer, out mutated);
                if (mutated)
                {
                    _count--;
                }

                return mutated;
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public bool TryGetValue(TKey key, out TValue value)
            {
                return this.Root.TryGetValue(key, _keyComparer, out value);
            }

            /// <summary>
            /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
            /// </summary>
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
            {
                Requires.NotNullAllowStructs(equalKey, nameof(equalKey));
                return this.Root.TryGetKey(equalKey, _keyComparer, out actualKey);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                this.Add(item.Key, item.Value);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public void Clear()
            {
                this.Root = ImmutableSortedDictionary<TKey, TValue>.Node.EmptyNode;
                _count = 0;
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return this.Root.Contains(item, _keyComparer, _valueComparer);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                this.Root.CopyTo(array, arrayIndex, this.Count);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                if (this.Contains(item))
                {
                    return this.Remove(item.Key);
                }

                return false;
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            public ImmutableSortedDictionary<TKey, TValue>.Enumerator GetEnumerator()
            {
                return this.Root.GetEnumerator(this);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Determines whether the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>
            /// contains an element with the specified value.
            /// </summary>
            /// <param name="value">
            /// The value to locate in the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>.
            /// The value can be null for reference types.
            /// </param>
            /// <returns>
            /// true if the <see cref="ImmutableSortedDictionary{TKey, TValue}"/> contains
            /// an element with the specified value; otherwise, false.
            /// </returns>
            [Pure]
            public bool ContainsValue(TValue value)
            {
                return _root.ContainsValue(value, _valueComparer);
            }

            /// <summary>
            /// Removes any entries from the dictionaries with keys that match those found in the specified sequence.
            /// </summary>
            /// <param name="items">The keys for entries to remove from the dictionary.</param>
            [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                Requires.NotNull(items, nameof(items));

                foreach (var pair in items)
                {
                    this.Add(pair);
                }
            }

            /// <summary>
            /// Removes any entries from the dictionaries with keys that match those found in the specified sequence.
            /// </summary>
            /// <param name="keys">The keys for entries to remove from the dictionary.</param>
            public void RemoveRange(IEnumerable<TKey> keys)
            {
                Requires.NotNull(keys, nameof(keys));

                foreach (var key in keys)
                {
                    this.Remove(key);
                }
            }

            /// <summary>
            /// Gets the value for a given key if a matching key exists in the dictionary.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <returns>The value for the key, or the default value for type <typeparamref name="TValue"/> if no matching key was found.</returns>
            [Pure]
            public TValue GetValueOrDefault(TKey key)
            {
                return this.GetValueOrDefault(key, default(TValue));
            }

            /// <summary>
            /// Gets the value for a given key if a matching key exists in the dictionary.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="defaultValue">The default value to return if no matching key is found in the dictionary.</param>
            /// <returns>
            /// The value for the key, or <paramref name="defaultValue"/> if no matching key was found.
            /// </returns>
            [Pure]
            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
            {
                Requires.NotNullAllowStructs(key, nameof(key));

                TValue value;
                if (this.TryGetValue(key, out value))
                {
                    return value;
                }

                return defaultValue;
            }

            /// <summary>
            /// Creates an immutable sorted dictionary based on the contents of this instance.
            /// </summary>
            /// <returns>An immutable map.</returns>
            /// <remarks>
            /// This method is an O(n) operation, and approaches O(1) time as the number of
            /// actual mutations to the set since the last call to this method approaches 0.
            /// </remarks>
            public ImmutableSortedDictionary<TKey, TValue> ToImmutable()
            {
                // Creating an instance of ImmutableSortedMap<T> with our root node automatically freezes our tree,
                // ensuring that the returned instance is immutable.  Any further mutations made to this builder
                // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
                if (_immutable == null)
                {
                    _immutable = Wrap(this.Root, _count, _keyComparer, _valueComparer);
                }

                return _immutable;
            }
            #endregion
        }
    }
    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableSortedDictionaryBuilderDebuggerProxy<TKey, TValue>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableSortedDictionary<TKey, TValue>.Builder _map;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private KeyValuePair<TKey, TValue>[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableSortedDictionaryBuilderDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="map">The collection to display in the debugger</param>
        public ImmutableSortedDictionaryBuilderDebuggerProxy(ImmutableSortedDictionary<TKey, TValue>.Builder map)
        {
            Requires.NotNull(map, nameof(map));
            _map = map;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = _map.ToArray(_map.Count);
                }

                return _contents;
            }
        }
    }
}
