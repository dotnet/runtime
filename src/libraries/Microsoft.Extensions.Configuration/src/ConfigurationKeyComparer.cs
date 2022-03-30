// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// IComparer implementation used to order configuration keys.
    /// </summary>
    public class ConfigurationKeyComparer : IComparer<string>
    {
        private static readonly char s_keyDelimiter = ConfigurationPath.KeyDelimiter[0];

        /// <summary>
        /// The default instance.
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

            if (x == null || y == null)
            {
                return (x is null ? 0 : CountPartsIn(xSpan)) - (y is null ? 0 : CountPartsIn(ySpan));
            }

            // Compare each part until we get two parts that are not equal
            while (!xSpan.IsEmpty && !ySpan.IsEmpty)
            {
                if (xSpan[0] == s_keyDelimiter)
                {
                    xSpan = xSpan.Slice(1);
                    continue;
                }

                if (ySpan[0] == s_keyDelimiter)
                {
                    ySpan = ySpan.Slice(1);
                    continue;
                }

                int nextXIndex = xSpan.IndexOf(s_keyDelimiter);
                if (nextXIndex < 0)
                {
                    nextXIndex = xSpan.Length;
                }

                int nextYIndex = ySpan.IndexOf(s_keyDelimiter);
                if (nextYIndex < 0)
                {
                    nextYIndex = ySpan.Length;
                }

                int compareResult = Compare(xSpan.Slice(0, nextXIndex), ySpan.Slice(0, nextYIndex));
                if (compareResult != 0)
                {
                    return compareResult;
                }

                xSpan = xSpan.Slice(nextXIndex);
                ySpan = ySpan.Slice(nextYIndex);
            }

            if (xSpan.IsEmpty)
            {
                return ySpan.IsEmpty ? 0 : -CountPartsIn(ySpan);
            }

            return CountPartsIn(xSpan);

            static int CountPartsIn(ReadOnlySpan<char> a)
            {
                int count = 0, aIndex = 0;
                while (aIndex < a.Length)
                {
                    int nextAIndex = a.Slice(aIndex).IndexOf(s_keyDelimiter);
                    if (nextAIndex < 0)
                    {
                        return count + 1;
                    }

                    if (a[aIndex] == s_keyDelimiter)
                    {
                        aIndex++;
                        continue;
                    }

                    aIndex += nextAIndex + 1;
                    count++;
                }

                return count;
            }

            static int Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
            {
#if NETCOREAPP
                bool aIsInt = int.TryParse(a, out int value1);
                bool bIsInt = int.TryParse(b, out int value2);
#else
                bool aIsInt = int.TryParse(a.ToString(), out int value1);
                bool bIsInt = int.TryParse(b.ToString(), out int value2);
#endif
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
        }
    }
}
