// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Implements IComparer to order configuration keys.
    /// </summary>
    public class ConfigurationKeyComparer : IComparer<string>
    {
        private const char KeyDelimiter = ':';

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ConfigurationKeyComparer Instance { get; } = new ConfigurationKeyComparer();

        /// <summary>A comparer delegate with the default instance.</summary>
        internal static Comparison<string> Comparison { get; } = Instance.Compare;

        /// <summary>
        /// Compares two strings.
        /// </summary>
        /// <param name="x">First string.</param>
        /// <param name="y">Second string.</param>
        /// <returns>Less than 0 if x is less than y, 0 if x is equal to y and greater than 0 if x is greater than y.</returns>
        public int Compare(string? x, string? y)
        {
            ReadOnlySpan<char> xSpan = x.AsSpan();
            ReadOnlySpan<char> ySpan = y.AsSpan();

            xSpan = SkipAheadOnDelimiter(xSpan);
            ySpan = SkipAheadOnDelimiter(ySpan);

            // Compare each part until we get two parts that are not equal
            while (!xSpan.IsEmpty && !ySpan.IsEmpty)
            {
                int xDelimiterIndex = xSpan.IndexOf(KeyDelimiter);
                int yDelimiterIndex = ySpan.IndexOf(KeyDelimiter);

                int compareResult = Compare(
                    xDelimiterIndex == -1 ? xSpan : xSpan.Slice(0, xDelimiterIndex),
                    yDelimiterIndex == -1 ? ySpan : ySpan.Slice(0, yDelimiterIndex));

                if (compareResult != 0)
                {
                    return compareResult;
                }

                xSpan = xDelimiterIndex == -1 ? default :
                    SkipAheadOnDelimiter(xSpan.Slice(xDelimiterIndex + 1));
                ySpan = yDelimiterIndex == -1 ? default :
                    SkipAheadOnDelimiter(ySpan.Slice(yDelimiterIndex + 1));
            }

            return xSpan.IsEmpty ? (ySpan.IsEmpty ? 0 : -1) : 1;

            static ReadOnlySpan<char> SkipAheadOnDelimiter(ReadOnlySpan<char> a)
            {
                while (!a.IsEmpty && a[0] == KeyDelimiter)
                {
                    a = a.Slice(1);
                }
                return a;
            }

            static int Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
            {
                int value1 = 0, value2 = 0;
                bool aIsInt = CanBeInteger(a) && TryParseInt(a, out value1);
                bool bIsInt = CanBeInteger(b) && TryParseInt(b, out value2);

                int result;

                if (!aIsInt && !bIsInt)
                {
                    // Both are strings
                    result = a.CompareTo(b, StringComparison.OrdinalIgnoreCase);
                }
                else if (aIsInt && bIsInt)
                {
                    // Both are int
                    result = value1 - value2;
                }
                else
                {
                    // Only one of them is int
                    result = aIsInt ? -1 : 1;
                }

                return result;
            }

            // A necessary (not sufficient) precondition for int.TryParse to succeed: the first character must be a
            // digit, a sign, or leading white space. Guarding with this skips the parse for the common textual keys
            // and, on down-level frameworks, avoids the per-comparison ToString allocation.
            static bool CanBeInteger(ReadOnlySpan<char> s)
            {
                if (s.IsEmpty)
                {
                    return false;
                }

                char c = s[0];
                return (uint)(c - '0') <= 9 || c == '-' || c == '+' || char.IsWhiteSpace(c);
            }

            static bool TryParseInt(ReadOnlySpan<char> s, out int value)
            {
#if NET
                return int.TryParse(s, out value);
#else
                return int.TryParse(s.ToString(), out value);
#endif
            }
        }
    }
}
