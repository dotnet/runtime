// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal static class SetSupport
    {
        public static string[] ExtractStringKeysToArray<TKey, TValue>(KeyValuePair<TKey, TValue>[] source) where TKey : notnull
        {
            string[] keys = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                keys[i] = (string)(object)source[i].Key;
            }
            return keys;
        }

        public static bool IsProperSubsetOf<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>, IFindItem<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            if (other is ICollection<T> otherAsCollection)
            {
                // No set is a proper subset of an empty set.
                if (otherAsCollection.Count == 0)
                {
                    return false;
                }

                // The empty set is a proper subset of anything but the empty set.
                if (set.Count == 0)
                {
                    return otherAsCollection.Count > 0;
                }

                // Faster if other is a hashset (and we're using same equality comparer).
                if (other is IReadOnlySet<T> otherAsSet && CompatibleComparers(set, otherAsSet))
                {
                    if (set.Count >= otherAsSet.Count)
                    {
                        return false;
                    }

                    // This has strictly less than number of items in other, so the following
                    // check suffices for proper subset.
                    return IsSubsetOfHashSetWithCompatibleComparer(set, otherAsSet);
                }
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(set, other, returnIfUnfound: false);
            return uniqueCount == set.Count && unfoundCount > 0;
        }

        public static bool IsProperSupersetOf<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>, IFindItem<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            // The empty set isn't a proper superset of any set, and a set is never a strict superset of itself.
            if (set.Count == 0)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    // Note that this has at least one element, based on above check.
                    return true;
                }

                // Faster if other is a hashset with the same equality comparer
                if (other is IReadOnlySet<T> otherAsSet && CompatibleComparers(set, otherAsSet))
                {
                    if (otherAsSet.Count >= set.Count)
                    {
                        return false;
                    }

                    // Now perform element check.
                    return ContainsAllElements(set, otherAsSet);
                }
            }

            // Couldn't fall out in the above cases; do it the long way
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(set, other, returnIfUnfound: true);
            return uniqueCount < set.Count && unfoundCount == 0;
        }

        public static bool IsSubsetOf<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>, IFindItem<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            // The empty set is a subset of any set, and a set is a subset of itself.
            // Set is always a subset of itself
            if (set.Count == 0)
            {
                return true;
            }

            // Faster if other has unique elements according to this equality comparer; so check
            // that other is a hashset using the same equality comparer.
            if (other is IReadOnlySet<T> otherAsSet && CompatibleComparers(set, otherAsSet))
            {
                // if this has more elements then it can't be a subset
                if (set.Count > otherAsSet.Count)
                {
                    return false;
                }

                // already checked that we're using same equality comparer. simply check that
                // each element in this is contained in other.
                return IsSubsetOfHashSetWithCompatibleComparer(set, otherAsSet);
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(set, other, returnIfUnfound: false);
            return uniqueCount == set.Count && unfoundCount >= 0;
        }

        public static bool IsSupersetOf<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            // Try to fall out early based on counts.
            if (other is ICollection<T> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    return true;
                }

                // Try to compare based on counts alone if other is a hashset with same equality comparer.
                if (other is IReadOnlySet<T> otherAsSet &&
                    CompatibleComparers(set, otherAsSet) &&
                    otherAsSet.Count > set.Count)
                {
                    return false;
                }
            }

            return ContainsAllElements(set, other);
        }

        public static bool Overlaps<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            if (set.Count == 0)
            {
                return false;
            }

            foreach (T element in other)
            {
                if (set.Contains(element))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool SetEquals<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>, IFindItem<T>
            where T : notnull
        {
            Requires.NotNull(other, nameof(other));

            // Faster if other is a hashset and we're using same equality comparer.
            if (other is IReadOnlySet<T> otherAsSet && CompatibleComparers(set, otherAsSet))
            {
                // Attempt to return early: since both contain unique elements, if they have
                // different counts, then they can't be equal.
                if (set.Count != otherAsSet.Count)
                {
                    return false;
                }

                // Already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal.
                return ContainsAllElements(set, otherAsSet);
            }
            else
            {
                // If this count is 0 but other contains at least one element, they can't be equal.
                if (set.Count == 0 &&
                    other is ICollection<T> otherAsCollection &&
                    otherAsCollection.Count > 0)
                {
                    return false;
                }

                (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(set, other, returnIfUnfound: true);
                return uniqueCount == set.Count && unfoundCount == 0;
            }
        }

        private static bool CompatibleComparers<T, TSet>(in TSet set, IReadOnlySet<T> other)
            where TSet : IFrozenSet<T>
            where T : notnull
        {
            if (set is FrozenOrdinalStringSet foss)
            {
                if (other is HashSet<string> hs)
                {
                    if (foss.Comparer.CaseInsensitive)
                    {
                        return hs.Comparer.Equals(StringComparer.OrdinalIgnoreCase);
                    }

                    return hs.Comparer.Equals(StringComparer.Ordinal);
                }
                else if (other is FrozenOrdinalStringSet otherfoss)
                {
                    return foss.Comparer.CaseInsensitive == otherfoss.Comparer.CaseInsensitive;
                }
            }
            else if (set is FrozenSet<T> s)
            {
                if (other is HashSet<T> hs)
                {
                    return hs.Comparer == s.Comparer;
                }
                else if (other is FrozenSet<T> fs)
                {
                    return fs.Comparer == s.Comparer;
                }
            }
            else
            {
                if (other is HashSet<int> hs)
                {
                    return hs.Comparer == EqualityComparer<int>.Default;
                }

                return other is FrozenIntSet;
            }

            return false;
        }

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = _count; i.e. everything
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = _count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = _count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than _count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        ///
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        private static unsafe KeyValuePair<int, int> CheckUniqueAndUnfoundElements<T, TSet>(in TSet set, IEnumerable<T> other, bool returnIfUnfound)
            where TSet : IFrozenSet<T>, IFindItem<T>
            where T : notnull
        {
            // Need special case for when this has no elements.
            if (set.Count == 0)
            {
                int numElementsInOther = 0;
                foreach (T item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return new KeyValuePair<int, int>(0, numElementsInOther);
            }

            int originalCount = set.Count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            int[]? rentedArray = null;
            Span<int> span = intArrayLength <= 128 ?
                stackalloc int[128] :
                (rentedArray = ArrayPool<int>.Shared.Rent(intArrayLength));

            var bitHelper = new BitHelper(span);

            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this

            foreach (T item in other)
            {
                int index = set.FindItemIndex(item);
                if (index >= 0)
                {
                    if (!bitHelper.IsMarked(index))
                    {
                        // Item hasn't been seen yet.
                        bitHelper.MarkBit(index);
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

        private static bool ContainsAllElements<T, TSet>(in TSet set, IEnumerable<T> other)
            where TSet : IFrozenSet<T>
            where T : notnull
        {
            foreach (T element in other)
            {
                if (!set.Contains(element))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSubsetOfHashSetWithCompatibleComparer<T, TSet>(in TSet set, IReadOnlySet<T> other)
            where TSet : IFrozenSet<T>
            where T : notnull
        {
            foreach (T item in set)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        private ref struct BitHelper
        {
            private const int IntSize = sizeof(int) * 8;
            private readonly Span<int> _span;

            internal BitHelper(Span<int> span)
            {
                span.Clear();
                _span = span;
            }

            internal static int ToIntArrayLength(int n) => ((n - 1) / IntSize) + 1;

            internal void MarkBit(int bitPosition)
            {
                int bitArrayIndex = bitPosition / IntSize;
                _span[bitArrayIndex] |= 1 << (bitPosition % IntSize);
            }

            internal bool IsMarked(int bitPosition)
            {
                int bitArrayIndex = bitPosition / IntSize;
                return (_span[bitArrayIndex] & (1 << (bitPosition % IntSize))) != 0;
            }
        }
    }
}
