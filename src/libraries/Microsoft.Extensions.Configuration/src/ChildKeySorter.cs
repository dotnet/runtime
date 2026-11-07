// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

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
        /// <summary>Sorts the first <paramref name="count"/> keys of <paramref name="keys"/> in place.</summary>
        /// <param name="keys">The accumulated child keys.</param>
        /// <param name="count">The number of keys to sort.</param>
        public static void Sort(string[] keys, int count)
        {
            if (count < 2 || TryOrderAsIndexes(keys, count))
            {
                return;
            }

            NumberFormatInfo formatInfo = NumberFormatInfo.CurrentInfo;
            bool preCheck = MayPreCheck(formatInfo.PositiveSign) && MayPreCheck(formatInfo.NegativeSign);

#if NET11_0_OR_GREATER
            keys.AsSpan(0, count).Sort(new SegmentComparer(formatInfo, preCheck));
#elif NET
            // Before that, sorting by IComparer<T> boxed the comparer and then made a Comparison<T> out of it anyway,
            // so the delegate is passed directly instead, which costs one allocation rather than two.
            keys.AsSpan(0, count).Sort((x, y) => CompareSegment(x, y, formatInfo, preCheck));
#else
            Array.Sort(keys, 0, count, new SegmentComparer(formatInfo, preCheck));
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

        private static int CompareSegment(string? x, string? y, NumberFormatInfo formatInfo, bool preCheck)
        {
            if (string.IsNullOrEmpty(x))
            {
                return string.IsNullOrEmpty(y) ? 0 : -1;
            }
            else if (string.IsNullOrEmpty(y))
            {
                return 1;
            }

            return TryParse(x, formatInfo, preCheck, out int xNumber)
                ? TryParse(y, formatInfo, preCheck, out int yNumber) ? xNumber.CompareTo(yNumber) : -1
                : TryParse(y, formatInfo, preCheck, out int _) ? 1 : x.AsSpan().CompareTo(y.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParse(string s, NumberFormatInfo formatInfo, bool preCheck, out int value)
        {
            if (preCheck && CannotStartNumber(s[0]))
            {
                value = 0;
                return false;
            }

            return int.TryParse(s, NumberStyles.Integer, formatInfo, out value);
        }

        private static bool CannotStartNumber(char c) =>
            c < 0x80 && (uint)(c - '0') > 9 && c != '-' && c != '+' && !char.IsWhiteSpace(c);

        private static bool MayPreCheck(string sign) => sign.Length == 0 || !CannotStartNumber(sign[0]);

#if NET11_0_OR_GREATER || !NET
        private readonly struct SegmentComparer : IComparer<string>
        {
            private readonly NumberFormatInfo _formatInfo;
            private readonly bool _preCheck;

            internal SegmentComparer(NumberFormatInfo formatInfo, bool preCheck)
            {
                _formatInfo = formatInfo;
                _preCheck = preCheck;
            }

            public int Compare(string? x, string? y) => CompareSegment(x, y, _formatInfo, _preCheck);
        }
#endif
    }
}
