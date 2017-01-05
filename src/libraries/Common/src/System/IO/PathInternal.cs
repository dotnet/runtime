// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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

            if (HasIllegalCharacters(path))
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
        
        /// <summary>
        /// Returns the start index of the filename
        /// in the given path, or 0 if no directory
        /// or volume separator is found.
        /// </summary>
        /// <param name="path">The path in which to find the index of the filename.</param>
        /// <remarks>
        /// This method returns path.Length for
        /// inputs like "/usr/foo/" on Unix. As such,
        /// it is not safe for being used to index
        /// the string without additional verification.
        /// </remarks>
        internal static int FindFileNameIndex(string path)
        {
            Debug.Assert(path != null);
            CheckInvalidPathChars(path);

            for (int i = path.Length - 1; i >= 0; i--)
            {
                char ch = path[i];
                if (IsDirectoryOrVolumeSeparator(ch))
                    return i + 1;
            }

            return 0; // the whole path is the filename
        }

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        internal static bool EndsInDirectorySeparator(string path) =>
            !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);

        /// <summary>
        /// Get the common path length from the start of the string.
        /// </summary>
        internal static int GetCommonPathLength(string first, string second, bool ignoreCase)
        {
            int commonChars = EqualStartingCharacterCount(first, second, ignoreCase: ignoreCase);

            // If nothing matches
            if (commonChars == 0)
                return commonChars;

            // Or we're a full string and equal length or match to a separator
            if (commonChars == first.Length
                && (commonChars == second.Length || IsDirectorySeparator(second[commonChars])))
                return commonChars;

            if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
                return commonChars;

            // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
            while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
                commonChars--;

            return commonChars;
        }

        /// <summary>
        /// Gets the count of common characters from the left optionally ignoring case
        /// </summary>
        internal static unsafe int EqualStartingCharacterCount(string first, string second, bool ignoreCase)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

            int commonChars = 0;

            fixed (char* f = first)
            fixed (char* s = second)
            {
                char* l = f;
                char* r = s;
                char* leftEnd = l + first.Length;
                char* rightEnd = r + second.Length;

                while (l != leftEnd && r != rightEnd
                    && (*l == *r || (ignoreCase && char.ToUpperInvariant((*l)) == char.ToUpperInvariant((*r)))))
                {
                    commonChars++;
                    l++;
                    r++;
                }
            }

            return commonChars;
        }

        /// <summary>
        /// Returns true if the two paths have the same root
        /// </summary>
        internal static bool AreRootsEqual(string first, string second, StringComparison comparisonType)
        {
            int firstRootLength = GetRootLength(first);
            int secondRootLength = GetRootLength(second);

            return firstRootLength == secondRootLength
                && string.Compare(
                    strA: first,
                    indexA: 0, 
                    strB: second,
                    indexB: 0,
                    length: firstRootLength,
                    comparisonType: comparisonType) == 0;
        }
    }
}
