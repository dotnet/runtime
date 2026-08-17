// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Orders accumulated child keys the way <see cref="ConfigurationKeyComparer"/> does, but without paying for
    /// work related to segments and delimiters that cannot apply to a child key.
    /// </summary>
    /// <remarks>
    /// A child key is a single path segment: <see cref="ConfigurationProvider"/> slices it between two delimiters, so
    /// it can never contain one. The general comparer does not know that, and per comparison it skips leading
    /// delimiters and scans both operands for a <c>':'</c> that is not there.
    /// <para>
    /// The two orders agree except on a pair of numeric keys far enough apart that the general comparer's
    /// <c>value1 - value2</c> overflows and reverses them. This sorter compares such a pair with <c>CompareTo</c>,
    /// so it does not reproduce that.
    /// </para>
    /// </remarks>
    internal static class ChildKeySorter
    {
#if NET
        private static readonly Comparison<string> s_comparer = CompareSegment;
#else
        private static readonly SegmentComparer s_comparer = new SegmentComparer();
#endif

        /// <summary>Sorts the first <paramref name="count"/> keys of <paramref name="keys"/> in place.</summary>
        /// <param name="keys">The accumulated child keys.</param>
        /// <param name="count">The number of keys to sort.</param>
        public static void Sort(string[] keys, int count)
        {
            if (count < 2 || TryOrderAsIndexes(keys, count))
            {
                return;
            }

#if NET
            keys.AsSpan(0, count).Sort(s_comparer);
#else
            Array.Sort(keys, 0, count, s_comparer);
#endif
        }

        /// <summary>
        /// Orders the children of an array section by moving each key to the slot its own value names, which needs no
        /// comparisons at all. An array is stored as <c>0</c>, <c>1</c>, <c>2</c> and so on, so its children are the
        /// indexes <c>0</c> to <paramref name="count"/> - 1, and a provider lists them in document order, which leaves
        /// them already placed.
        /// </summary>
        /// <returns>
        /// <see langword="false"/> as soon as a key is found that is not such an index, leaving the keys in some
        /// permutation of themselves for the caller to sort.
        /// </returns>
        private static bool TryOrderAsIndexes(string[] keys, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!TryGetIndex(keys[i], count, out int index))
                {
                    return false;
                }

                while (index != i)
                {
                    if (!TryGetIndex(keys[index], count, out int occupant) || occupant == index)
                    {
                        return false;
                    }

                    (keys[i], keys[index]) = (keys[index], keys[i]);
                    index = occupant;
                }
            }

            return true;
        }

        /// <summary>
        /// Parses a segment naming one of <paramref name="count"/> slots. Digits only, and no leading zero unless the
        /// segment is <c>"0"</c>, so that no two segments can name the same slot.
        /// </summary>
        private static bool TryGetIndex(string? segment, int count, out int index)
        {
            index = 0;
            if (string.IsNullOrEmpty(segment) || segment.Length > 9 || (segment.Length > 1 && segment[0] == '0'))
            {
                return false;
            }

            for (int i = 0; i < segment.Length; i++)
            {
                int digit = segment[i] - '0';
                if ((uint)digit > 9)
                {
                    return false;
                }

                index = (index * 10) + digit;
                if (index >= count)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareSegment(string? x, string? y)
        {
            if (string.IsNullOrEmpty(x))
            {
                return string.IsNullOrEmpty(y) ? 0 : -1;
            }
            else if (string.IsNullOrEmpty(y))
            {
                return 1;
            }

            return int.TryParse(x, out int xNumber)
                ? int.TryParse(y, out int yNumber) ? xNumber.CompareTo(yNumber) : -1
                : int.TryParse(y, out int _) ? 1 : x.AsSpan().CompareTo(y.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

#if !NET
        private sealed class SegmentComparer : IComparer<string>
        {
            public int Compare(string? x, string? y) => CompareSegment(x, y);
        }
#endif
    }
}
