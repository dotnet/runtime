// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A thin wrapper around the Keys or Values enumerators so they look like a collection.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of value in the dictionary.</typeparam>
    /// <typeparam name="T">Either TKey or TValue.</typeparam>
    internal abstract class KeysOrValuesCollectionAccessor<TKey, TValue, T> : ICollection<T>, ICollection
    {
        /// <summary>
        /// The underlying wrapped dictionary.
        /// </summary>
        private readonly IImmutableDictionary<TKey, TValue> dictionary;

        /// <summary>
        /// The key or value enumerable that this instance wraps.
        /// </summary>
        private readonly IEnumerable<T> keysOrValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysOrValuesCollectionAccessor{TKey, TValue, T}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to base on.</param>
        /// <param name="keysOrValues">The keys or values enumeration to wrap as a collection.</param>
        protected KeysOrValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary, IEnumerable<T> keysOrValues)
        {
            Requires.NotNull(dictionary, "dictionary");
            Requires.NotNull(keysOrValues, "keysOrValues");

            this.dictionary = dictionary;
            this.keysOrValues = keysOrValues;
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
        public int Count
        {
            get { return this.dictionary.Count; }
        }

        /// <summary>
        /// Gets the wrapped dictionary.
        /// </summary>
        protected IImmutableDictionary<TKey, TValue> Dictionary
        {
            get { return this.dictionary; }
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public abstract bool Contains(T item);

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Requires.NotNull(array, "array");
            Requires.Range(arrayIndex >= 0, "arrayIndex");
            Requires.Range(array.Length >= arrayIndex + this.Count, "arrayIndex");

            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// See <see cref="IEnumerable&lt;T&gt;"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return this.keysOrValues.GetEnumerator();
        }

        /// <summary>
        /// See <see cref="System.Collections.IEnumerable"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

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

            if (Count == 0)
            {
                return;
            }

            int[] indices = new int[1]; // SetValue takes a params array; lifting out the implicit allocation from the loop
            foreach (T item in this)
            {
                indices[0] = arrayIndex++;
                array.SetValue(item, indices);
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).
        /// </summary>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, false.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</returns>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get { return this; }
        }
    }

    /// <summary>
    /// A lightweight collection view over and IEnumerable of keys.
    /// </summary>
    internal class KeysCollectionAccessor<TKey, TValue> : KeysOrValuesCollectionAccessor<TKey, TValue, TKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeysCollectionAccessor{TKey, TValue}"/> class.
        /// </summary>
        internal KeysCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Keys)
        {
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
        /// </summary>
        public override bool Contains(TKey item)
        {
            return this.Dictionary.ContainsKey(item);
        }
    }

    /// <summary>
    /// A lightweight collection view over and IEnumerable of values.
    /// </summary>
    internal class ValuesCollectionAccessor<TKey, TValue> : KeysOrValuesCollectionAccessor<TKey, TValue, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValuesCollectionAccessor{TKey, TValue}"/> class.
        /// </summary>
        internal ValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Values)
        {
        }

        /// <summary>
        /// See <see cref="ICollection&lt;T&gt;"/>
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
