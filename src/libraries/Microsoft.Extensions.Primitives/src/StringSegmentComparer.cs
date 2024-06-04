// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Compares two <see cref="StringSegment"/> objects.
    /// </summary>
    public class StringSegmentComparer : IComparer<StringSegment>, IEqualityComparer<StringSegment>
    {
        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-sensitive ordinal <see cref="StringSegment"/> comparison.
        /// </summary>
        public static StringSegmentComparer Ordinal { get; }
            = new StringSegmentComparer(StringComparison.Ordinal, StringComparer.Ordinal);

        /// <summary>
        /// Gets a <see cref="StringSegmentComparer"/> object that performs a case-insensitive ordinal <see cref="StringSegment"/> comparison.
        /// </summary>
        public static StringSegmentComparer OrdinalIgnoreCase { get; }
            = new StringSegmentComparer(StringComparison.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

        private StringSegmentComparer(StringComparison comparison, StringComparer comparer)
        {
            Comparison = comparison;
            Comparer = comparer;
        }

        private StringComparison Comparison { get; }
        private StringComparer Comparer { get; }

        /// <summary>
        /// Compares two <see cref="StringSegment"/> objects and returns an indication of their relative sort order.
        /// </summary>
        /// <param name="x">The first <see cref="StringSegment"/> to compare.</param>
        /// <param name="y">The second <see cref="StringSegment"/> to compare.</param>
        /// <returns>A 32-bit signed integer that indicates the lexical relationship between the two comparands.</returns>
        public int Compare(StringSegment x, StringSegment y)
        {
            return StringSegment.Compare(x, y, Comparison);
        }

        /// <summary>
        /// Determines whether two <see cref="StringSegment"/> objects are equal.
        /// </summary>
        /// <param name="x">The first <see cref="StringSegment"/> to compare.</param>
        /// <param name="y">The second <see cref="StringSegment"/> to compare.</param>
        /// <returns><see langword="true"/> if the two <see cref="StringSegment"/> objects are equal; otherwise, <see langword="false"/>.</returns>
        public bool Equals(StringSegment x, StringSegment y)
        {
            return StringSegment.Equals(x, y, Comparison);
        }

        /// <summary>
        /// Returns a hash code for a <see cref="StringSegment"/> object.
        /// </summary>
        /// <param name="obj">The <see cref="StringSegment"/> to get a hash code for.</param>
        /// <returns>A hash code for a <see cref="StringSegment"/>, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(StringSegment obj)
        {
#if NET || NETSTANDARD2_1
            return string.GetHashCode(obj.AsSpan(), Comparison);
#else
            if (!obj.HasValue)
            {
                return 0;
            }

            // .NET Core strings use randomized hash codes for security reasons. Consequently we must materialize the StringSegment as a string
            return Comparer.GetHashCode(obj.Value);
#endif
        }
    }
}
