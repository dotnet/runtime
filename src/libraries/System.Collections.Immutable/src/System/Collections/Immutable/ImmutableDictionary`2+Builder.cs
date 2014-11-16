// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Validation;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner Builder class.
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
        [DebuggerTypeProxy(typeof(ImmutableDictionary<,>.Builder.DebuggerProxy))]
        public sealed class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            /// <summary>
            /// The root of the binary tree that stores the collection.  Contents are typically not entirely frozen.
            /// </summary>
            private ImmutableSortedDictionary<int, HashBucket>.Node root = ImmutableSortedDictionary<int, HashBucket>.Node.EmptyNode;

            /// <summary>
            /// The comparers.
            /// </summary>
            private Comparers comparers;

            /// <summary>
            /// The number of elements in this collection.
            /// </summary>
            private int count;

            /// <summary>
            /// Caches an immutable instance that represents the current state of the collection.
            /// </summary>
            /// <value>Null if no immutable view has been created for the current version.</value>
            private ImmutableDictionary<TKey, TValue> immutable;

            /// <summary>
            /// A number that increments every time the builder changes its contents.
            /// </summary>
            private int version;

            /// <summary>
            /// The object callers may use to synchronize access to this collection.
            /// </summary>
            private object syncRoot;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary&lt;TKey, TValue&gt;.Builder"/> class.
            /// </summary>
            /// <param name="map">The map that serves as the basis for this Builder.</param>
            internal Builder(ImmutableDictionary<TKey, TValue> map)
            {
                Requires.NotNull(map, "map");
                this.root = map.root;
                this.count = map.count;
                this.comparers = map.comparers;
                this.immutable = map;
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
                    return this.comparers.KeyComparer;
                }

                set
                {
                    Requires.NotNull(value, "value");
                    if (value != this.KeyComparer)
                    {
                        var comparers = Comparers.Get(value, this.ValueComparer);
                        var input = new MutationInput(ImmutableSortedDictionary<int, HashBucket>.Node.EmptyNode, comparers, 0);
                        var result = ImmutableDictionary<TKey, TValue>.AddRange(this, input);

                        this.immutable = null;
                        this.comparers = comparers;
                        this.count = result.CountAdjustment; // offset from 0
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
                    return this.comparers.ValueComparer;
                }

                set
                {
                    Requires.NotNull(value, "value");
                    if (value != this.ValueComparer)
                    {
                        // When the key comparer is the same but the value comparer is different, we don't need a whole new tree
                        // because the structure of the tree does not depend on the value comparer.
                        // We just need a new root node to store the new value comparer.
                        this.comparers = this.comparers.WithValueComparer(value);
                        this.immutable = null; // invalidate cached immutable
                    }
                }
            }

            #region IDictionary<TKey, TValue> Properties

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
            public int Count
            {
                get { return this.count; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TKey> Keys
            {
                get { return this.root.Values.SelectMany(b => b).Select(kv => kv.Key); }
            }

            /// <summary>
            /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
            /// </summary>
            /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
            ICollection<TKey> IDictionary<TKey, TValue>.Keys
            {
                get { return this.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// See <see cref="IReadOnlyDictionary{TKey, TValue}"/>
            /// </summary>
            public IEnumerable<TValue> Values
            {
                get { return this.root.Values.SelectMany(b => b).Select(kv => kv.Value).ToArray(this.Count); }
            }

            /// <summary>
            /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
            /// </summary>
            /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
            ICollection<TValue> IDictionary<TKey, TValue>.Values
            {
                get { return this.Values.ToArray(this.Count); }
            }

            #endregion

            #region IDictionary Properties

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.IDictionary" /> object has a fixed size.
            /// </summary>
            /// <returns>true if the <see cref="T:System.Collections.IDictionary" /> object has a fixed size; otherwise, false.</returns>
            bool IDictionary.IsFixedSize
            {
                get { return false; }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
            /// </summary>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.
            ///   </returns>
            bool IDictionary.IsReadOnly
            {
                get { return false; }
            }

            /// <summary>
            /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.
            ///   </returns>
            ICollection IDictionary.Keys
            {
                get { return this.Keys.ToArray(this.Count); }
            }

            /// <summary>
            /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.
            ///   </returns>
            ICollection IDictionary.Values
            {
                get { return this.Values.ToArray(this.Count); }
            }

            #endregion

            #region ICollection Properties

            /// <summary>
            /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
            /// </summary>
            /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            object ICollection.SyncRoot
            {
                get
                {
                    if (this.syncRoot == null)
                    {
                        Threading.Interlocked.CompareExchange<Object>(ref this.syncRoot, new Object(), null);
                    }

                    return this.syncRoot;
                }
            }

            /// <summary>
            /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
            /// </summary>
            /// <returns>true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, false.</returns>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            #endregion

            #region IDictionary Methods

            /// <summary>
            /// Adds an element with the provided key and value to the <see cref="T:System.Collections.IDictionary" /> object.
            /// </summary>
            /// <param name="key">The <see cref="T:System.Object" /> to use as the key of the element to add.</param>
            /// <param name="value">The <see cref="T:System.Object" /> to use as the value of the element to add.</param>
            void IDictionary.Add(object key, object value)
            {
                this.Add((TKey)key, (TValue)value);
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.IDictionary" /> object contains an element with the specified key.
            /// </summary>
            /// <param name="key">The key to locate in the <see cref="T:System.Collections.IDictionary" /> object.</param>
            /// <returns>
            /// true if the <see cref="T:System.Collections.IDictionary" /> contains an element with the key; otherwise, false.
            /// </returns>
            bool IDictionary.Contains(object key)
            {
                return this.ContainsKey((TKey)key);
            }

            /// <summary>
            /// Returns an <see cref="T:System.Collections.IDictionaryEnumerator" /> object for the <see cref="T:System.Collections.IDictionary" /> object.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IDictionaryEnumerator" /> object for the <see cref="T:System.Collections.IDictionary" /> object.
            /// </returns>
            /// <exception cref="System.NotImplementedException"></exception>
            IDictionaryEnumerator IDictionary.GetEnumerator()
            {
                return new DictionaryEnumerator<TKey, TValue>(this.GetEnumerator());
            }

            /// <summary>
            /// Removes the element with the specified key from the <see cref="T:System.Collections.IDictionary" /> object.
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
            /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                Requires.NotNull(array, "array");
                Requires.Range(arrayIndex >= 0, "arrayIndex");
                Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");

                if (this.count == 0) 
                {
                    return;
                }

                int[] indices = new int[1]; // SetValue takes a params array; lifting out the implicit allocation from the loop
                foreach (var item in this)
                {
                    indices[0] = arrayIndex++;
                    array.SetValue(new DictionaryEntry(item.Key, item.Value), indices);
                }
            }

            #endregion

            /// <summary>
            /// Gets the current version of the contents of this builder.
            /// </summary>
            internal int Version
            {
                get { return this.version; }
            }

            /// <summary>
            /// Gets the initial data to pass to a query or mutation method.
            /// </summary>
            private MutationInput Origin
            {
                get { return new MutationInput(this.Root, this.comparers, this.count); }
            }

            /// <summary>
            /// Gets or sets the root of this data structure.
            /// </summary>
            private ImmutableSortedDictionary<int, HashBucket>.Node Root
            {
                get
                {
                    return this.root;
                }

                set
                {
                    // We *always* increment the version number because some mutations
                    // may not create a new value of root, although the existing root
                    // instance may have mutated.
                    this.version++;

                    if (this.root != value)
                    {
                        this.root = value;

                        // Clear any cached value for the immutable view since it is now invalidated.
                        this.immutable = null;
                    }
                }
            }

            /// <summary>
            /// Gets or sets the element with the specified key.
            /// </summary>
            /// <returns>The element with the specified key.</returns>
            /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
            /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
            /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
            public TValue this[TKey key]
            {
                get
                {
                    TValue value;
                    if (this.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    throw new KeyNotFoundException();
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
                Requires.NotNull(keys, "keys");

                foreach (var key in keys)
                {
                    this.Remove(key);
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this.root, this);
            }

            /// <summary>
            /// Gets the value for a given key if a matching key exists in the dictionary.
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <returns>The value for the key, or <c>default(TValue)</c> if no matching key was found.</returns>
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
                Requires.NotNullAllowStructs(key, "key");

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
                if (this.immutable == null)
                {
                    this.immutable = ImmutableDictionary<TKey, TValue>.Wrap(this.root, this.comparers, this.count);
                }

                return this.immutable;
            }

            #endregion

            #region IDictionary<TKey, TValue> Members

            /// <summary>
            /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
            /// </summary>
            /// <param name="key">The object to use as the key of the element to add.</param>
            /// <param name="value">The object to use as the value of the element to add.</param>
            /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
            /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
            public void Add(TKey key, TValue value)
            {
                var result = ImmutableDictionary<TKey, TValue>.Add(key, value, KeyCollisionBehavior.ThrowIfValueDifferent, this.Origin);
                this.Apply(result);
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
            /// </summary>
            /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
            /// <returns>
            /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
            /// </returns>
            /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
            public bool ContainsKey(TKey key)
            {
                return ImmutableDictionary<TKey, TValue>.ContainsKey(key, this.Origin);
            }

            /// <summary>
            /// Determines whether the ImmutableSortedMap&lt;TKey,TValue&gt;
            /// contains an element with the specified value.
            /// </summary>
            /// <param name="value">
            /// The value to locate in the ImmutableSortedMap&lt;TKey,TValue&gt;.
            /// The value can be null for reference types.
            /// </param>
            /// <returns>
            /// true if the ImmutableSortedMap&lt;TKey,TValue&gt; contains
            /// an element with the specified value; otherwise, false.
            /// </returns>
            [Pure]
            public bool ContainsValue(TValue value)
            {
                return this.Values.Contains(value, this.ValueComparer);
            }

            /// <summary>
            /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
            /// </summary>
            /// <param name="key">The key of the element to remove.</param>
            /// <returns>
            /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
            /// </returns>
            /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
            ///   
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
            public bool Remove(TKey key)
            {
                var result = ImmutableDictionary<TKey, TValue>.Remove(key, this.Origin);
                return this.Apply(result);
            }

            /// <summary>
            /// Gets the value associated with the specified key.
            /// </summary>
            /// <param name="key">The key whose value to get.</param>
            /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
            /// <returns>
            /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
            /// </returns>
            /// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
            public bool TryGetValue(TKey key, out TValue value)
            {
                return ImmutableDictionary<TKey, TValue>.TryGetValue(key, this.Origin, out value);
            }

            /// <summary>
            /// See the <see cref="IImmutableDictionary&lt;TKey, TValue&gt;"/> interface.
            /// </summary>
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
            {
                return ImmutableDictionary<TKey, TValue>.TryGetKey(equalKey, this.Origin, out actualKey);
            }

            /// <summary>
            /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
            public void Add(KeyValuePair<TKey, TValue> item)
            {
                this.Add(item.Key, item.Value);
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
            public void Clear()
            {
                this.Root = ImmutableSortedDictionary<int, HashBucket>.Node.EmptyNode;
                this.count = 0;
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
            /// </returns>
            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return ImmutableDictionary<TKey, TValue>.Contains(item, this.Origin);
            }

            /// <summary>
            /// See the <see cref="ICollection&lt;T&gt;"/> interface.
            /// </summary>
            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                Requires.NotNull(array, "array");

                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            #endregion

            #region ICollection<KeyValuePair<TKey, TValue>> Members

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </returns>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
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
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
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
                this.count += result.CountAdjustment;
                return result.CountAdjustment != 0;
            }
            /// <summary>
            /// A simple view of the immutable collection that the debugger can show to the developer.
            /// </summary>
            [ExcludeFromCodeCoverage]
            private class DebuggerProxy
            {
                /// <summary>
                /// The collection to be enumerated.
                /// </summary>
                private readonly ImmutableDictionary<TKey, TValue>.Builder map;

                /// <summary>
                /// The simple view of the collection.
                /// </summary>
                private KeyValuePair<TKey, TValue>[] contents;

                /// <summary>   
                /// Initializes a new instance of the <see cref="DebuggerProxy"/> class.
                /// </summary>
                /// <param name="map">The collection to display in the debugger</param>
                public DebuggerProxy(ImmutableDictionary<TKey, TValue>.Builder map)
                {
                    Requires.NotNull(map, "map");
                    this.map = map;
                }

                /// <summary>
                /// Gets a simple debugger-viewable collection.
                /// </summary>
                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public KeyValuePair<TKey, TValue>[] Contents
                {
                    get
                    {
                        if (this.contents == null)
                        {
                            this.contents = this.map.ToArray(this.map.Count);
                        }

                        return this.contents;
                    }
                }
            }
        }
    }
}
