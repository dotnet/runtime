// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// IComparer implementation used to order configuration keys.
    /// </summary>
    public class ConfigurationKeyComparer : IComparer<string>
    {
        private static readonly string[] _keyDelimiterArray = new[] { ConfigurationPath.KeyDelimiter };

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
            if (x is not null && y is not null)
            {
                if (x.Contains(ConfigurationPath.KeyDelimiter) && y.Contains(ConfigurationPath.KeyDelimiter))
                {
                    int xIndex = 0;
                    int yIndex = 0;

                    // Compare each part until we get two parts that are not equal
                    while (xIndex < x.Length && yIndex < y.Length)
                    {
                        int nextXIndex = x.IndexOf(ConfigurationPath.KeyDelimiter, xIndex);
                        int nextYIndex = y.IndexOf(ConfigurationPath.KeyDelimiter, yIndex);

                        ReadOnlySpan<char> xSpan;
                        if (nextXIndex >= 0)
                        {
                            xSpan = x.AsSpan().Slice(xIndex, nextXIndex - xIndex);
                            xIndex = nextXIndex + 1;
                        }
                        else
                        {
                            xSpan = x.AsSpan().Slice(xIndex, x.Length - xIndex);
                            xIndex = x.Length;
                        }

                        ReadOnlySpan<char> ySpan;
                        if (nextYIndex >= 0)
                        {
                            ySpan = y.AsSpan().Slice(yIndex, nextYIndex - yIndex);
                            yIndex = nextYIndex + 1;
                        }
                        else
                        {
                            ySpan = y.AsSpan().Slice(yIndex, y.Length - yIndex);
                            yIndex = y.Length;
                        }

                        int compareResult = Compare(xSpan, ySpan);
                        if (compareResult != 0)
                        {
                            return compareResult;
                        }
                    }

                    string[] xParts = x.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries);
                    string[] yParts = y.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries);

                    // If we get here, the common parts are equal.
                    // If they are of the same length, then they are totally identical
                    return xParts.Length - yParts.Length;
                }
                else
                {
                    return Compare(x.AsSpan(), y.AsSpan());
                }
            }

            return x?.Length ?? 0 - y?.Length ?? 0;

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
