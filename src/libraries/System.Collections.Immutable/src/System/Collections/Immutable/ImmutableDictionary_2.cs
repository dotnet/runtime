// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable unordered dictionary implementation.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableDictionaryDebuggerProxy<,>))]
    public sealed partial class ImmutableDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IImmutableDictionaryInternal<TKey, TValue>, IHashKeyCollection<TKey>, IDictionary<TKey, TValue>, IDictionary
    {
        /// <summary>
        /// An empty immutable dictionary with default equality comparers.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ImmutableDictionary<TKey, TValue> Empty = new ImmutableDictionary<TKey, TValue>();

        /// <summary>
        /// The singleton delegate that freezes the contents of hash buckets when the root of the data structure is frozen.
        /// </summary>
        private static readonly Action<KeyValuePair<int, HashBucket>> s_FreezeBucketAction = (kv) => kv.Value.Freeze();

        /// <summary>
        /// The number of elements in the collection.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// The root node of the tree that stores this map.
        /// </summary>
        private readonly SortedInt32KeyNode<HashBucket> _root;

        /// <summary>
        /// The comparer used when comparing hash buckets.
        /// </summary>
        private readonly Comparers _comparers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="comparers">The comparers.</param>
        /// <param name="count">The number of elements in the map.</param>
        private ImmutableDictionary(SortedInt32KeyNode<HashBucket> root, Comparers comparers, int count)
            : this(Requires.NotNullPassthrough(comparers, nameof(comparers)))
        {
            Requires.NotNull(root, nameof(root));

            root.Freeze(s_FreezeBucketAction);
            _root = root;
            _count = count;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="comparers">The comparers.</param>
        private ImmutableDictionary(Comparers comparers = null)
        {
            _comparers = comparers ?? Comparers.Get(EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default);
            _root = SortedInt32KeyNode<HashBucket>.EmptyNode;
        }

        /// <summary>
        /// How to respond when a key collision is discovered.
        /// </summary>
        internal enum KeyCollisionBehavior
        {
            /// <summary>
            /// Sets the value for the given key, even if that overwrites an existing value.
            /// </summary>
            SetValue,

            /// <summary>
            /// Skips the mutating operation if a key conflict is detected.
            /// </summary>
            Skip,

            /// <summary>
            /// Throw an exception if the key already exists with a different key.
            /// </summary>
            ThrowIfValueDifferent,

            /// <summary>
            /// Throw an exception if the key already exists regardless of its value.
            /// </summary>
            ThrowAlways,
        }

        /// <summary>
        /// The result of a mutation operation.
        /// </summary>
        internal enum OperationResult
        {
            /// <summary>
            /// The change was applied and did not require a change to the number of elements in the collection.
            /// </summary>
            AppliedWithoutSizeChange,

            /// <summary>
            /// The change required element(s) to be added or removed from the collection.
            /// </summary>
            SizeChanged,

            /// <summary>
            /// No change was required (the operation ended in a no-op).
            /// </summary>
            NoChangeRequired,
        }

        #region Public Properties

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public ImmutableDictionary<TKey, TValue> Clear()
        {
            return this.IsEmpty ? this : EmptyWithComparers(_comparers);
        }

        /// <summary>
        /// Gets the number of elements in this collection.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return this.Count == 0; }
        }

        /// <summary>
        /// Gets the key comparer.
        /// </summary>
        public IEqualityComparer<TKey> KeyComparer
        {
            get { return _comparers.KeyComparer; }
        }

        /// <summary>
        /// Gets the value comparer used to determine whether values are equal.
        /// </summary>
        public IEqualityComparer<TValue> ValueComparer
        {
            get { return _comparers.ValueComparer; }
        }

        /// <summary>
        /// Gets the keys in the map.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                foreach (var bucket in _root)
                {
                    foreach (var item in bucket.Value)
                    {
                        yield return item.Key;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the values in the map.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var bucket in _root)
                {
                    foreach (var item in bucket.Value)
                    {
                        yield return item.Value;
                    }
                }
            }
        }

        #endregion

        #region IImmutableDictionary<TKey,TValue> Properties

        /// <summary>
        /// Gets the empty instance.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
        {
            return this.Clear();
        }

        #endregion

        #region IDictionary<TKey, TValue> Properties

        /// <summary>
        /// Gets the keys.
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return new KeysCollectionAccessor<TKey, TValue>(this); }
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return new ValuesCollectionAccessor<TKey, TValue>(this); }
        }

        #endregion

        /// <summary>
        /// Gets a data structure that captures the current state of this map, as an input into a query or mutating function.
        /// </summary>
        private MutationInput Origin
        {
            get { return new MutationInput(this); }
        }

        /// <summary>
        /// Gets the <typeparamref name="TValue"/> with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                Requires.NotNullAllowStructs(key, nameof(key));

                TValue value;
                if (this.TryGetValue(key, out value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// Gets or sets the <typeparamref name="TValue"/> with the specified key.
        /// </summary>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get { return this[key]; }
            set { throw new NotSupportedException(); }
        }

        #region ICollection<KeyValuePair<TKey, TValue>> Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Creates a collection with the same contents as this collection that
        /// can be efficiently mutated across multiple operations using standard
        /// mutable interfaces.
        /// </summary>
        /// <remarks>
        /// This is an O(1) operation and results in only a single (small) memory allocation.
        /// The mutable collection that is returned is *not* thread-safe.
        /// </remarks>
        [Pure]
        public Builder ToBuilder()
        {
            // We must not cache the instance created here and return it to various callers.
            // Those who request a mutable collection must get references to the collection
            // that version independently of each other.
            return new Builder(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            var result = Add(key, value, KeyCollisionBehavior.ThrowIfValueDifferent, this.Origin);
            return result.Finalize(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            Requires.NotNull(pairs, nameof(pairs));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            return this.AddRange(pairs, false);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);
            Contract.Ensures(!Contract.Result<ImmutableDictionary<TKey, TValue>>().IsEmpty);

            var result = Add(key, value, KeyCollisionBehavior.SetValue, this.Origin);
            return result.Finalize(this);
        }

        /// <summary>
        /// Applies a given set of key=value pairs to an immutable dictionary, replacing any conflicting keys in the resulting dictionary.
        /// </summary>
        /// <param name="items">The key=value pairs to set on the map.  Any keys that conflict with existing keys will overwrite the previous values.</param>
        /// <returns>An immutable dictionary.</returns>
        [Pure]
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public ImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            var result = AddRange(items, this.Origin, KeyCollisionBehavior.SetValue);
            return result.Finalize(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> Remove(TKey key)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            var result = Remove(key, this.Origin);
            return result.Finalize(this);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            Requires.NotNull(keys, nameof(keys));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            int count = _count;
            var root = _root;
            foreach (var key in keys)
            {
                int hashCode = this.KeyComparer.GetHashCode(key);
                HashBucket bucket;
                if (root.TryGetValue(hashCode, out bucket))
                {
                    OperationResult result;
                    var newBucket = bucket.Remove(key, _comparers.KeyOnlyComparer, out result);
                    root = UpdateRoot(root, hashCode, newBucket, _comparers.HashBucketEqualityComparer);
                    if (result == OperationResult.SizeChanged)
                    {
                        count--;
                    }
                }
            }

            return this.Wrap(root, count);
        }

        /// <summary>
        /// Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            return ContainsKey(key, this.Origin);
        }

        /// <summary>
        /// Determines whether [contains] [the specified key value pair].
        /// </summary>
        /// <param name="pair">The key value pair.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified key value pair]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return Contains(pair, this.Origin);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            return TryGetValue(key, this.Origin, out value);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            Requires.NotNullAllowStructs(equalKey, nameof(equalKey));
            return TryGetKey(equalKey, this.Origin, out actualKey);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> WithComparers(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            if (keyComparer == null)
            {
                keyComparer = EqualityComparer<TKey>.Default;
            }

            if (valueComparer == null)
            {
                valueComparer = EqualityComparer<TValue>.Default;
            }

            if (this.KeyComparer == keyComparer)
            {
                if (this.ValueComparer == valueComparer)
                {
                    return this;
                }
                else
                {
                    // When the key comparer is the same but the value comparer is different, we don't need a whole new tree
                    // because the structure of the tree does not depend on the value comparer.
                    // We just need a new root node to store the new value comparer.
                    var comparers = _comparers.WithValueComparer(valueComparer);
                    return new ImmutableDictionary<TKey, TValue>(_root, comparers, _count);
                }
            }
            else
            {
                var comparers = Comparers.Get(keyComparer, valueComparer);
                var set = new ImmutableDictionary<TKey, TValue>(comparers);
                set = set.AddRange(this, avoidToHashMap: true);
                return set;
            }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableDictionary<TKey, TValue> WithComparers(IEqualityComparer<TKey> keyComparer)
        {
            return this.WithComparers(keyComparer, _comparers.ValueComparer);
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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_root);
        }

        #endregion

        #region IImmutableDictionary<TKey,TValue> Methods

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            return this.Add(key, value);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
        {
            return this.SetItem(key, value);
        }

        /// <summary>
        /// Applies a given set of key=value pairs to an immutable dictionary, replacing any conflicting keys in the resulting dictionary.
        /// </summary>
        /// <param name="items">The key=value pairs to set on the map.  Any keys that conflict with existing keys will overwrite the previous values.</param>
        /// <returns>An immutable dictionary.</returns>
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            return this.SetItems(items);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return this.AddRange(pairs);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
        {
            return this.RemoveRange(keys);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key)
        {
            return this.Remove(key);
        }

        #endregion

        #region IDictionary<TKey, TValue> Methods

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An element with the same key already exists in the <see cref="IDictionary{TKey, TValue}"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The <see cref="IDictionary{TKey, TValue}"/> is read-only.
        /// </exception>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="IDictionary{TKey, TValue}"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The <see cref="IDictionary{TKey, TValue}"/> is read-only.
        /// </exception>
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>> Methods

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Requires.NotNull(array, nameof(array));
            Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
            Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        #endregion

        #region IDictionary Properties

        /// <summary>
        /// Gets a value indicating whether the <see cref="IDictionary"/> object has a fixed size.
        /// </summary>
        /// <returns>true if the <see cref="IDictionary"/> object has a fixed size; otherwise, false.</returns>
        bool IDictionary.IsFixedSize
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ICollection{T}"/> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="ICollection{T}"/> is read-only; otherwise, false.
        ///   </returns>
        bool IDictionary.IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> containing the keys of the <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}"/> containing the keys of the object that implements <see cref="IDictionary{TKey, TValue}"/>.
        ///   </returns>
        ICollection IDictionary.Keys
        {
            get { return new KeysCollectionAccessor<TKey, TValue>(this); }
        }

        /// <summary>
        /// Gets an <see cref="ICollection{T}"/> containing the values in the <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="ICollection{T}"/> containing the values in the object that implements <see cref="IDictionary{TKey, TValue}"/>.
        ///   </returns>
        ICollection IDictionary.Values
        {
            get { return new ValuesCollectionAccessor<TKey, TValue>(this); }
        }

        #endregion

        /// <summary>
        /// Gets the root node (for testing purposes).
        /// </summary>
        internal SortedInt32KeyNode<HashBucket> Root
        {
            get { return _root; }
        }

        #region IDictionary Methods

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="IDictionary"/> object.
        /// </summary>
        /// <param name="key">The <see cref="object"/> to use as the key of the element to add.</param>
        /// <param name="value">The <see cref="object"/> to use as the value of the element to add.</param>
        void IDictionary.Add(object key, object value)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        object IDictionary.this[object key]
        {
            get { return this[(TKey)key]; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        void IDictionary.Clear()
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection Methods

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

        #region ICollection Properties

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="ICollection"/>.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <returns>true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized
        {
            get
            {
                // This is immutable, so it is always thread-safe.
                return true;
            }
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey, TValue>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.IsEmpty ?
                Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator() :
                this.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets an empty collection with the specified comparers.
        /// </summary>
        /// <param name="comparers">The comparers.</param>
        /// <returns>The empty dictionary.</returns>
        [Pure]
        private static ImmutableDictionary<TKey, TValue> EmptyWithComparers(Comparers comparers)
        {
            Requires.NotNull(comparers, nameof(comparers));

            return Empty._comparers == comparers
                ? Empty
                : new ImmutableDictionary<TKey, TValue>(comparers);
        }

        /// <summary>
        /// Attempts to discover an <see cref="ImmutableDictionary{TKey, TValue}"/> instance beneath some enumerable sequence
        /// if one exists.
        /// </summary>
        /// <param name="sequence">The sequence that may have come from an immutable map.</param>
        /// <param name="other">Receives the concrete <see cref="ImmutableDictionary{TKey, TValue}"/> typed value if one can be found.</param>
        /// <returns><c>true</c> if the cast was successful; <c>false</c> otherwise.</returns>
        private static bool TryCastToImmutableMap(IEnumerable<KeyValuePair<TKey, TValue>> sequence, out ImmutableDictionary<TKey, TValue> other)
        {
            other = sequence as ImmutableDictionary<TKey, TValue>;
            if (other != null)
            {
                return true;
            }

            var builder = sequence as Builder;
            if (builder != null)
            {
                other = builder.ToImmutable();
                return true;
            }

            return false;
        }

        #region Static query and manipulator methods

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static bool ContainsKey(TKey key, MutationInput origin)
        {
            int hashCode = origin.KeyComparer.GetHashCode(key);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                TValue value;
                return bucket.TryGetValue(key, origin.KeyOnlyComparer, out value);
            }

            return false;
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static bool Contains(KeyValuePair<TKey, TValue> keyValuePair, MutationInput origin)
        {
            int hashCode = origin.KeyComparer.GetHashCode(keyValuePair.Key);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                TValue value;
                return bucket.TryGetValue(keyValuePair.Key, origin.KeyOnlyComparer, out value)
                    && origin.ValueComparer.Equals(value, keyValuePair.Value);
            }

            return false;
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static bool TryGetValue(TKey key, MutationInput origin, out TValue value)
        {
            int hashCode = origin.KeyComparer.GetHashCode(key);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                return bucket.TryGetValue(key, origin.KeyOnlyComparer, out value);
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static bool TryGetKey(TKey equalKey, MutationInput origin, out TKey actualKey)
        {
            int hashCode = origin.KeyComparer.GetHashCode(equalKey);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                return bucket.TryGetKey(equalKey, origin.KeyOnlyComparer, out actualKey);
            }

            actualKey = equalKey;
            return false;
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static MutationResult Add(TKey key, TValue value, KeyCollisionBehavior behavior, MutationInput origin)
        {
            Requires.NotNullAllowStructs(key, nameof(key));

            OperationResult result;
            int hashCode = origin.KeyComparer.GetHashCode(key);
            HashBucket bucket = origin.Root.GetValueOrDefault(hashCode);
            var newBucket = bucket.Add(key, value, origin.KeyOnlyComparer, origin.ValueComparer, behavior, out result);
            if (result == OperationResult.NoChangeRequired)
            {
                return new MutationResult(origin);
            }

            var newRoot = UpdateRoot(origin.Root, hashCode, newBucket, origin.HashBucketComparer);
            return new MutationResult(newRoot, result == OperationResult.SizeChanged ? +1 : 0);
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static MutationResult AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items, MutationInput origin, KeyCollisionBehavior collisionBehavior = KeyCollisionBehavior.ThrowIfValueDifferent)
        {
            Requires.NotNull(items, nameof(items));

            int countAdjustment = 0;
            var newRoot = origin.Root;
            foreach (var pair in items)
            {
                int hashCode = origin.KeyComparer.GetHashCode(pair.Key);
                HashBucket bucket = newRoot.GetValueOrDefault(hashCode);
                OperationResult result;
                var newBucket = bucket.Add(pair.Key, pair.Value, origin.KeyOnlyComparer, origin.ValueComparer, collisionBehavior, out result);
                newRoot = UpdateRoot(newRoot, hashCode, newBucket, origin.HashBucketComparer);
                if (result == OperationResult.SizeChanged)
                {
                    countAdjustment++;
                }
            }

            return new MutationResult(newRoot, countAdjustment);
        }

        /// <summary>
        /// Performs the operation on a given data structure.
        /// </summary>
        private static MutationResult Remove(TKey key, MutationInput origin)
        {
            int hashCode = origin.KeyComparer.GetHashCode(key);
            HashBucket bucket;
            if (origin.Root.TryGetValue(hashCode, out bucket))
            {
                OperationResult result;
                var newRoot = UpdateRoot(origin.Root, hashCode, bucket.Remove(key, origin.KeyOnlyComparer, out result), origin.HashBucketComparer);
                return new MutationResult(newRoot, result == OperationResult.SizeChanged ? -1 : 0);
            }

            return new MutationResult(origin);
        }

        /// <summary>
        /// Performs the set operation on a given data structure.
        /// </summary>
        private static SortedInt32KeyNode<HashBucket> UpdateRoot(SortedInt32KeyNode<HashBucket> root, int hashCode, HashBucket newBucket, IEqualityComparer<HashBucket> hashBucketComparer)
        {
            bool mutated;
            if (newBucket.IsEmpty)
            {
                return root.Remove(hashCode, out mutated);
            }
            else
            {
                bool replacedExistingValue;
                return root.SetItem(hashCode, newBucket, hashBucketComparer, out replacedExistingValue, out mutated);
            }
        }

        #endregion

        /// <summary>
        /// Wraps the specified data structure with an immutable collection wrapper.
        /// </summary>
        /// <param name="root">The root of the data structure.</param>
        /// <param name="comparers">The comparers.</param>
        /// <param name="count">The number of elements in the data structure.</param>
        /// <returns>
        /// The immutable collection.
        /// </returns>
        private static ImmutableDictionary<TKey, TValue> Wrap(SortedInt32KeyNode<HashBucket> root, Comparers comparers, int count)
        {
            Requires.NotNull(root, nameof(root));
            Requires.NotNull(comparers, nameof(comparers));
            Requires.Range(count >= 0, nameof(count));
            return new ImmutableDictionary<TKey, TValue>(root, comparers, count);
        }

        /// <summary>
        /// Wraps the specified data structure with an immutable collection wrapper.
        /// </summary>
        /// <param name="root">The root of the data structure.</param>
        /// <param name="adjustedCountIfDifferentRoot">The adjusted count if the root has changed.</param>
        /// <returns>The immutable collection.</returns>
        private ImmutableDictionary<TKey, TValue> Wrap(SortedInt32KeyNode<HashBucket> root, int adjustedCountIfDifferentRoot)
        {
            if (root == null)
            {
                return this.Clear();
            }

            if (_root != root)
            {
                return root.IsEmpty ? this.Clear() : new ImmutableDictionary<TKey, TValue>(root, _comparers, adjustedCountIfDifferentRoot);
            }

            return this;
        }

        /// <summary>
        /// Bulk adds entries to the map.
        /// </summary>
        /// <param name="pairs">The entries to add.</param>
        /// <param name="avoidToHashMap"><c>true</c> when being called from <see cref="WithComparers(IEqualityComparer{TKey}, IEqualityComparer{TValue})"/> to avoid <see cref="T:System.StackOverflowException"/>.</param>
        [Pure]
        private ImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs, bool avoidToHashMap)
        {
            Requires.NotNull(pairs, nameof(pairs));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            // Some optimizations may apply if we're an empty list.
            if (this.IsEmpty && !avoidToHashMap)
            {
                // If the items being added actually come from an ImmutableDictionary<TKey, TValue>
                // then there is no value in reconstructing it.
                ImmutableDictionary<TKey, TValue> other;
                if (TryCastToImmutableMap(pairs, out other))
                {
                    return other.WithComparers(this.KeyComparer, this.ValueComparer);
                }
            }

            var result = AddRange(pairs, this.Origin);
            return result.Finalize(this);
        }
    }
}
