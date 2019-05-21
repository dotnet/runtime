// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
    [DebuggerTypeProxy(typeof(ImmutableDictionaryDebuggerProxy<,>))]
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

                throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
            }
        }

#if !NETSTANDARD10
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

            return this.AddRange(items, overwriteOnCollision: false, avoidToSortedMap: false);
        }

        /// <summary>
        /// See the <see cref="IImmutableDictionary{TKey, TValue}"/> interface.
        /// </summary>
        [Pure]
        public ImmutableSortedDictionary<TKey, TValue> Remove(TKey value)
        {
            Requires.NotNullAllowStructs(value, nameof(value));
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
                                throw new ArgumentException(SR.Format(SR.DuplicateKey, item.Key));
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
    }
}
