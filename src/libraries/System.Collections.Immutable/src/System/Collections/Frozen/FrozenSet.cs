// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

namespace System.Collections.Frozen
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="FrozenSet{T}"/> class.
    /// </summary>
    public static class FrozenSet
    {
        /// <summary>Creates a <see cref="FrozenSet{T}"/> with the specified values.</summary>
        /// <param name="source">The values to use to populate the set.</param>
        /// <param name="comparer">The comparer implementation to use to compare values for equality. If null, <see cref="EqualityComparer{T}.Default"/> is used.</param>
        /// <typeparam name="T">The type of the values in the set.</typeparam>
        /// <returns>A frozen set.</returns>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
        {
            GetUniqueValues(source, comparer, out FrozenSet<T>? existing, out HashSet<T>? uniqueValues);

            // Trimming note:
            // This avoids delegating to ToFrozenSet(..., bool optimizeForReading) to avoid rooting
            // ChooseImplementationOptimizedForReading, which in turn references many different concrete implementations.
            return existing ??
                ChooseImplementationOptimizedForConstruction(uniqueValues!);
        }

        /// <summary>Creates a <see cref="FrozenSet{T}"/> with the specified values.</summary>
        /// <param name="source">The values to use to populate the set.</param>
        /// <param name="optimizeForReading">
        /// <see langword="true"/> to do more work as part of set construction to optimize for subsequent reading of the data;
        /// <see langword="false"/> to prefer making construction more efficient. The default is <see langword="false"/>.
        /// </param>
        /// <typeparam name="T">The type of the values in the set.</typeparam>
        /// <returns>A frozen set.</returns>
        /// <remarks>
        /// Frozen collections are immutable and may be optimized for situations where a collection is created very infrequently but
        /// is used very frequently at runtime. Setting <paramref name="optimizeForReading"/> to <see langword="true"/> will result in a
        /// relatively high cost to create the collection in exchange for improved performance when subsequently using the collection.
        /// Using <see langword="true"/> is ideal for collections that are created once, potentially at the startup of a service, and then
        /// used throughout the remainder of the lifetime of the service. Because of the high cost of creation, frozen collections should
        /// only be initialized with trusted input.
        /// </remarks>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, bool optimizeForReading) =>
            ToFrozenSet(source, null, optimizeForReading);

        /// <summary>Creates a <see cref="FrozenSet{T}"/> with the specified values.</summary>
        /// <param name="source">The values to use to populate the set.</param>
        /// <param name="comparer">The comparer implementation to use to compare values for equality. If null, <see cref="EqualityComparer{T}.Default"/> is used.</param>
        /// <param name="optimizeForReading">
        /// <see langword="true"/> to do more work as part of set construction to optimize for subsequent reading of the data;
        /// <see langword="false"/> to prefer making construction more efficient. The default is <see langword="false"/>.
        /// </param>
        /// <typeparam name="T">The type of the values in the set.</typeparam>
        /// <returns>A frozen set.</returns>
        /// <remarks>
        /// Frozen collections are immutable and may be optimized for situations where a collection is created very infrequently but
        /// is used very frequently at runtime. Setting <paramref name="optimizeForReading"/> to <see langword="true"/> will result in a
        /// relatively high cost to create the collection in exchange for improved performance when subsequently using the collection.
        /// Using <see langword="true"/> is ideal for collections that are created once, potentially at the startup of a service, and then
        /// used throughout the remainder of the lifetime of the service. Because of the high cost of creation, frozen collections should
        /// only be initialized with trusted input.
        /// </remarks>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer, bool optimizeForReading)
        {
            GetUniqueValues(source, comparer, out FrozenSet<T>? existing, out HashSet<T>? uniqueValues);
            return existing ?? (optimizeForReading ?
                ChooseImplementationOptimizedForReading(uniqueValues!) :
                ChooseImplementationOptimizedForConstruction(uniqueValues!));
        }

        /// <summary>Extracts from the source either an existing <see cref="FrozenSet{T}"/> instance or a <see cref="HashSet{T}"/> containing the values and the specified <paramref name="comparer"/>.</summary>
        private static void GetUniqueValues<T>(
            IEnumerable<T> source, IEqualityComparer<T>? comparer,
            out FrozenSet<T>? existing, out HashSet<T>? uniqueValues)
        {
            ThrowHelper.ThrowIfNull(source);
            comparer ??= EqualityComparer<T>.Default;

            // If the source is already frozen with the same comparer, it can simply be returned.
            if (source is FrozenSet<T> fs && fs.Comparer.Equals(comparer))
            {
                existing = fs;
                uniqueValues = null;
                return;
            }

            // Ensure we have a HashSet<> using the specified comparer such that all items
            // are non-null and unique according to that comparer.
            uniqueValues = source as HashSet<T>;
            if (uniqueValues is null ||
                (uniqueValues.Count != 0 && !uniqueValues.Comparer.Equals(comparer)))
            {
                uniqueValues = new HashSet<T>(source, comparer);
            }

            if (uniqueValues.Count == 0)
            {
                existing = ReferenceEquals(comparer, FrozenSet<T>.Empty.Comparer) ?
                    FrozenSet<T>.Empty :
                    new EmptyFrozenSet<T>(comparer);
                uniqueValues = null;
                return;
            }

            Debug.Assert(uniqueValues is not null);
            Debug.Assert(uniqueValues.Comparer.Equals(comparer));

            existing = null;
        }

        private static FrozenSet<T> ChooseImplementationOptimizedForConstruction<T>(HashSet<T> source)
        {
            return new DefaultFrozenSet<T>(source, optimizeForReading: false);
        }

        private static FrozenSet<T> ChooseImplementationOptimizedForReading<T>(HashSet<T> source)
        {
            IEqualityComparer<T> comparer = source.Comparer;

            if (typeof(T).IsValueType)
            {
                // Optimize for value types when the default comparer is being used. In such a case, the implementation
                // may use {Equality}Comparer<T>.Default.Compare/Equals/GetHashCode directly, with generic specialization enabling
                // the Equals/GetHashCode methods to be devirtualized and possibly inlined.
                if (ReferenceEquals(comparer, EqualityComparer<T>.Default))
                {
                    if (source.Count <= Constants.MaxItemsInSmallValueTypeFrozenCollection)
                    {
                        // If the type is a something we know we can efficiently compare, use a specialized implementation
                        // that will enable quickly ruling out values outside of the range of keys stored.
                        if (Constants.IsKnownComparable<T>())
                        {
                            return (FrozenSet<T>)(object)new SmallValueTypeComparableFrozenSet<T>(source);
                        }

                        // Otherwise, use an implementation optimized for a small number of value types using the default comparer.
                        return (FrozenSet<T>)(object)new SmallValueTypeDefaultComparerFrozenSet<T>(source);
                    }

                    // Use a hash-based implementation.

                    // For Int32 values, we can reuse the item storage as the hash storage, saving on space and extra indirection.
                    if (typeof(T) == typeof(int))
                    {
                        return (FrozenSet<T>)(object)new Int32FrozenSet((HashSet<int>)(object)source);
                    }

                    // Fallback to an implementation usable with any value type and the default comparer.
                    return new ValueTypeDefaultComparerFrozenSet<T>(source);
                }
            }
            else if (typeof(T) == typeof(string))
            {
                // Null is rare as a value in the set and we don't optimize for it.  This enables the ordinal string
                // implementation to fast-path out on null inputs rather than having to accommodate null inputs.
                if (!source.Contains(default!))
                {
                    // If the value is a string and the comparer is known to provide ordinal (case-sensitive or case-insensitive) semantics,
                    // we can use an implementation that's able to examine and optimize based on lengths and/or subsequences within those strings.
                    if (ReferenceEquals(comparer, EqualityComparer<T>.Default) ||
                        ReferenceEquals(comparer, StringComparer.Ordinal) ||
                        ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
                    {
                        HashSet<string> stringValues = (HashSet<string>)(object)source;
                        var entries = new string[stringValues.Count];
                        stringValues.CopyTo(entries);

                        IEqualityComparer<string> stringComparer = (IEqualityComparer<string>)(object)comparer;

                        FrozenSet<string>? frozenSet = LengthBucketsFrozenSet.CreateLengthBucketsFrozenSetIfAppropriate(entries, stringComparer);
                        if (frozenSet is not null)
                        {
                            return (FrozenSet<T>)(object)frozenSet;
                        }

                        KeyAnalyzer.Analyze(entries, ReferenceEquals(stringComparer, StringComparer.OrdinalIgnoreCase), out KeyAnalyzer.AnalysisResults results);
                        if (results.SubstringHashing)
                        {
                            if (results.RightJustifiedSubstring)
                            {
                                if (results.IgnoreCase)
                                {
                                    frozenSet = results.AllAscii
                                        ? new OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveAsciiSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount)
                                        : new OrdinalStringFrozenSet_RightJustifiedCaseInsensitiveSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount);
                                }
                                else
                                {
                                    frozenSet = results.HashCount == 1
                                        ? new OrdinalStringFrozenSet_RightJustifiedSingleChar(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex)
                                        : new OrdinalStringFrozenSet_RightJustifiedSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount);
                                }
                            }
                            else
                            {
                                if (results.IgnoreCase)
                                {
                                    frozenSet = results.AllAscii
                                        ? new OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveAsciiSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount)
                                        : new OrdinalStringFrozenSet_LeftJustifiedCaseInsensitiveSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount);
                                }
                                else
                                {
                                    frozenSet = results.HashCount == 1
                                        ? new OrdinalStringFrozenSet_LeftJustifiedSingleChar(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex)
                                        : new OrdinalStringFrozenSet_LeftJustifiedSubstring(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff, results.HashIndex, results.HashCount);
                                }
                            }
                        }
                        else
                        {
                            if (results.IgnoreCase)
                            {
                                frozenSet = results.AllAscii
                                    ? new OrdinalStringFrozenSet_FullCaseInsensitiveAscii(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff)
                                    : new OrdinalStringFrozenSet_FullCaseInsensitive(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff);
                            }
                            else
                            {
                                frozenSet = new OrdinalStringFrozenSet_Full(entries, stringComparer, results.MinimumLength, results.MaximumLengthDiff);
                            }
                        }

                        return (FrozenSet<T>)(object)frozenSet;
                    }
                }
            }

            if (source.Count <= Constants.MaxItemsInSmallFrozenCollection)
            {
                // use the specialized set for low item counts
                return new SmallFrozenSet<T>(source);
            }

            // No special-cases apply. Use the default frozen set.
            return new DefaultFrozenSet<T>(source, optimizeForReading: true);
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
        public static FrozenSet<T> Empty { get; } = new EmptyFrozenSet<T>(EqualityComparer<T>.Default);

        /// <summary>Gets the comparer used by this set.</summary>
        public IEqualityComparer<T> Comparer { get; }

        /// <summary>Gets a collection containing the values in the set.</summary>
        /// <remarks>The order of the values in the set is unspecified.</remarks>
        public ImmutableArray<T> Items => ImmutableArrayFactory.Create(ItemsCore);

        /// <inheritdoc cref="Items" />
        private protected abstract T[] ItemsCore { get; }

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

            T[] items = ItemsCore;
            Array.Copy(items, 0, array!, index, items.Length);
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
