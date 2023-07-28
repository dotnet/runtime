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

        public int Compare(StringSegment x, StringSegment y)
        {
            return StringSegment.Compare(x, y, Comparison);
        }

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
#if NETCOREAPP || NETSTANDARD2_1
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
