// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable sorted dictionary implementation.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(ImmutableSortedDictionaryDebuggerProxy<,>))]
    public sealed partial class ImmutableSortedDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, ISortKeyCollection<TKey>, IDictionary<TKey, TValue>, IDictionary
    {
        /// <summary>
        /// An empty sorted dictionary with default sort and equality comparers.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ImmutableSortedDictionary<TKey, TValue> Empty = new ImmutableSortedDictionary<TKey, TValue>();

        /// <summary>
        /// The root node of the AVL tree that stores this map.
        /// </summary>
        private readonly Node _root;

        /// <summary>
        /// The number of elements in the set.
        /// </summary>
        private readonly int _count;

        /// <summary>
        /// The comparer used to sort keys in this map.
        /// </summary>
        private readonly IComparer<TKey> _keyComparer;

        /// <summary>
        /// The comparer used to detect equivalent values in this map.
        /// </summary>
        private readonly IEqualityComparer<TValue> _valueComparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        internal ImmutableSortedDictionary(IComparer<TKey> keyComparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            _keyComparer = keyComparer ?? Comparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            _root = Node.EmptyNode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="root">The root of the tree containing the contents of the map.</param>
        /// <param name="count">The number of elements in this map.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        private ImmutableSortedDictionary(Node root, int count, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            Requires.NotNull(root, nameof(root));
            Requires.Range(count >= 0, nameof(count));
            Requires.NotNull(keyComparer, nameof(keyComparer));
            Requires.NotNull(valueComparer, nameof(valueComparer));

            root.Freeze();
            _root = root;
            _count = count;
            _keyComparer = keyComparer;
            _valueComparer = valueComparer;
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public ImmutableSortedDictionary<TKey, TValue> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>().IsEmpty);
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>().KeyComparer == ((ISortKeyCollection<TKey>)this).KeyComparer);
            return _root.IsEmpty ? this : Empty.WithComparers(_keyComparer, _valueComparer);
        }

        #region IImmutableMap<TKey, TValue> Properties

        /// <summary>
        /// Gets the value comparer used to determine whether values are equal.
        /// </summary>
        public IEqualityComparer<TValue> ValueComparer
        {
            get { return _valueComparer; }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool IsEmpty
        {
            get { return _root.IsEmpty; }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get { return _root.Keys; }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get { return _root.Values; }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
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

        #region ICollection<KeyValuePair<TKey, TValue>> Properties

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return true; }
        }

        #endregion

        #region ISortKeyCollection<TKey> Properties

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public IComparer<TKey> KeyComparer
        {
            get { return _keyComparer; }
        }

        #endregion

        /// <summary>
        /// Gets the root node (for testing purposes).
        /// </summary>
        internal Node Root
        {
            get { return _root; }
        }

        #region IImmutableMap<TKey, TValue> Indexers

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

        #endregion

        /// <summary>
        /// Gets or sets the <typeparamref name="TValue"/> with the specified key.
        /// </summary>
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get { return this[key]; }
            set { throw new NotSupportedException(); }
        }

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
        public ImmutableSortedDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);
            bool mutated;
            var result = _root.Add(key, value, _keyComparer, _valueComparer, out mutated);
            return this.Wrap(result, _count + 1);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);
            Contract.Ensures(!Contract.Result<ImmutableSortedDictionary<TKey, TValue>>().IsEmpty);
            bool replacedExistingValue, mutated;
            var result = _root.SetItem(key, value, _keyComparer, _valueComparer, out replacedExistingValue, out mutated);
            return this.Wrap(result, replacedExistingValue ? _count : _count + 1);
        }

        /// <summary>
        /// Applies a given set of key=value pairs to an immutable dictionary, replacing any conflicting keys in the resulting dictionary.
        /// </summary>
        /// <param name="items">The key=value pairs to set on the map.  Any keys that conflict with existing keys will overwrite the previous values.</param>
        /// <returns>An immutable dictionary.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableDictionary<TKey, TValue>>() != null);

            return this.AddRange(items, overwriteOnCollision: true, avoidToSortedMap: false);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);

            return this.AddRange(items, overwriteOnCollision: false, avoidToSortedMap: false);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> Remove(TKey value)
        {
            Requires.NotNullAllowStructs(value, nameof(value));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);
            bool mutated;
            var result = _root.Remove(value, _keyComparer, out mutated);
            return this.Wrap(result, _count - 1);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            Requires.NotNull(keys, nameof(keys));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);

            var result = _root;
            int count = _count;
            foreach (TKey key in keys)
            {
                bool mutated;
                var newResult = result.Remove(key, _keyComparer, out mutated);
                if (mutated)
                {
                    result = newResult;
                    count--;
                }
            }

            return this.Wrap(result, count);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> WithComparers(IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>().IsEmpty == this.IsEmpty);
            if (keyComparer == null)
            {
                keyComparer = Comparer<TKey>.Default;
            }

            if (valueComparer == null)
            {
                valueComparer = EqualityComparer<TValue>.Default;
            }

            if (keyComparer == _keyComparer)
            {
                if (valueComparer == _valueComparer)
                {
                    return this;
                }
                else
                {
                    // When the key comparer is the same but the value comparer is different, we don't need a whole new tree
                    // because the structure of the tree does not depend on the value comparer.
                    // We just need a new root node to store the new value comparer.
                    return new ImmutableSortedDictionary<TKey, TValue>(_root, _count, _keyComparer, valueComparer);
                }
            }
            else
            {
                // A new key comparer means the whole tree structure could change.  We must build a new one.
                var result = new ImmutableSortedDictionary<TKey, TValue>(Node.EmptyNode, 0, keyComparer, valueComparer);
                result = result.AddRange(this, overwriteOnCollision: false, avoidToSortedMap: true);
                return result;
            }
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> WithComparers(IComparer<TKey> keyComparer)
        {
            return this.WithComparers(keyComparer, _valueComparer);
        }

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

        #endregion

        #region IImmutableDictionary<TKey, TValue> Methods

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            return this.Add(key, value);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
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
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return this.AddRange(pairs);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
        {
            return this.RemoveRange(keys);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [ExcludeFromCodeCoverage]
        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key)
        {
            return this.Remove(key);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            return _root.ContainsKey(key, _keyComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return _root.Contains(pair, _keyComparer, _valueComparer);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value)
        {
            Requires.NotNullAllowStructs(key, nameof(key));
            return _root.TryGetValue(key, _keyComparer, out value);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            Requires.NotNullAllowStructs(equalKey, nameof(equalKey));
            return _root.TryGetKey(equalKey, _keyComparer, out actualKey);
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
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        void ICollection.CopyTo(Array array, int index)
        {
            _root.CopyTo(array, index, this.Count);
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
        [ExcludeFromCodeCoverage]
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.GetEnumerator();
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
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return _root.GetEnumerator();
        }

        /// <summary>
        /// Creates a new sorted set wrapper for a node tree.
        /// </summary>
        /// <param name="root">The root of the collection.</param>
        /// <param name="count">The number of elements in the map.</param>
        /// <param name="keyComparer">The key comparer to use for the map.</param>
        /// <param name="valueComparer">The value comparer to use for the map.</param>
        /// <returns>The immutable sorted set instance.</returns>
        [Pure]
        private static ImmutableSortedDictionary<TKey, TValue> Wrap(Node root, int count, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            return root.IsEmpty
                ? Empty.WithComparers(keyComparer, valueComparer)
                : new ImmutableSortedDictionary<TKey, TValue>(root, count, keyComparer, valueComparer);
        }

        /// <summary>
        /// Attempts to discover an <see cref="ImmutableSortedDictionary{TKey, TValue}"/> instance beneath some enumerable sequence
        /// if one exists.
        /// </summary>
        /// <param name="sequence">The sequence that may have come from an immutable map.</param>
        /// <param name="other">Receives the concrete <see cref="ImmutableSortedDictionary{TKey, TValue}"/> typed value if one can be found.</param>
        /// <returns><c>true</c> if the cast was successful; <c>false</c> otherwise.</returns>
        private static bool TryCastToImmutableMap(IEnumerable<KeyValuePair<TKey, TValue>> sequence, out ImmutableSortedDictionary<TKey, TValue> other)
        {
            other = sequence as ImmutableSortedDictionary<TKey, TValue>;
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

        /// <summary>
        /// Bulk adds entries to the map.
        /// </summary>
        /// <param name="items">The entries to add.</param>
        /// <param name="overwriteOnCollision"><c>true</c> to allow the <paramref name="items"/> sequence to include duplicate keys and let the last one win; <c>false</c> to throw on collisions.</param>
        /// <param name="avoidToSortedMap"><c>true</c> when being called from <see cref="WithComparers(IComparer{TKey}, IEqualityComparer{TValue})"/> to avoid <see cref="T:System.StackOverflowException"/>.</param>
        [Pure]
        private ImmutableSortedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision, bool avoidToSortedMap)
        {
            Requires.NotNull(items, nameof(items));
            Contract.Ensures(Contract.Result<ImmutableSortedDictionary<TKey, TValue>>() != null);

            // Some optimizations may apply if we're an empty set.
            if (this.IsEmpty && !avoidToSortedMap)
            {
                return this.FillFromEmpty(items, overwriteOnCollision);
            }

            // Let's not implement in terms of ImmutableSortedMap.Add so that we're
            // not unnecessarily generating a new wrapping map object for each item.
            var result = _root;
            var count = _count;
            foreach (var item in items)
            {
                bool mutated;
                bool replacedExistingValue = false;
                var newResult = overwriteOnCollision
                    ? result.SetItem(item.Key, item.Value, _keyComparer, _valueComparer, out replacedExistingValue, out mutated)
                    : result.Add(item.Key, item.Value, _keyComparer, _valueComparer, out mutated);
                if (mutated)
                {
                    result = newResult;
                    if (!replacedExistingValue)
                    {
                        count++;
                    }
                }
            }

            return this.Wrap(result, count);
        }

        /// <summary>
        /// Creates a wrapping collection type around a root node.
        /// </summary>
        /// <param name="root">The root node to wrap.</param>
        /// <param name="adjustedCountIfDifferentRoot">The number of elements in the new tree, assuming it's different from the current tree.</param>
        /// <returns>A wrapping collection type for the new tree.</returns>
        [Pure]
        private ImmutableSortedDictionary<TKey, TValue> Wrap(Node root, int adjustedCountIfDifferentRoot)
        {
            if (_root != root)
            {
                return root.IsEmpty ? this.Clear() : new ImmutableSortedDictionary<TKey, TValue>(root, adjustedCountIfDifferentRoot, _keyComparer, _valueComparer);
            }
            else
            {
                return this;
            }
        }

        /// <summary>
        /// Efficiently creates a new collection based on the contents of some sequence.
        /// </summary>
        [Pure]
        private ImmutableSortedDictionary<TKey, TValue> FillFromEmpty(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision)
        {
            Debug.Assert(this.IsEmpty);
            Requires.NotNull(items, nameof(items));

            // If the items being added actually come from an ImmutableSortedSet<T>,
            // and the sort order is equivalent, then there is no value in reconstructing it.
            ImmutableSortedDictionary<TKey, TValue> other;
            if (TryCastToImmutableMap(items, out other))
            {
                return other.WithComparers(this.KeyComparer, this.ValueComparer);
            }

            var itemsAsDictionary = items as IDictionary<TKey, TValue>;
            SortedDictionary<TKey, TValue> dictionary;
            if (itemsAsDictionary != null)
            {
                dictionary = new SortedDictionary<TKey, TValue>(itemsAsDictionary, this.KeyComparer);
            }
            else
            {
                dictionary = new SortedDictionary<TKey, TValue>(this.KeyComparer);
                foreach (var item in items)
                {
                    if (overwriteOnCollision)
                    {
                        dictionary[item.Key] = item.Value;
                    }
                    else
                    {
                        TValue value;
                        if (dictionary.TryGetValue(item.Key, out value))
                        {
                            if (!_valueComparer.Equals(value, item.Value))
                            {
                                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.DuplicateKey, item.Key));
                            }
                        }
                        else
                        {
                            dictionary.Add(item.Key, item.Value);
                        }
                    }
                }
            }

            if (dictionary.Count == 0)
            {
                return this;
            }

            Node root = Node.NodeTreeFromSortedDictionary(dictionary);
            return new ImmutableSortedDictionary<TKey, TValue>(root, dictionary.Count, this.KeyComparer, this.ValueComparer);
        }

        /// <summary>
        /// Enumerates the contents of a binary tree.
        /// </summary>
        /// <remarks>
        /// This struct can and should be kept in exact sync with the other binary tree enumerators: 
        /// <see cref="ImmutableList{T}.Enumerator"/>, <see cref="ImmutableSortedDictionary{TKey, TValue}.Enumerator"/>, and <see cref="ImmutableSortedSet{T}.Enumerator"/>.
        /// 
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable 
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, ISecurePooledObjectUser
        {
            /// <summary>
            /// The resource pool of reusable mutable stacks for purposes of enumeration.
            /// </summary>
            /// <remarks>
            /// We utilize this resource pool to make "allocation free" enumeration achievable.
            /// </remarks>
            private static readonly SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator> s_enumeratingStacks =
                new SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>();

            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder _builder;

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly int _poolUserId;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private Node _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            private SecurePooledObject<Stack<RefAsValueType<Node>>> _stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private Node _current;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int _enumeratingBuilderVersion;

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            /// <param name="builder">The builder, if applicable.</param>
            internal Enumerator(Node root, Builder builder = null)
            {
                Requires.NotNull(root, nameof(root));

                _root = root;
                _builder = builder;
                _current = null;
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (!_root.IsEmpty)
                {
                    if (!s_enumeratingStacks.TryTake(this, out _stack))
                    {
                        _stack = s_enumeratingStacks.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
                    }

                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_current != null)
                    {
                        return _current.Value;
                    }

                    throw new InvalidOperationException();
                }
            }

            /// <inheritdoc/>
            int ISecurePooledObjectUser.PoolUserId
            {
                get { return _poolUserId; }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Disposes of this enumerator and returns the stack reference to the resource pool.
            /// </summary>
            public void Dispose()
            {
                _root = null;
                _current = null;
                Stack<RefAsValueType<Node>> stack;
                if (_stack != null && _stack.TryUse(ref this, out stack))
                {
                    stack.ClearFastWhenEmpty();
                    s_enumeratingStacks.TryAdd(this, _stack);
                }

                _stack = null;
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                this.ThrowIfChanged();

                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    if (stack.Count > 0)
                    {
                        Node n = stack.Pop().Value;
                        _current = n;
                        this.PushLeft(n.Right);
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _current = null;
                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    stack.ClearFastWhenEmpty();
                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
            /// </summary>
            internal void ThrowIfDisposed()
            {
                // Since this is a struct, copies might not have been marked as disposed.
                // But the stack we share across those copies would know.
                // This trick only works when we have a non-null stack.
                // For enumerators of empty collections, there isn't any natural
                // way to know when a copy of the struct has been disposed of.

                if (_root == null || (_stack != null && !_stack.IsOwned(ref this)))
                {
                    Requires.FailObjectDisposed(this);
                }
            }

            /// <summary>
            /// Throws an exception if the underlying builder's contents have been changed since enumeration started.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the collection has changed.</exception>
            private void ThrowIfChanged()
            {
                if (_builder != null && _builder.Version != _enumeratingBuilderVersion)
                {
                    throw new InvalidOperationException(SR.CollectionModifiedDuringEnumeration);
                }
            }

            /// <summary>
            /// Pushes this node and all its Left descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushLeft(Node node)
            {
                Requires.NotNull(node, nameof(node));
                var stack = _stack.Use(ref this);
                while (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<Node>(node));
                    node = node.Left;
                }
            }
        }

        /// <summary>
        /// A node in the AVL tree storing this map.
        /// </summary>
        [DebuggerDisplay("{_key} = {_value}")]
        internal sealed class Node : IBinaryTree<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
        {
            /// <summary>
            /// The default empty node.
            /// </summary>
            internal static readonly Node EmptyNode = new Node();

            /// <summary>
            /// The key associated with this node.
            /// </summary>
            private readonly TKey _key;

            /// <summary>
            /// The value associated with this node.
            /// </summary>
            /// <remarks>
            /// Sadly this field could be readonly but doing so breaks serialization due to bug: 
            /// http://connect.microsoft.com/VisualStudio/feedback/details/312970/weird-argumentexception-when-deserializing-field-in-typedreferences-cannot-be-static-or-init-only
            /// </remarks>
            private TValue _value;

            /// <summary>
            /// A value indicating whether this node has been frozen (made immutable).
            /// </summary>
            /// <remarks>
            /// Nodes must be frozen before ever being observed by a wrapping collection type
            /// to protect collections from further mutations.
            /// </remarks>
            private bool _frozen;

            /// <summary>
            /// The depth of the tree beneath this node.
            /// </summary>
            private byte _height; // AVL tree max height <= ~1.44 * log2(maxNodes + 2)

            /// <summary>
            /// The left tree.
            /// </summary>
            private Node _left;

            /// <summary>
            /// The right tree.
            /// </summary>
            private Node _right;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}.Node"/> class
            /// that is pre-frozen.
            /// </summary>
            private Node()
            {
                Contract.Ensures(this.IsEmpty);
                _frozen = true; // the empty node is *always* frozen.
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableSortedDictionary{TKey, TValue}.Node"/> class
            /// that is not yet frozen.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="left">The left.</param>
            /// <param name="right">The right.</param>
            /// <param name="frozen">Whether this node is prefrozen.</param>
            private Node(TKey key, TValue value, Node left, Node right, bool frozen = false)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(left, nameof(left));
                Requires.NotNull(right, nameof(right));
                Debug.Assert(!frozen || (left._frozen && right._frozen));
                Contract.Ensures(!this.IsEmpty);
                Contract.Ensures(_key != null);
                Contract.Ensures(_left == left);
                Contract.Ensures(_right == right);

                _key = key;
                _value = value;
                _left = left;
                _right = right;
                _height = checked((byte)(1 + Math.Max(left._height, right._height)));
                _frozen = frozen;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmpty
            {
                get
                {
                    Contract.Ensures((_left != null && _right != null) || Contract.Result<bool>());
                    return _left == null;
                }
            }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree<KeyValuePair<TKey, TValue>> IBinaryTree<KeyValuePair<TKey, TValue>>.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree<KeyValuePair<TKey, TValue>> IBinaryTree<KeyValuePair<TKey, TValue>>.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the height of the tree beneath this node.
            /// </summary>
            public int Height { get { return _height; } }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            public Node Left { get { return _left; } }

            /// <summary>
            /// Gets the left branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Left
            {
                get { return _left; }
            }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            public Node Right { get { return _right; } }

            /// <summary>
            /// Gets the right branch of this node.
            /// </summary>
            IBinaryTree IBinaryTree.Right
            {
                get { return _right; }
            }

            /// <summary>
            /// Gets the value represented by the current node.
            /// </summary>
            public KeyValuePair<TKey, TValue> Value
            {
                get { return new KeyValuePair<TKey, TValue>(_key, _value); }
            }

            /// <summary>
            /// Gets the number of elements contained by this node and below.
            /// </summary>
            int IBinaryTree.Count
            {
                get { throw new NotSupportedException(); }
            }

            /// <summary>
            /// Gets the keys.
            /// </summary>
            internal IEnumerable<TKey> Keys
            {
                get { return this.Select(p => p.Key); }
            }

            /// <summary>
            /// Gets the values.
            /// </summary>
            internal IEnumerable<TValue> Values
            {
                get { return this.Select(p => p.Value); }
            }

            #region IEnumerable<TKey> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

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
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <param name="builder">The builder, if applicable.</param>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            internal Enumerator GetEnumerator(Builder builder)
            {
                return new Enumerator(this, builder);
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            internal void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex, int dictionarySize)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + dictionarySize, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array[arrayIndex++] = item;
                }
            }

            /// <summary>
            /// See <see cref="IDictionary{TKey, TValue}"/>
            /// </summary>
            internal void CopyTo(Array array, int arrayIndex, int dictionarySize)
            {
                Requires.NotNull(array, nameof(array));
                Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
                Requires.Range(array.Length >= arrayIndex + dictionarySize, nameof(arrayIndex));

                foreach (var item in this)
                {
                    array.SetValue(new DictionaryEntry(item.Key, item.Value), arrayIndex++);
                }
            }

            /// <summary>
            /// Creates a node tree from an existing (mutable) collection.
            /// </summary>
            /// <param name="dictionary">The collection.</param>
            /// <returns>The root of the node tree.</returns>
            [Pure]
            internal static Node NodeTreeFromSortedDictionary(SortedDictionary<TKey, TValue> dictionary)
            {
                Requires.NotNull(dictionary, nameof(dictionary));
                Contract.Ensures(Contract.Result<Node>() != null);

                var list = dictionary.AsOrderedCollection();
                return NodeTreeFromList(list, 0, list.Count);
            }

            /// <summary>
            /// Adds the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            internal Node Add(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                bool dummy;
                return this.SetOrAdd(key, value, keyComparer, valueComparer, false, out dummy, out mutated);
            }

            /// <summary>
            /// Adds the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            internal Node SetItem(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, out bool replacedExistingValue, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                return this.SetOrAdd(key, value, keyComparer, valueComparer, true, out replacedExistingValue, out mutated);
            }

            /// <summary>
            /// Removes the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            internal Node Remove(TKey key, IComparer<TKey> keyComparer, out bool mutated)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                return this.RemoveRecursive(key, keyComparer, out mutated);
            }

            /// <summary>
            /// Gets the value or default.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <returns>The value.</returns>
            [Pure]
            internal TValue GetValueOrDefault(TKey key, IComparer<TKey> keyComparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(key, keyComparer);
                return match.IsEmpty ? default(TValue) : match._value;
            }

            /// <summary>
            /// Tries to get the value.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="value">The value.</param>
            /// <returns>True if the key was found.</returns>
            [Pure]
            internal bool TryGetValue(TKey key, IComparer<TKey> keyComparer, out TValue value)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(key, keyComparer);
                if (match.IsEmpty)
                {
                    value = default(TValue);
                    return false;
                }
                else
                {
                    value = match._value;
                    return true;
                }
            }

            /// <summary>
            /// Searches the dictionary for a given key and returns the equal key it finds, if any.
            /// </summary>
            /// <param name="equalKey">The key to search for.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="actualKey">The key from the dictionary that the search found, or <paramref name="equalKey"/> if the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            /// <remarks>
            /// This can be useful when you want to reuse a previously stored reference instead of
            /// a newly constructed one (so that more sharing of references can occur) or to look up
            /// the canonical value, or a value that has more complete data than the value you currently have,
            /// although their comparer functions indicate they are equal.
            /// </remarks>
            [Pure]
            internal bool TryGetKey(TKey equalKey, IComparer<TKey> keyComparer, out TKey actualKey)
            {
                Requires.NotNullAllowStructs(equalKey, nameof(equalKey));
                Requires.NotNull(keyComparer, nameof(keyComparer));

                var match = this.Search(equalKey, keyComparer);
                if (match.IsEmpty)
                {
                    actualKey = equalKey;
                    return false;
                }
                else
                {
                    actualKey = match._key;
                    return true;
                }
            }

            /// <summary>
            /// Determines whether the specified key contains key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <returns>
            /// <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            internal bool ContainsKey(TKey key, IComparer<TKey> keyComparer)
            {
                Requires.NotNullAllowStructs(key, nameof(key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                return !this.Search(key, keyComparer).IsEmpty;
            }

            /// <summary>
            /// Determines whether the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>
            /// contains an element with the specified value.
            /// </summary>
            /// <param name="value">
            /// The value to locate in the <see cref="ImmutableSortedDictionary{TKey, TValue}"/>.
            /// The value can be null for reference types.
            /// </param>
            /// <param name="valueComparer">The value comparer to use.</param>
            /// <returns>
            /// true if the <see cref="ImmutableSortedDictionary{TKey, TValue}"/> contains
            /// an element with the specified value; otherwise, false.
            /// </returns>
            [Pure]
            internal bool ContainsValue(TValue value, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNull(valueComparer, nameof(valueComparer));
                foreach (KeyValuePair<TKey, TValue> item in this)
                {
                    if (valueComparer.Equals(value, item.Value))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Determines whether [contains] [the specified pair].
            /// </summary>
            /// <param name="pair">The pair.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <returns>
            /// <c>true</c> if [contains] [the specified pair]; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            internal bool Contains(KeyValuePair<TKey, TValue> pair, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNullAllowStructs(pair.Key, nameof(pair.Key));
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                var matchingNode = this.Search(pair.Key, keyComparer);
                if (matchingNode.IsEmpty)
                {
                    return false;
                }

                return valueComparer.Equals(matchingNode._value, pair.Value);
            }

            /// <summary>
            /// Freezes this node and all descendant nodes so that any mutations require a new instance of the nodes.
            /// </summary>
            internal void Freeze(Action<KeyValuePair<TKey, TValue>> freezeAction = null)
            {
                // If this node is frozen, all its descendants must already be frozen.
                if (!_frozen)
                {
                    if (freezeAction != null)
                    {
                        freezeAction(new KeyValuePair<TKey, TValue>(_key, _value));
                    }

                    _left.Freeze(freezeAction);
                    _right.Freeze(freezeAction);
                    _frozen = true;
                }
            }

            #region Tree balancing methods

            /// <summary>
            /// AVL rotate left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateLeft(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                var right = tree._right;
                return right.Mutate(left: tree.Mutate(right: right._left));
            }

            /// <summary>
            /// AVL rotate right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node RotateRight(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                var left = tree._left;
                return left.Mutate(right: tree.Mutate(left: left._right));
            }

            /// <summary>
            /// AVL rotate double-left operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleLeft(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._right.IsEmpty)
                {
                    return tree;
                }

                Node rotatedRightChild = tree.Mutate(right: RotateRight(tree._right));
                return RotateLeft(rotatedRightChild);
            }

            /// <summary>
            /// AVL rotate double-right operation.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>The rotated tree.</returns>
            private static Node DoubleRight(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (tree._left.IsEmpty)
                {
                    return tree;
                }

                Node rotatedLeftChild = tree.Mutate(left: RotateLeft(tree._left));
                return RotateRight(rotatedLeftChild);
            }

            /// <summary>
            /// Returns a value indicating whether the tree is in balance.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>0 if the tree is in balance, a positive integer if the right side is heavy, or a negative integer if the left side is heavy.</returns>
            [Pure]
            private static int Balance(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);

                return tree._right._height - tree._left._height;
            }

            /// <summary>
            /// Determines whether the specified tree is right heavy.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>
            /// <c>true</c> if [is right heavy] [the specified tree]; otherwise, <c>false</c>.
            /// </returns>
            [Pure]
            private static bool IsRightHeavy(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) >= 2;
            }

            /// <summary>
            /// Determines whether the specified tree is left heavy.
            /// </summary>
            [Pure]
            private static bool IsLeftHeavy(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                return Balance(tree) <= -2;
            }

            /// <summary>
            /// Balances the specified tree.
            /// </summary>
            /// <param name="tree">The tree.</param>
            /// <returns>A balanced tree.</returns>
            [Pure]
            private static Node MakeBalanced(Node tree)
            {
                Requires.NotNull(tree, nameof(tree));
                Debug.Assert(!tree.IsEmpty);
                Contract.Ensures(Contract.Result<Node>() != null);

                if (IsRightHeavy(tree))
                {
                    return Balance(tree._right) < 0 ? DoubleLeft(tree) : RotateLeft(tree);
                }

                if (IsLeftHeavy(tree))
                {
                    return Balance(tree._left) > 0 ? DoubleRight(tree) : RotateRight(tree);
                }

                return tree;
            }

            #endregion

            /// <summary>
            /// Creates a node tree that contains the contents of a list.
            /// </summary>
            /// <param name="items">An indexable list with the contents that the new node tree should contain.</param>
            /// <param name="start">The starting index within <paramref name="items"/> that should be captured by the node tree.</param>
            /// <param name="length">The number of elements from <paramref name="items"/> that should be captured by the node tree.</param>
            /// <returns>The root of the created node tree.</returns>
            [Pure]
            private static Node NodeTreeFromList(IOrderedCollection<KeyValuePair<TKey, TValue>> items, int start, int length)
            {
                Requires.NotNull(items, nameof(items));
                Requires.Range(start >= 0, nameof(start));
                Requires.Range(length >= 0, nameof(length));
                Contract.Ensures(Contract.Result<Node>() != null);

                if (length == 0)
                {
                    return EmptyNode;
                }

                int rightCount = (length - 1) / 2;
                int leftCount = (length - 1) - rightCount;
                Node left = NodeTreeFromList(items, start, leftCount);
                Node right = NodeTreeFromList(items, start + leftCount + 1, rightCount);
                var item = items[start + leftCount];
                return new Node(item.Key, item.Value, left, right, true);
            }

            /// <summary>
            /// Adds the specified key. Callers are expected to have validated arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="overwriteExistingValue">if <c>true</c>, an existing key=value pair will be overwritten with the new one.</param>
            /// <param name="replacedExistingValue">Receives a value indicating whether an existing value was replaced.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            private Node SetOrAdd(TKey key, TValue value, IComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue, out bool replacedExistingValue, out bool mutated)
            {
                // Arg validation skipped in this private method because it's recursive and the tax
                // of revalidating arguments on each recursive call is significant.
                // All our callers are therefore required to have done input validation.
                replacedExistingValue = false;
                if (this.IsEmpty)
                {
                    mutated = true;
                    return new Node(key, value, this, this);
                }
                else
                {
                    Node result = this;
                    int compareResult = keyComparer.Compare(key, _key);
                    if (compareResult > 0)
                    {
                        var newRight = _right.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }
                    else if (compareResult < 0)
                    {
                        var newLeft = _left.SetOrAdd(key, value, keyComparer, valueComparer, overwriteExistingValue, out replacedExistingValue, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        if (valueComparer.Equals(_value, value))
                        {
                            mutated = false;
                            return this;
                        }
                        else if (overwriteExistingValue)
                        {
                            mutated = true;
                            replacedExistingValue = true;
                            result = new Node(key, value, _left, _right);
                        }
                        else
                        {
                            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, SR.DuplicateKey, key));
                        }
                    }

                    return mutated ? MakeBalanced(result) : result;
                }
            }

            /// <summary>
            /// Removes the specified key. Callers are expected to validate arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="mutated">Receives a value indicating whether this node tree has mutated because of this operation.</param>
            /// <returns>The new AVL tree.</returns>
            private Node RemoveRecursive(TKey key, IComparer<TKey> keyComparer, out bool mutated)
            {
                // Skip parameter validation because it's too expensive and pointless in recursive methods.
                if (this.IsEmpty)
                {
                    mutated = false;
                    return this;
                }
                else
                {
                    Node result = this;
                    int compare = keyComparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        // We have a match.
                        mutated = true;

                        // If this is a leaf, just remove it 
                        // by returning Empty.  If we have only one child,
                        // replace the node with the child.
                        if (_right.IsEmpty && _left.IsEmpty)
                        {
                            result = EmptyNode;
                        }
                        else if (_right.IsEmpty && !_left.IsEmpty)
                        {
                            result = _left;
                        }
                        else if (!_right.IsEmpty && _left.IsEmpty)
                        {
                            result = _right;
                        }
                        else
                        {
                            // We have two children. Remove the next-highest node and replace
                            // this node with it.
                            var successor = _right;
                            while (!successor._left.IsEmpty)
                            {
                                successor = successor._left;
                            }

                            bool dummyMutated;
                            var newRight = _right.Remove(successor._key, keyComparer, out dummyMutated);
                            result = successor.Mutate(left: _left, right: newRight);
                        }
                    }
                    else if (compare < 0)
                    {
                        var newLeft = _left.Remove(key, keyComparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(left: newLeft);
                        }
                    }
                    else
                    {
                        var newRight = _right.Remove(key, keyComparer, out mutated);
                        if (mutated)
                        {
                            result = this.Mutate(right: newRight);
                        }
                    }

                    return result.IsEmpty ? result : MakeBalanced(result);
                }
            }

            /// <summary>
            /// Creates a node mutation, either by mutating this node (if not yet frozen) or by creating a clone of this node
            /// with the described changes.
            /// </summary>
            /// <param name="left">The left branch of the mutated node.</param>
            /// <param name="right">The right branch of the mutated node.</param>
            /// <returns>The mutated (or created) node.</returns>
            private Node Mutate(Node left = null, Node right = null)
            {
                if (_frozen)
                {
                    return new Node(_key, _value, left ?? _left, right ?? _right);
                }
                else
                {
                    if (left != null)
                    {
                        _left = left;
                    }

                    if (right != null)
                    {
                        _right = right;
                    }

                    _height = checked((byte)(1 + Math.Max(_left._height, _right._height)));
                    return this;
                }
            }

            /// <summary>
            /// Searches the specified key. Callers are expected to validate arguments.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="keyComparer">The key comparer.</param>
            [Pure]
            private Node Search(TKey key, IComparer<TKey> keyComparer)
            {
                // Arg validation is too expensive for recursive methods.
                // Callers are expected to have validated parameters.
                if (this.IsEmpty)
                {
                    return this;
                }
                else
                {
                    int compare = keyComparer.Compare(key, _key);
                    if (compare == 0)
                    {
                        return this;
                    }
                    else if (compare > 0)
                    {
                        return _right.Search(key, keyComparer);
                    }
                    else
                    {
                        return _left.Search(key, keyComparer);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableSortedDictionaryDebuggerProxy<TKey, TValue>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableSortedDictionary<TKey, TValue> _map;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private KeyValuePair<TKey, TValue>[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableSortedDictionaryDebuggerProxy{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="map">The collection to display in the debugger</param>
        public ImmutableSortedDictionaryDebuggerProxy(ImmutableSortedDictionary<TKey, TValue> map)
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
