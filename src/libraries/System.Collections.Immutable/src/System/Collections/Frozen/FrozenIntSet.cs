// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A frozen set of integers.
    /// </summary>
    /// <remarks>
    /// Frozen sets are immutable and are optimized for situations where a set
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a set is created at startup of an application and used throughout the life
    /// of the application.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IReadOnlyCollectionDebugView<int>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenIntSet : IFrozenSet<int>, IFindItem<int>, ISet<int>
    {
        private readonly FrozenHashTable _hashTable;

        /// <summary>
        /// Gets an empty frozen integer set.
        /// </summary>
        public static FrozenIntSet Empty => new(Array.Empty<int>());

        internal FrozenIntSet(IEnumerable<int> items)
        {
            int[] incoming = MakeUniqueArray(items);
            _hashTable = FrozenHashTable.Create(
                incoming,
                item => item,
                (_, _) => { });
        }

        private static int[] MakeUniqueArray(IEnumerable<int> items)
        {
            EqualityComparer<int> comp = EqualityComparer<int>.Default;
            if (!(items is HashSet<int> hs && hs.Comparer == comp))
            {
                hs = new HashSet<int>(items, comp);
            }

            if (hs.Count == 0)
            {
                return Array.Empty<int>();
            }

            int[] result = new int[hs.Count];
            hs.CopyTo(result);

            return result;
        }

        /// <inheritdoc />
        public FrozenList<int> Items => new(_hashTable.HashCodes);

        /// <inheritdoc />
        public FrozenEnumerator<int> GetEnumerator() => new(_hashTable.HashCodes);

        /// <summary>
        /// Gets an enumeration of the set's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<int>)Array.Empty<int>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of the set's items.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<int>)Array.Empty<int>()).GetEnumerator();

        /// <summary>
        /// Gets the number of items in the set.
        /// </summary>
        public int Count => _hashTable.Count;

        /// <summary>
        /// Checks whether an item is present in the set.
        /// </summary>
        /// <param name="item">The item to probe for.</param>
        /// <returns><see langword="true"/> if the item is in the set, <see langword="false"/> otherwise.</returns>
        public bool Contains(int item)
        {
            _hashTable.FindMatchingEntries(item, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (item == _hashTable.EntryHashCode(index))
                {
                    return true;
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
        int IFindItem<int>.FindItemIndex(int item)
        {
            _hashTable.FindMatchingEntries(item, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (item == _hashTable.EntryHashCode(index))
                {
                    return index;
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
        public bool IsProperSubsetOf(IEnumerable<int> other) => SetSupport.IsProperSubsetOf(this, other);

        /// <summary>
        /// Determines whether this set is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a proper superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsProperSupersetOf(IEnumerable<int> other) => SetSupport.IsProperSupersetOf(this, other);

        /// <summary>
        /// Determines whether this set is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsSubsetOf(IEnumerable<int> other) => SetSupport.IsSubsetOf(this, other);

        /// <summary>
        /// Determines whether this set is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set is a superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool IsSupersetOf(IEnumerable<int> other) => SetSupport.IsSupersetOf(this, other);

        /// <summary>
        /// Determines whether this set shares any elements with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set and the collection share at least one element; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool Overlaps(IEnumerable<int> other) => SetSupport.Overlaps(this, other);

        /// <summary>
        /// Determines whether this set and collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns><see langword="true" /> if the set and the collection contains the exact same elements; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="other"/> is <see langword="null"/>.</exception>
        public bool SetEquals(IEnumerable<int> other) => SetSupport.SetEquals(this, other);

        /// <summary>
        /// Copies the content of the set to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<int> destination) => _hashTable.HashCodes.AsSpan().CopyTo(destination);

        /// <summary>
        /// Copies the content of the set to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(int[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Gets a value indicating whether this collection is a read-only collection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICollection<int>.IsReadOnly => true;

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<int>.Add(int item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<int>.Clear() => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<int>.Remove(int item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ISet<int>.Add(int item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<int>.ExceptWith(IEnumerable<int> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<int>.IntersectWith(IEnumerable<int> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<int>.SymmetricExceptWith(IEnumerable<int> other) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ISet<int>.UnionWith(IEnumerable<int> other) => throw new NotSupportedException();
    }
}
