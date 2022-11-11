// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Frozen
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="FrozenSet{T}"/> class.
    /// </summary>
    /// <remarks>
    /// Frozen collections are immutable and are optimized for situations where a collection
    /// is created very infrequently but is used very frequently at runtime. They have a relatively high
    /// cost to create but provide excellent lookup performance. Thus, these are ideal for cases
    /// where a collection is created once, potentially at the startup of an application, and used throughout
    /// the remainder of the life of the application. Frozen collections should only be initialized with
    /// trusted input.
    /// </remarks>
    public static class FrozenSet
    {
        /// <summary>Creates a <see cref="FrozenSet{T}"/> with the specified values.</summary>
        /// <param name="source">The values to use to populate the set.</param>
        /// <param name="comparer">The comparer implementation to use to compare values for equality. If null, <see cref="EqualityComparer{T}.Default"/> is used.</param>
        /// <typeparam name="T">The type of the values in the set.</typeparam>
        /// <remarks>If the same key appears multiple times in the input, the latter one in the sequence takes precedence.</remarks>
        /// <returns>A frozen set.</returns>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
        {
            ThrowHelper.ThrowIfNull(source);
            comparer ??= EqualityComparer<T>.Default;

            // If the source is already frozen with the same comparer, it can simply be returned.
            if (source is FrozenSet<T> existing &&
                existing.Comparer.Equals(comparer))
            {
                return existing;
            }

            // Ensure we have a HashSet<,> using the specified comparer such that all values
            // are non-null and unique according to that comparer.
            if (source is not HashSet<T> uniqueValues ||
                (uniqueValues.Count != 0 && !uniqueValues.Comparer.Equals(comparer)))
            {
                uniqueValues = new HashSet<T>(source, comparer);
            }

            // If the input was empty, simply return the empty frozen set singleton. The comparer is ignored.
            if (uniqueValues.Count == 0)
            {
                return FrozenSet<T>.Empty;
            }

            if (typeof(T).IsValueType)
            {
                // Optimize for value types when the default comparer is being used. In such a case, the implementation
                // may use EqualityComparer<T>.Default.Equals/GetHashCode directly, with generic specialization enabling
                // the Equals/GetHashCode methods to be devirtualized and possibly inlined.
                if (ReferenceEquals(comparer, EqualityComparer<T>.Default))
                {
                    // In the specific case of Int32 keys, we can optimize further to reduce memory consumption by using
                    // the underlying FrozenHashtable's Int32 index as the values themselves, avoiding the need to store the
                    // same values yet again.
                    return typeof(T) == typeof(int) ?
                        (FrozenSet<T>)(object)new Int32FrozenSet((HashSet<int>)(object)uniqueValues) :
                        new ValueTypeDefaultComparerFrozenSet<T>(uniqueValues);
                }
            }
            else if (typeof(T) == typeof(string))
            {
                // Null is rare as a value in the set and we don't optimize for it.  This enables the ordinal string
                // implementation to fast-path out on null inputs rather than having to accomodate null inputs.
                if (!uniqueValues.Contains(default!))
                {
                    // If the value is a string and the comparer is known to provide ordinal (case-sensitive or case-insensitive) semantics,
                    // we can use an implementation that's able to examine and optimize based on lengths and/or subsequences within those strings.
                    if (ReferenceEquals(comparer, EqualityComparer<T>.Default) ||
                        ReferenceEquals(comparer, StringComparer.Ordinal) ||
                        ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
                    {
                        HashSet<string> stringValues = (HashSet<string>)(object)uniqueValues;
                        IEqualityComparer<string> stringComparer = (IEqualityComparer<string>)(object)comparer;

                        FrozenSet<string> frozenSet =
                            LengthBucketsFrozenSet.TryCreateLengthBucketsFrozenSet(stringValues, stringComparer) ??
                            (FrozenSet<string>)new OrdinalStringFrozenSet(stringValues, stringComparer);

                        return (FrozenSet<T>)(object)frozenSet;
                    }
                }
            }

            // No special-cases apply. Use the default frozen set.
            return new DefaultFrozenSet<T>(uniqueValues, comparer);
        }
    }

    /// <summary>Provides an immutable, read-only set optimized for fast lookup and enumeration.</summary>
    /// <typeparam name="T">The type of the values in this set.</typeparam>
    /// <remarks>
    /// Frozen collections are immutable and are optimized for situations where a collection
    /// is created very infrequently but is used very frequently at runtime. They have a relatively high
    /// cost to create but provide excellent lookup performance. Thus, these are ideal for cases
    /// where a collection is created once, potentially at the startup of an application, and used throughout
    /// the remainder of the life of the application. Frozen collections should only be initialized with
    /// trusted input.
    /// </remarks>
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    [DebuggerDisplay("Count = {Count}")]
    public abstract class FrozenSet<T> : ISet<T>,
#if NET5_0_OR_GREATER
        IReadOnlySet<T>,
#endif
        IReadOnlyCollection<T>, ICollection
    {
        /// <summary>Initialize the set.</summary>
        /// <param name="comparer">The comparer to use and to expose from <see cref="Comparer"/>.</param>
        private protected FrozenSet(IEqualityComparer<T> comparer) => Comparer = comparer;

        /// <summary>Gets an empty <see cref="FrozenSet{T}"/>.</summary>
        public static FrozenSet<T> Empty { get; } = new EmptyFrozenSet<T>();

        /// <summary>Gets the comparer used by this set.</summary>
        public IEqualityComparer<T> Comparer { get; }

        /// <summary>Gets a collection containing the values in the set.</summary>
        /// <remarks>The order of the values in the set is unspecified.</remarks>
        public ImmutableArray<T> Items => ItemsCore;

        /// <inheritdoc cref="Items" />
        private protected abstract ImmutableArray<T> ItemsCore { get; }

        /// <summary>Gets the number of values contained in the set.</summary>
        public int Count => CountCore;

        /// <inheritdoc cref="Count" />
        private protected abstract int CountCore { get; }

        /// <summary>Copies the values in the set to an array, starting at the specified <paramref name="destinationIndex"/>.</summary>
        /// <param name="destination">The array that is the destination of the values copied from the set.</param>
        /// <param name="destinationIndex">The zero-based index in <paramref name="destination"/> at which copying begins.</param>
        public void CopyTo(T[] destination, int destinationIndex)
        {
            ThrowHelper.ThrowIfNull(destination);
            CopyTo(destination.AsSpan(destinationIndex));
        }

        /// <summary>Copies the values in the set to a span.</summary>
        /// <param name="destination">The span that is the destination of the values copied from the set.</param>
        public void CopyTo(Span<T> destination) =>
            Items.AsSpan().CopyTo(destination);

        /// <inheritdoc />
        void ICollection.CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            Array.Copy(Items.array!, 0, array!, index, Items.Length);
        }

        /// <inheritdoc />
        bool ICollection<T>.IsReadOnly => true;

        /// <inheritdoc />
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc />
        object ICollection.SyncRoot => this;

        /// <summary>Determines whether the set contains the specified element.</summary>
        /// <param name="item">The element to locate.</param>
        /// <returns><see langword="true"/> if the set contains the specified element; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item) =>
            FindItemIndex(item) >= 0;

        /// <summary>Searches the set for a given value and returns the equal value it finds, if any.</summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of T when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            int index = FindItemIndex(equalValue);
            if (index >= 0)
            {
                actualValue = Items[index];
                return true;
            }

            actualValue = default;
            return false;
        }

        /// <summary>Finds the index of a specific value in a set.</summary>
        /// <param name="item">The value to lookup.</param>
        /// <returns>The index of the value, or -1 if not found.</returns>
        private protected abstract int FindItemIndex(T item);

        /// <summary>Returns an enumerator that iterates through the set.</summary>
        /// <returns>An enumerator that iterates through the set.</returns>
        public Enumerator GetEnumerator() => GetEnumeratorCore();

        /// <inheritdoc cref="GetEnumerator" />
        private protected abstract Enumerator GetEnumeratorCore();

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            Count == 0 ? ((IList<T>)Array.Empty<T>()).GetEnumerator() :
            GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() =>
            Count == 0 ? Array.Empty<T>().GetEnumerator() :
            GetEnumerator();

        /// <inheritdoc />
        bool ISet<T>.Add(T item) => throw new NotSupportedException();

        /// <inheritdoc />
        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc />
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc />
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc />
        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotSupportedException();

        /// <inheritdoc />
        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        /// <inheritdoc />
        void ICollection<T>.Clear() => throw new NotSupportedException();

        /// <inheritdoc />
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        /// <inheritdoc cref="ISet{T}.IsProperSubsetOf(IEnumerable{T})" />
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return IsProperSubsetOfCore(other);
        }

        /// <inheritdoc cref="IsProperSubsetOf" />
        private protected abstract bool IsProperSubsetOfCore(IEnumerable<T> other);

        /// <inheritdoc cref="ISet{T}.IsProperSupersetOf(IEnumerable{T})" />
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return IsProperSupersetOfCore(other);
        }

        /// <inheritdoc cref="IsProperSupersetOf" />
        private protected abstract bool IsProperSupersetOfCore(IEnumerable<T> other);

        /// <inheritdoc cref="ISet{T}.IsSubsetOf(IEnumerable{T})" />
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return IsSubsetOfCore(other);
        }

        /// <inheritdoc cref="IsSubsetOf" />
        private protected abstract bool IsSubsetOfCore(IEnumerable<T> other);

        /// <inheritdoc cref="ISet{T}.IsSupersetOf(IEnumerable{T})" />
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return IsSupersetOfCore(other);
        }

        /// <inheritdoc cref="IsSupersetOf" />
        private protected abstract bool IsSupersetOfCore(IEnumerable<T> other);

        /// <inheritdoc cref="ISet{T}.Overlaps(IEnumerable{T})" />
        public bool Overlaps(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return OverlapsCore(other);
        }

        /// <inheritdoc cref="Overlaps" />
        private protected abstract bool OverlapsCore(IEnumerable<T> other);

        /// <inheritdoc cref="ISet{T}.SetEquals(IEnumerable{T})" />
        public bool SetEquals(IEnumerable<T> other)
        {
            ThrowHelper.ThrowIfNull(other);
            return SetEqualsCore(other);
        }

        /// <inheritdoc cref="SetEquals" />
        private protected abstract bool SetEqualsCore(IEnumerable<T> other);

        /// <summary>Enumerates the values of a <see cref="FrozenSet{T}"/>.</summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _entries;
            private int _index;

            internal Enumerator(T[] entries)
            {
                _entries = entries;
                _index = -1;
            }

            /// <inheritdoc cref="IEnumerator.MoveNext" />
            public bool MoveNext()
            {
                _index++;
                if ((uint)_index < (uint)_entries.Length)
                {
                    return true;
                }

                _index = _entries.Length;
                return false;
            }

            /// <inheritdoc cref="IEnumerator{T}.Current" />
            public readonly T Current
            {
                get
                {
                    if ((uint)_index >= (uint)_entries.Length)
                    {
                        ThrowHelper.ThrowInvalidOperationException();
                    }

                    return _entries[_index];
                }
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current!;

            /// <inheritdoc />
            void IEnumerator.Reset() => _index = -1;

            /// <inheritdoc />
            void IDisposable.Dispose() { }
        }
    }
}
