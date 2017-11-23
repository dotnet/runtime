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
    /// Contains the inner <see cref="ImmutableDictionary{TKey, TValue}.Builder"/> class.
    /// </content>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public sealed partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// A dictionary that mutates with little or no memory allocations,
        /// can produce and/or build on immutable dictionary instances very efficiently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// While <see cref="ImmutableDictionary{TKey, TValue}.AddRange(IEnumerable{KeyValuePair{TKey, TValue}})"/>
        /// and other bulk change methods already provide fast bulk change operations on the collection, this class allows
        /// multiple combinations of changes to be made to a set with equal efficiency.
        /// </para>
        /// <para>
        /// Instance members of this class are <em>not</em> thread-safe.
        /// </para>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "Ignored")]
        [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(ImmutableDictionaryBuilderDebuggerProxy<,>))]
        public sealed class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            /// <summary>
            /// The root of the binary tree that stores the collection.  Contents are typically not entirely frozen.
            /// </summary>
            private SortedInt32KeyNode<HashBucket> _root = SortedInt32KeyNode<HashBucket>.EmptyNode;

            /// <summary>
            /// The comparers.
            /// </summary>
            private Comparers _comparers;

            /// <summary>
            /// The number of elements in this collection.
            /// </summary>
            private int _count;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableDictionary<TKey, TValue> _immutable;

            /// <summary>
            /// A number that increments every time the builder changes its contents.
            /// </summary>
            private int _version;

            /// <summary>
            /// The object callers may use to synchronize access to this collection.
            /// </summary>
            private object _syncRoot;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.Builder"/> class.
            /// </summary>
            /// <param name="map">The map that serves as the basis for this Builder.</param>
            internal Builder(ImmutableDictionary<TKey, TValue> map)
            {
                Requires.NotNull(map, nameof(map));
                _root = map._root;
                _count = map._count;
                _comparers = map._comparers;
                _immutable = map;
            }

            /// <summary>
            /// Gets or sets the key comparer.
            /// </summary>
            /// <value>
            /// The key comparer.
            /// </value>
            public IEqualityComparer<TKey> KeyComparer
            {
                get
                {
                    return _comparers.KeyComparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));
                    if (value != this.KeyComparer)
                    {
                        var comparers = Comparers.Get(value, this.ValueComparer);
                        var input = new MutationInput(SortedInt32KeyNode<HashBucket>.EmptyNode, comparers, 0);
                        var result = ImmutableDictionary<TKey, TValue>.AddRange(this, input);

                        _immutable = null;
                        _comparers = comparers;
                        _count = result.CountAdjustment; // offset from 0
                        this.Root = result.Root;
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
                    return _comparers.ValueComparer;
                }

                set
                {
                    Requires.NotNull(value, nameof(value));
                    if (value != this.ValueComparer)
                    {
                        // When the key comparer is the same but the value comparer is different, we don't need a whole new tree
                        // because the structure of the tree does not depend on the value comparer.
                        // We just need a new root node to store the new value comparer.
                        _comparers = _comparers.WithValueComparer(value);
                        _immutable = null; // invalidate cached immutable
                    }
                }
            }

            #region IDictionary<TKey, TValue> Properties

            /// <summary>
            /// Gets the number of elements contained in the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <returns>The number of elements contained in the <see cref="ICollection{T}"/>.</returns>
            public int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.</returns>
            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TKey> Keys
            {
                get
                {
                    foreach (KeyValuePair<TKey, TValue> item in this)
                    {
                        yield return item.Key;
                    }
                }
            }

            /// <summary>
            /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <returns>An <see cref="ICollection{T}"/> containing the keys of the object that implements <see cref="IDictionary{TKey, TValue}"/>.</returns>
            ICollection<TKey> IDictionary<TKey, TValue>.Keys
            {
                get { return this.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TValue> Values
            {
                get
                {
                    foreach (KeyValuePair<TKey, TValue> item in this)
                    {
                        yield return item.Value;
                    }
                }
            }

            /// <summary>
            /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <returns>An <see cref="ICollection{T}"/> containing the values in the object that implements <see cref="IDictionary{TKey, TValue}"/>.</returns>
            ICollection<TValue> IDictionary<TKey, TValue>.Values
            {
                get { return this.Values.ToArray(this.Count); }
            }

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
            /// </returns>
            ICollection IDictionary.Keys
            {
                get { return this.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <returns>
            /// An <see cref="ICollection{T}"/> containing the values in the object that implements <see cref="IDictionary{TKey, TValue}"/>.
            /// </returns>
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
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array.SetValue(new DictionaryEntry(item.Key, item.Value), arrayIndex++);
                }
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
            /// Gets the initial data to pass to a query or mutation method.
            /// </summary>
            private MutationInput Origin
            {
                get { return new MutationInput(this.Root, _comparers, _count); }
            }

            /// <summary>
            /// Gets or sets the root of this data structure.
            /// </summary>
            private SortedInt32KeyNode<HashBucket> Root
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

            /// <summary>
            /// Gets or sets the element with the specified key.
            /// </summary>
            /// <returns>The element with the specified key.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
            /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
            /// <exception cref="NotSupportedException">The property is set and the <see cref="IDictionary{TKey, TValue}"/> is read-only.</exception>
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
                    var result = ImmutableDictionary<TKey, TValue>.Add(key, value, KeyCollisionBehavior.SetValue, this.Origin);
                    this.Apply(result);
                }
            }

            #region Public Methods

            /// <summary>
            /// Adds a sequence of values to this collection.
            /// </summary>
            /// <param name="items">The items.</param>
            [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                var result = ImmutableDictionary<TKey, TValue>.AddRange(items, this.Origin);
                this.Apply(result);
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
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(_root, this);
            }

            /// <summary>
            /// Gets the value for a given key if a matching key exists in the dictionary.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <returns>The value for the key, or the default value of type <typeparamref name="TValue"/> if no matching key was found.</returns>
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
            /// Creates an immutable dictionary based on the contents of this instance.
            /// </summary>
            /// <returns>An immutable map.</returns>
            /// <remarks>
            /// This method is an O(n) operation, and approaches O(1) time as the number of
            /// actual mutations to the set since the last call to this method approaches 0.
            /// </remarks>
            public ImmutableDictionary<TKey, TValue> ToImmutable()
            {
                // Creating an instance of ImmutableSortedMap<T> with our root node automatically freezes our tree,
                // ensuring that the returned instance is immutable.  Any further mutations made to this builder
                // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
                if (_immutable == null)
                {
                    _immutable = ImmutableDictionary<TKey, TValue>.Wrap(_root, _comparers, _count);
                }

                return _immutable;
            }

            #endregion

            #region IDictionary<TKey, TValue> Members

            /// <summary>
            /// Adds an element with the provided key and value to the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <param name="key">The object to use as the key of the element to add.</param>
            /// <param name="value">The object to use as the value of the element to add.</param>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
            /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="IDictionary{TKey, TValue}"/>.</exception>
            /// <exception cref="NotSupportedException">The <see cref="IDictionary{TKey, TValue}"/> is read-only.</exception>
            public void Add(TKey key, TValue value)
            {
                var result = ImmutableDictionary<TKey, TValue>.Add(key, value, KeyCollisionBehavior.ThrowIfValueDifferent, this.Origin);
                this.Apply(result);
            }

            /// <summary>
            /// Determines whether the <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key.
            /// </summary>
            /// <param name="key">The key to locate in the <see cref="IDictionary{TKey, TValue}"/>.</param>
            /// <returns>
            /// true if the <see cref="IDictionary{TKey, TValue}"/> contains an element with the key; otherwise, false.
            /// </returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
            public bool ContainsKey(TKey key)
            {
                return ImmutableDictionary<TKey, TValue>.ContainsKey(key, this.Origin);
            }

            /// <summary>
            /// Determines whether the <see cref="ImmutableDictionary{TKey, TValue}"/>
            /// contains an element with the specified value.
            /// </summary>
            /// <param name="value">
            /// The value to locate in the <see cref="ImmutableDictionary{TKey, TValue}"/>.
            /// The value can be null for reference types.
            /// </param>
            /// <returns>
            /// true if the <see cref="ImmutableDictionary{TKey, TValue}"/> contains
            /// an element with the specified value; otherwise, false.
            /// </returns>
            [Pure]
            public bool ContainsValue(TValue value)
            {
                foreach (KeyValuePair<TKey, TValue> item in this)
                {
                    if (this.ValueComparer.Equals(value, item.Value))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Removes the element with the specified key from the <see cref="IDictionary{TKey, TValue}"/>.
            /// </summary>
            /// <param name="key">The key of the element to remove.</param>
            /// <returns>
            /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="IDictionary{TKey, TValue}"/>.
            /// </returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
            ///   
            /// <exception cref="NotSupportedException">The <see cref="IDictionary{TKey, TValue}"/> is read-only.</exception>
            public bool Remove(TKey key)
            {
                var result = ImmutableDictionary<TKey, TValue>.Remove(key, this.Origin);
                return this.Apply(result);
            }

            /// <summary>
            /// Gets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key whose value to get.</param>
            /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value of the type <typeparamref name="TValue"/>. This parameter is passed uninitialized.</param>
            /// <returns>
            /// true if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false.
            /// </returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
            public bool TryGetValue(TKey key, out TValue value)
            {
                return ImmutableDictionary<TKey, TValue>.TryGetValue(key, this.Origin, out value);
            }

            /// <summary>
            /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
            /// </summary>
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
            {
                return ImmutableDictionary<TKey, TValue>.TryGetKey(equalKey, this.Origin, out actualKey);
            }

            /// <summary>
            /// Adds an item to the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                this.Add(item.Key, item.Value);
            }

            /// <summary>
            /// Removes all items from the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only. </exception>
            public void Clear()
            {
                this.Root = SortedInt32KeyNode<HashBucket>.EmptyNode;
                _count = 0;
            }

            /// <summary>
            /// Determines whether the <see cref="ICollection{T}"/> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="ICollection{T}"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the <see cref="ICollection{T}"/>; otherwise, false.
            /// </returns>
            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return ImmutableDictionary<TKey, TValue>.Contains(item, this.Origin);
            }

            /// <summary>
            /// See the <see cref="ICollection{T}"/> interface.
            /// </summary>
            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                Requires.NotNull(array, nameof(array));

                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            #endregion

            #region ICollection<KeyValuePair<TKey, TValue>> Members

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="ICollection{T}"/>.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="ICollection{T}"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> was successfully removed from the <see cref="ICollection{T}"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="ICollection{T}"/>.
            /// </returns>
            /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                // Before removing based on the key, check that the key (if it exists) has the value given in the parameter as well.
                if (this.Contains(item))
                {
                    return this.Remove(item.Key);
                }

                return false;
            }

            #endregion

            #region IEnumerator<T> methods

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Applies the result of some mutation operation to this instance.
            /// </summary>
            /// <param name="result">The result.</param>
            private bool Apply(MutationResult result)
            {
                this.Root = result.Root;
                _count += result.CountAdjustment;
                return result.CountAdjustment != 0;
            }
        }
    }

    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableDictionaryBuilderDebuggerProxy<TKey, TValue>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableDictionary<TKey, TValue>.Builder _map;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private KeyValuePair<TKey, TValue>[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableDictionaryBuilderDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="map">The collection to display in the debugger</param>
        public ImmutableDictionaryBuilderDebuggerProxy(ImmutableDictionary<TKey, TValue>.Builder map)
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
