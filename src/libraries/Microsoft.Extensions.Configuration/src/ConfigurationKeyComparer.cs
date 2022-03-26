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
            if (string.IsNullOrWhiteSpace(x))
            {
                if (string.IsNullOrWhiteSpace(y))
                {
                    return 0;
                }

                return -y!.Length;
            }

            if (string.IsNullOrWhiteSpace(y))
            {
                return x!.Length;
            }

            int result;
            if (!x!.Contains(ConfigurationPath.KeyDelimiter) || !y!.Contains(ConfigurationPath.KeyDelimiter))
            {
                result = compare(x, y);
                if (result != 0)
                {
                    // One of them is different
                    return result;
                }
            }

            string[] xParts = x.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries);
            string[] yParts = y.Split(_keyDelimiterArray, StringSplitOptions.RemoveEmptyEntries);

            // Compare each part until we get two parts that are not equal
            for (int i = 0; i < Math.Min(xParts.Length, yParts.Length); i++)
            {
                result = compare(xParts[i], yParts[i]);
                if (result != 0)
                {
                    // One of them is different
                    return result;
                }
            }

            // If we get here, the common parts are equal.
            // If they are of the same length, then they are totally identical
            return xParts.Length - yParts.Length;

            static int compare(string? a, string? b)
            {
                int value1 = 0;
                int value2 = 0;

                bool aIsInt = a != null && int.TryParse(a, out value1);
                bool bIsInt = b != null && int.TryParse(b, out value2);

                int result;

                if (!aIsInt && !bIsInt)
                {
                    // Both are strings
                    result = string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
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
