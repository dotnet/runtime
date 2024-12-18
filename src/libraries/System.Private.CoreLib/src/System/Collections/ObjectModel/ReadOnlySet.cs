// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.ObjectModel
{
    /// <summary>Represents a read-only, generic set of values.</summary>
    /// <typeparam name="T">The type of values in the set.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlySet<T> : IReadOnlySet<T>, ISet<T>, ICollection
    {
        /// <summary>The wrapped set.</summary>
        private readonly ISet<T> _set;

        /// <summary>Initializes a new instance of the <see cref="ReadOnlySet{T}"/> class that is a wrapper around the specified set.</summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(ISet<T> set)
        {
            ArgumentNullException.ThrowIfNull(set);
            _set = set;
        }

        /// <summary>Gets an empty <see cref="ReadOnlySet{T}"/>.</summary>
        public static ReadOnlySet<T> Empty { get; } = new ReadOnlySet<T>(new HashSet<T>());

        /// <summary>Gets the set that is wrapped by this <see cref="ReadOnlySet{T}"/> object.</summary>
        protected ISet<T> Set => _set;

        /// <inheritdoc/>
        public int Count => _set.Count;

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() =>
            _set.Count == 0 ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator() :
            _set.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public bool Contains(T item) => _set.Contains(item);

        /// <inheritdoc/>
        public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

        /// <inheritdoc/>
        public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

        /// <inheritdoc/>
        public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

        /// <inheritdoc/>
        public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

        /// <inheritdoc/>
        public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

        /// <inheritdoc/>
        public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

        /// <inheritdoc/>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => CollectionHelpers.CopyTo(_set, array, index);

        /// <inheritdoc/>
        bool ICollection<T>.IsReadOnly => true;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => _set is ICollection c ? c.SyncRoot : this;

        /// <inheritdoc/>
        bool ISet<T>.Add(T item) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        /// <inheritdoc/>
        void ICollection<T>.Clear() => throw new NotSupportedException();

        /// <inheritdoc/>
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
