// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A thin wrapper around the <see cref="IDictionary{TKey, TValue}.Keys"/> or <see cref="IDictionary{TKey, TValue}.Values"/> enumerators so they look like a collection.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of value in the dictionary.</typeparam>
    /// <typeparam name="T">Either TKey or TValue.</typeparam>
    internal abstract class KeysOrValuesCollectionAccessor<TKey, TValue, T> : ICollection<T>, ICollection
    {
        /// <summary>
        /// The underlying wrapped dictionary.
        /// </summary>
        private readonly IImmutableDictionary<TKey, TValue> _dictionary;

        /// <summary>
        /// The key or value enumerable that this instance wraps.
        /// </summary>
        private readonly IEnumerable<T> _keysOrValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysOrValuesCollectionAccessor{TKey, TValue, T}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to base on.</param>
        /// <param name="keysOrValues">The keys or values enumeration to wrap as a collection.</param>
        protected KeysOrValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary, IEnumerable<T> keysOrValues)
        {
            Requires.NotNull(dictionary, nameof(dictionary));
            Requires.NotNull(keysOrValues, nameof(keysOrValues));

            _dictionary = dictionary;
            _keysOrValues = keysOrValues;
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="ICollection{T}"/>.</returns>
        public int Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Gets the wrapped dictionary.
        /// </summary>
        protected IImmutableDictionary<TKey, TValue> Dictionary
        {
            get { return _dictionary; }
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public abstract bool Contains(T item);

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Requires.NotNull(array, nameof(array));
            Requires.Range(arrayIndex >= 0, nameof(arrayIndex));
            Requires.Range(array.Length >= arrayIndex + this.Count, nameof(arrayIndex));

            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="IEnumerable{T}"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _keysOrValues.GetEnumerator();
        }

        /// <summary>
        /// See <see cref="System.Collections.IEnumerable"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

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

            foreach (T item in this)
            {
                array.SetValue(item, arrayIndex++);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <returns>true if access to the <see cref="ICollection"/> is synchronized (thread safe); otherwise, false.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="ICollection"/>.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="ICollection"/>.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { return this; }
        }
    }

    /// <summary>
    /// A lightweight collection view over and IEnumerable of keys.
    /// </summary>
    internal sealed class KeysCollectionAccessor<TKey, TValue> : KeysOrValuesCollectionAccessor<TKey, TValue, TKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeysCollectionAccessor{TKey, TValue}"/> class.
        /// </summary>
        internal KeysCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Keys)
        {
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public override bool Contains(TKey item)
        {
            return this.Dictionary.ContainsKey(item);
        }
    }

    /// <summary>
    /// A lightweight collection view over and IEnumerable of values.
    /// </summary>
    internal sealed class ValuesCollectionAccessor<TKey, TValue> : KeysOrValuesCollectionAccessor<TKey, TValue, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValuesCollectionAccessor{TKey, TValue}"/> class.
        /// </summary>
        internal ValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Values)
        {
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public override bool Contains(TValue item)
        {
            var sortedDictionary = this.Dictionary as ImmutableSortedDictionary<TKey, TValue>;
            if (sortedDictionary != null)
            {
                return sortedDictionary.ContainsValue(item);
            }

            var dictionary = this.Dictionary as IImmutableDictionaryInternal<TKey, TValue>;
            if (dictionary != null)
            {
                return dictionary.ContainsValue(item);
            }

            throw new NotSupportedException();
        }
    }
}
