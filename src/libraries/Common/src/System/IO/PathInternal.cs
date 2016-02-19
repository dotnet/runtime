// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        /// <summary>
        /// Checks for invalid path characters in the given path.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown if the path is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if the path has invalid characters.</exception>
        /// <param name="path">The path to check for invalid characters.</param>
        internal static void CheckInvalidPathChars(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (PathInternal.HasIllegalCharacters(path))
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));
        }


        /// <summary>
        /// Returns true if the given StringBuilder starts with the given value.
        /// </summary>
        /// <param name="value">The string to compare against the start of the StringBuilder.</param>
        internal static bool StartsWithOrdinal(this StringBuilder builder, string value)
        {
            if (value == null || builder.Length < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (builder[i] != value[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns true if the given string starts with the given value.
        /// </summary>
        /// <param name="value">The string to compare against the start of the source string.</param>
        internal static bool StartsWithOrdinal(this string source, string value)
        {
            if (value == null || source.Length < value.Length)
                return false;

            return source.StartsWith(value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Trims the specified characters from the end of the StringBuilder.
        /// </summary>
        internal static StringBuilder TrimEnd(this StringBuilder builder, params char[] trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
                return builder;

            int end = builder.Length - 1;

            for (; end >= 0; end--)
            {
                int i = 0;
                char ch = builder[end];
                for (; i < trimChars.Length; i++)
                {
                    if (trimChars[i] == ch) break;
                }
                if (i == trimChars.Length)
                {
                    // Not a trim char
                    break;
                }
            }

            builder.Length = end + 1;
            return builder;
        }
    }
}
