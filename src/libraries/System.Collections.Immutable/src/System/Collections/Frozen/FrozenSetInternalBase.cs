// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides an internal base class from which frozen set implementations may derive.</summary>
    /// <remarks>The primary purpose of this base type is to provide implementations of the various Is methods.</remarks>
    /// <typeparam name="T">The type of values in the set.</typeparam>
    /// <typeparam name="TThisWrapper">
    /// The type of a struct that implements the internal IGenericSpecializedWrapper and wraps this struct.
    /// This is an optimization, to minimize the virtual calls necessary to implement these bulk operations.
    /// </typeparam>
    internal abstract class FrozenSetInternalBase<T, TThisWrapper> : FrozenSet<T>
        where TThisWrapper : struct, FrozenSetInternalBase<T, TThisWrapper>.IGenericSpecializedWrapper
    {
        /// <summary>A wrapper around this that enables access to important members without making virtual calls.</summary>
        private readonly TThisWrapper _thisSet;

        protected FrozenSetInternalBase(IEqualityComparer<T> comparer) : base(comparer)
        {
            _thisSet = default;
            _thisSet.Store(this);
        }

        /// <inheritdoc />
        private protected override bool IsProperSubsetOfCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            if (other is ICollection<T> otherAsCollection)
            {
                int otherCount = otherAsCollection.Count;

                if (otherCount == 0)
                {
                    // No set is a proper subset of an empty set.
                    return false;
                }

                // If the other is a set and is using the same equality comparer, the operation can be optimized.
                if (other is IReadOnlySet<T> otherAsSet && ComparersAreCompatible(otherAsSet))
                {
                    return _thisSet.Count < otherCount && IsSubsetOfSetWithCompatibleComparer(otherAsSet);
                }
            }

            // We couldn't take a fast path; do the full comparison.
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == _thisSet.Count && unfoundCount > 0;
        }

        /// <inheritdoc />
        private protected override bool IsProperSupersetOfCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            if (other is ICollection<T> otherAsCollection)
            {
                int otherCount = otherAsCollection.Count;

                if (otherCount == 0)
                {
                    // If other is the empty set, then this is a superset (since we know this one isn't empty).
                    return true;
                }

                // If the other is a set and is using the same equality comparer, the operation can be optimized.
                if (other is IReadOnlySet<T> otherAsSet && ComparersAreCompatible(otherAsSet))
                {
                    return _thisSet.Count > otherCount && ContainsAllElements(otherAsSet);
                }
            }

            // We couldn't take a fast path; do the full comparison.
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount < _thisSet.Count && unfoundCount == 0;
        }

        /// <inheritdoc />
        private protected override bool IsSubsetOfCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            // If the other is a set and is using the same equality comparer, the operation can be optimized.
            if (other is IReadOnlySet<T> otherAsSet && ComparersAreCompatible(otherAsSet))
            {
                return _thisSet.Count <= otherAsSet.Count && IsSubsetOfSetWithCompatibleComparer(otherAsSet);
            }

            // We couldn't take a fast path; do the full comparison.
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == _thisSet.Count && unfoundCount >= 0;
        }

        /// <inheritdoc />
        private protected override bool IsSupersetOfCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            // Try to compute the answer based purely on counts.
            if (other is ICollection<T> otherAsCollection)
            {
                int otherCount = otherAsCollection.Count;

                // If other is the empty set then this is a superset.
                if (otherCount == 0)
                {
                    return true;
                }

                // If the other is a set and is using the same equality comparer, the operation can be optimized.
                if (other is IReadOnlySet<T> otherAsSet &&
                    otherCount > _thisSet.Count &&
                    ComparersAreCompatible(otherAsSet))
                {
                    return false;
                }
            }

            return ContainsAllElements(other);
        }

        /// <inheritdoc />
        private protected override bool OverlapsCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            foreach (T element in other)
            {
                if (_thisSet.FindItemIndex(element) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        private protected override bool SetEqualsCore(IEnumerable<T> other)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            // If the other is a set and is using the same equality comparer, the operation can be optimized.
            if (other is IReadOnlySet<T> otherAsSet && ComparersAreCompatible(otherAsSet))
            {
                return _thisSet.Count == otherAsSet.Count && ContainsAllElements(otherAsSet);
            }

            // We couldn't take a fast path; do the full comparison.
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount == _thisSet.Count && unfoundCount == 0;
        }

        private bool ComparersAreCompatible(IReadOnlySet<T> other) =>
            other switch
            {
                HashSet<T> hs => _thisSet.Comparer.Equals(hs.Comparer),
                SortedSet<T> ss => _thisSet.Comparer.Equals(ss.Comparer),
                ImmutableHashSet<T> ihs => _thisSet.Comparer.Equals(ihs.KeyComparer),
                ImmutableSortedSet<T> iss => _thisSet.Comparer.Equals(iss.KeyComparer),
                FrozenSet<T> fs => _thisSet.Comparer.Equals(fs.Comparer),
                _ => false
            };

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset.
        /// </summary>
        /// <remarks>
        /// This is only used when other is an IEnumerable and not a known set. If other is a set
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = _count; i.e. everything
        ///    in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = _count; i.e. other may
        ///    have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = _count; i.e
        ///    other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        ///    than _count; i.e. everything in other was in this and this had at least one element
        ///    not contained in other.
        /// </remarks>
        private unsafe KeyValuePair<int, int> CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
        {
            Debug.Assert(_thisSet.Count != 0, "EmptyFrozenSet should have been used.");

            const int BitsPerInt32 = 32;
            int intArrayLength = (_thisSet.Count / BitsPerInt32) + 1;

            int[]? rentedArray = null;
            Span<int> seenItems = intArrayLength <= 256 ?
                stackalloc int[256] :
                (rentedArray = ArrayPool<int>.Shared.Rent(intArrayLength));
            seenItems.Clear();

            // Iterate through every item in the other collection.  For each, if it's
            // found in this set and hasn't yet been found in this set, track it. Otherwise,
            // track that items in the other set weren't found in this one.
            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this
            foreach (T item in other)
            {
                int index = _thisSet.FindItemIndex(item);
                if (index >= 0)
                {
                    if ((seenItems[index / BitsPerInt32] & (1 << index)) == 0)
                    {
                        // Item hasn't been seen yet.
                        seenItems[index / BitsPerInt32] |= 1 << index;
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            if (rentedArray is not null)
            {
                ArrayPool<int>.Shared.Return(rentedArray);
            }

            return new KeyValuePair<int, int>(uniqueFoundCount, unfoundCount);
        }

        private bool ContainsAllElements(IEnumerable<T> other)
        {
            foreach (T element in other)
            {
                if (_thisSet.FindItemIndex(element) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSubsetOfSetWithCompatibleComparer(IReadOnlySet<T> other)
        {
            foreach (T item in _thisSet)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Used to enable generic specialization with reference types.</summary>
        /// <remarks>
        /// The bulk Is operations may end up performing multiple operations on "this" set.
        /// To avoid each of those incurring virtual dispatch to the derived type, the derived
        /// type hands down a struct wrapper through which all calls are performed.  This base
        /// class uses that generic struct wrapper to specialize and devirtualize.
        /// </remarks>
        internal interface IGenericSpecializedWrapper
        {
            void Store(FrozenSet<T> @this);
            int Count { get; }
            int FindItemIndex(T item);
            IEqualityComparer<T> Comparer { get; }
            Enumerator GetEnumerator();
        }
    }
}
