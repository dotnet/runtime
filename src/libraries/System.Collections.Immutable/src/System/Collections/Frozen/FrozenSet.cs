// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A frozen set.
    /// </summary>
    /// <typeparam name="T">The type of the items in the set.</typeparam>
    /// <remarks>
    /// Frozen sets are immutable and are optimized for situations where a set
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a set is created at startup of an application and used throughout the life
    /// of the application.
    ///
    /// This is the general-purpose frozen set which can be used with any item type. If you need
    /// a set that has a string or integer as key, you will get better performance by using
    /// <see cref="FrozenOrdinalStringSet"/> or <see cref="FrozenIntSet"/>
    /// respectively.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IReadOnlyCollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenSet<T> : IFrozenSet<T>, IFindItem<T>, ISet<T>
        where T : notnull
    {
        private readonly FrozenHashTable _hashTable;
        private readonly T[] _items;

        /// <summary>
        /// Gets an empty frozen set.
        /// </summary>
        public static FrozenSet<T> Empty => new(Array.Empty<T>(), EqualityComparer<T>.Default);

        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenSet{T}"/> struct.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <param name="comparer">The comparer used to compare and hash items.</param>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        internal FrozenSet(IEnumerable<T> items, IEqualityComparer<T> comparer)
        {
            T[] incoming = MakeUniqueArray(items, comparer);

            if (ReferenceEquals(comparer, StringComparer.Ordinal) ||
                ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
            {
                comparer = (IEqualityComparer<T>)ComparerPicker.Pick((string[])(object)incoming, ignoreCase: ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase));
            }

            _items = incoming.Length == 0 ? Array.Empty<T>() : new T[incoming.Length];
            Comparer = comparer;

            T[] it = _items;
            _hashTable = FrozenHashTable.Create(
                incoming,
                comparer.GetHashCode,
                (index, item) => it[index] = item);
        }

        private static T[] MakeUniqueArray(IEnumerable<T> items, IEqualityComparer<T> comp)
        {
            if (!(items is HashSet<T> hs && hs.Comparer.Equals(comp)))
            {
                hs = new HashSet<T>(items, comp);
            }

            if (hs.Count == 0)
            {
                return Array.Empty<T>();
            }

            var result = new T[hs.Count];
            hs.CopyTo(result);

            return result;
        }

        /// <inheritdoc />
        public FrozenList<T> Items => new(_items);

        /// <inheritdoc />
        public FrozenEnumerator<T> GetEnumerator() => new(_items);

        /// <summary>
        /// Gets an enumeration of the set's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<T>)Array.Empty<T>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of the set's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : Array.Empty<T>().GetEnumerator();

        /// <summary>
        /// Gets the number of items in the set.
        /// </summary>
        public int Count => _hashTable.Count;

        /// <summary>
        /// Gets the comparer used by this set.
        /// </summary>
        public IEqualityComparer<T> Comparer { get; }

        /// <summary>
        /// Checks whether an item is present in the set.
        /// </summary>
        /// <param name="item">The item to probe for.</param>
        /// <returns><see langword="true"/> if the item is in the set, <see langword="false"/> otherwise.</returns>
        public bool Contains(T item)
        {
            int hashCode = Comparer.GetHashCode(item);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(item, _items[index]))
                    {
                        return true;
                    }
                }

                index++;
            }

            return false;
        }

        /// <summary>
        /// Looks up an item's index.
        /// </summary>
        /// <param name="item">The item to find.</param>
        /// <returns>The index of the item, or -1 if the item was not found.</returns>
        int IFindItem<T>.FindItemIndex(T item)
        {
            int hashCode = Comparer.GetHashCode(item);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(item, _items[index]))
                    {
                        return index;
                    }
                }

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether this set is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a proper subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsProperSubsetOf(IEnumerable<T> other) => SetSupport.IsProperSubsetOf(this, other);

        /// <summary>
        /// Determines whether this set is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a proper superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsProperSupersetOf(IEnumerable<T> other) => SetSupport.IsProperSupersetOf(this, other);

        /// <summary>
        /// Determines whether this set is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsSubsetOf(IEnumerable<T> other) => SetSupport.IsSubsetOf(this, other);

        /// <summary>
        /// Determines whether this set is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsSupersetOf(IEnumerable<T> other) => SetSupport.IsSupersetOf(this, other);

        /// <summary>
        /// Determines whether this set shares any elements with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set and the collection share at least one element; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool Overlaps(IEnumerable<T> other) => SetSupport.Overlaps(this, other);

        /// <summary>
        /// Determines whether this set and collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set and the collection contains the exact same elements; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool SetEquals(IEnumerable<T> other) => SetSupport.SetEquals(this, other);

        /// <summary>
        /// Copies the content of the set to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<T> destination) => _items.AsSpan().CopyTo(destination);

        /// <summary>
        /// Copies the content of the set to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ISet<T>.Add(T item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();
    }
}
