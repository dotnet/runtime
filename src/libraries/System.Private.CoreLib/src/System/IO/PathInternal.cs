// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.IO
{
    /// <summary>Contains internal path helpers that are shared between many projects.</summary>
    internal static partial class PathInternal
    {
        /// <summary>
        /// Returns true if the path starts in a directory separator.
        /// </summary>
        internal static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path) => path.Length > 0 && IsDirectorySeparator(path[0]);

#if MS_IO_REDIST
        internal static string EnsureTrailingSeparator(string path)
            => EndsInDirectorySeparator(path) ? path : path + DirectorySeparatorCharAsString;
#else
        internal static string EnsureTrailingSeparator(string path)
            => EndsInDirectorySeparator(path.AsSpan()) ? path : path + DirectorySeparatorCharAsString;
#endif

        internal static bool IsRoot(ReadOnlySpan<char> path)
            => path.Length == GetRootLength(path);

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
        internal static unsafe int EqualStartingCharacterCount(string? first, string? second, bool ignoreCase)
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
                    && (*l == *r || (ignoreCase && char.ToUpperInvariant(*l) == char.ToUpperInvariant(*r))))
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
        internal static bool AreRootsEqual(string? first, string? second, StringComparison comparisonType)
        {
            int firstRootLength = GetRootLength(first.AsSpan());
            int secondRootLength = GetRootLength(second.AsSpan());

            return firstRootLength == secondRootLength
                && string.Compare(
                    strA: first,
                    indexA: 0,
                    strB: second,
                    indexB: 0,
                    length: firstRootLength,
                    comparisonType: comparisonType) == 0;
        }

        /// <summary>
        /// Trims one trailing directory separator beyond the root of the path.
        /// </summary>
        [return: NotNullIfNotNull("path")]
        internal static string? TrimEndingDirectorySeparator(string? path) =>
            EndsInDirectorySeparator(path) && !IsRoot(path.AsSpan()) ?
                path!.Substring(0, path.Length - 1) :
                path;

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        internal static bool EndsInDirectorySeparator(string? path) =>
              !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);

        /// <summary>
        /// Trims one trailing directory separator beyond the root of the path.
        /// </summary>
        internal static ReadOnlySpan<char> TrimEndingDirectorySeparator(ReadOnlySpan<char> path) =>
            EndsInDirectorySeparator(path) && !IsRoot(path) ?
                path.Slice(0, path.Length - 1) :
                path;

        /// <summary>
        /// Returns true if the path ends in a directory separator.
        /// </summary>
        internal static bool EndsInDirectorySeparator(ReadOnlySpan<char> path) =>
            path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
    }
}
