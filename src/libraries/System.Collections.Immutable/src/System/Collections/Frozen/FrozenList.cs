// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple frozen list of items.
    /// </summary>
    /// <typeparam name="T">The item's type.</typeparam>
    /// <remarks>
    /// This type is a slight improvement over the classic <see cref="List{T}"/>. It uses less memory
    /// and enumerates items a bit faster.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IReadOnlyCollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenList<T> : IReadOnlyList<T>, ICollection<T>
    {
        private readonly T[] _items;

        /// <summary>
        /// Gets an empty frozen list.
        /// </summary>
        public static FrozenList<T> Empty { get; } = Array.Empty<T>().ToFrozenList();

        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenList{T}"/> struct.
        /// </summary>
        /// <param name="items">The items to track in the list.</param>
        /// <remarks>
        /// Note that this takes a reference to the incoming array and does not copy it. This means that mutating this
        /// array over time will also affect the items that this frozen collection returns.
        /// </remarks>
        internal FrozenList(T[] items)
        {
            _items = items;
        }

        internal FrozenList(IEnumerable<T> items)
        {
            if (items is ICollection<T> c)
            {
                T[] result = new T[c.Count];
                c.CopyTo(result, 0);
                _items = result;
            }
            else
            {
                _items = new List<T>(items).ToArray();
            }
        }

        /// <summary>
        /// Gets the element at the specified index in the list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        public T this[int index] => _items[index];

        /// <summary>
        /// Gets the number of items in the list.
        /// </summary>
        public int Count => _items.Length;

        /// <summary>
        /// Gets a span of the items in the list.
        /// </summary>
        /// <returns>The span of items.</returns>
        public ReadOnlySpan<T> AsSpan() => _items.AsSpan();

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the list.
        /// </returns>
        public FrozenEnumerator<T> GetEnumerator() => new(_items);

        /// <summary>
        /// Gets an enumeration of this list's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<T>)Array.Empty<T>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of this list's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : Array.Empty<T>().GetEnumerator();

        /// <summary>
        /// Copies the content of the list to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<T> destination) => _items.AsSpan().CopyTo(destination);

        /// <summary>
        /// Copies the content of the list to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Determines whether the list contains the given item.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><see langword="true"/> if the item is in the list, otherwise <see langword="false"/>. </returns>
        /// <remarks>This performs a slow linear scan through all the items and compares them using <see cref="EqualityComparer{T}.Default"/>.</remarks>
        public bool Contains(T item) => Array.IndexOf(_items, item) >= 0;

        /// <summary>
        /// Gets a value indicating whether this collection is a read-only collection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICollection<T>.IsReadOnly => true;

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<T>.Clear() => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
