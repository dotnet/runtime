// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <summary>
        /// Tries to remove relative segments from the given path, starting the analysis at the specified location.
        /// </summary>
        /// <param name="path">The input path.</param>
        /// <param name="sb">A reference to a value string builder that will store the result.</param>
        /// <returns><see langword="true" /> if the path was modified; <see langword="false" /> otherwise.</returns>
        internal static bool TryRemoveRedundantSegments(ReadOnlySpan<char> path, ref ValueStringBuilder sb)
        {
            Debug.Assert(path.Length > 0);

            ReadOnlySpan<char> root = Path.GetPathRoot(path);
            int charsToSkip = root.Length;

            bool isFullyQualified = Path.IsPathFullyQualified(path);
            bool flippedSeparator = false;

            char c;

            // Remove "//", "/./", and "/../" from the path by copying each character to the output, except the ones we're removing,
            // such that the builder contains the normalized path at the end.
            if (charsToSkip > 0)
            {
                // We treat "\.." , "\." and "\\" as a redundant segment.
                // We want to collapse the first separator past the root presuming the root actually ends in a separator.
                // In cases like "\\?\C:\.\" and "\\?\C:\..\", the first segment after the root will be ".\" and "..\" which is not
                // considered as a redundant segment and hence not be removed.
                if (IsDirectorySeparator(path[charsToSkip - 1]))
                {
                    charsToSkip--;
                }

                // Append the root, if any.
                // Normalize its directory separators if needed
                for (int i = 0; i < charsToSkip; i++)
                {
                    c = path[i];
                    flippedSeparator |= TryNormalizeSeparatorCharacter(ref c);
                    sb.Append(c);
                }
            }

            // Iterate the characters after the root, if any.
            for (int currPos = charsToSkip; currPos < path.Length; currPos++)
            {
                c = path[currPos];

                bool isDirectorySeparator = IsDirectorySeparator(c);

                // Normal case: Start analysis of current segment on the separator
                if (isDirectorySeparator && currPos + 1 < path.Length)
                {
                    // Skip repeated separators, take only the last one.
                    // e.g. "parent//child" => "parent/child", or "parent/////child" => "parent/child"
                    if (IsDirectorySeparator(path[currPos + 1]))
                    {
                        continue;
                    }

                    // Skip the next segment if it's a single dot (current directory).
                    // Even if we are at the beginning of a path that is not fully qualified, we always remove these.
                    // e.g. "parent/./child" => "parent/child", or "parent/." => "parent/" or "./other" => "other"
                    if (IsNextSegmentSingleDot(path, currPos))
                    {
                        currPos++;
                        continue;
                    }

                    // Skip the next segment if it's a double dot (backtrack to parent directory).
                    // e.g. "parent/child/../grandchild" => "parent/grandchild"
                    if (IsNextSegmentDoubleDot(path, currPos))
                    {
                        // Double dots can only get removed if we know who the parent is, which is always possible with fully qualified paths.
                        if (isFullyQualified)
                        {
                            TryUnwindBackToPreviousSeparator(ref sb, path, charsToSkip, currPos);
                            currPos += 2;
                            continue;
                        }
                        // Non fully qualified paths need to check if there is a folder segment before reaching position 0.
                        else
                        {
                            // If the previous segment is double dot, it means it wasn't processed in a previous loop
                            // on purpose due to the path being unqualified up to the current position.
                            // Otherwise, it has to be a folder or a single dot (single dots are always backtracked).
                            // e.g. "../known" or "../../known" or "known"
                            if (!IsPreviousSegmentDoubleDot(path, currPos))
                            {
                                // No folder was found behind the current position, only single or double dots. Add the double dots.
                                if (!TryUnwindBackToPreviousSeparator(ref sb, path, charsToSkip, currPos))
                                {
                                    // If the buffer contains data, add a directory separator only if the buffer does not have one already.
                                    // e.g. "..\.\.." => "..\.."
                                    if (sb.Length > 0 && !IsDirectorySeparator(sb[sb.Length - 1]))
                                    {
                                        sb.Append(path[currPos]);
                                        flippedSeparator |= TryNormalizeSeparatorCharacter(ref sb[sb.Length - 1]);
                                    }
                                    // Add the double dots
                                    sb.Append("..");
                                }

                                currPos += 2;
                                continue;
                            }
                        }
                    }
                }
                // Special case: single dot segments at the beginning of the path must be skipped
                else if (charsToSkip == 0 && sb.Length == 0 && IsNextSegmentSingleDot(path, currPos - 1))
                {
                    currPos++;
                    continue;
                }

                // Normalize the directory separator if needed
                if (isDirectorySeparator)
                {
                    flippedSeparator |= TryNormalizeSeparatorCharacter(ref c);
                }

                // Always add the character to the buffer if it's not a directory separator.

                // If it's a directory separator, only append it when:
                // - The path is fully qualified:
                //     e.g. In Unix, a rooted path: "/home/.." => "/"
                // - Is not fully qualified, but the buffer already has content:
                //     e.g. "folder/" => "folder/"
                // - Is not fully qualified, buffer is empty but the very first segment is a double dot:
                //     e.g. "/../folder" => "/../folder"

                // If it's a directory separator, do not append when it's the first character of a sequence with these conditions:
                // - Is not fully qualified, started with actual folders which got removed by double dot segments (buffer is empty), and
                //   has more double dot segments than folders, which would make the double dots reach the beginning of the buffer:
                //     e.g. "folder/../.." => ".." or "folder/folder/../../../" => "../"
                // - Is not fully qualified but is rooted, starts with double dots, or started with actual folders which got removed by
                // double dot segments (buffer is empty), and has more double dot segments than folders, which would make the double dots
                // reach the beginning of the buffer:
                //     e.g. "C:..\System32" => "C:\System32" or "C:System32\..\..\" => "C:..\"
                if (!isDirectorySeparator || isFullyQualified || sb.Length > root.Length ||
                    (IsNextSegmentDoubleDot(path, currPos) && (currPos == 0 || sb.Length > root.Length)))
                {
                    sb.Append(c);
                }
            }

            // If we haven't changed the source path, return the original
            if (!flippedSeparator && sb.Length == path.Length)
            {
                return false;
            }

            // Final adjustments:
            // We may have eaten the trailing separator from the root when we started and not replaced it.
            // Only append the trailing separator if the buffer contained information.
            if (charsToSkip != root.Length && sb.Length > 0)
            {
                if (sb.Length < charsToSkip)
                {
                    sb.Append(path[charsToSkip - 1]);
                }
                // e.g. "C:\." => "C:\" or "\\?\C:\.." => "\\?\C:\"
                else if (sb.Length == charsToSkip && path.Length > charsToSkip && IsDirectorySeparator(path[charsToSkip]))
                {
                    sb.Append(path[charsToSkip]);
                }
            }
            // If the buffer contained information, but the path was not fully qualified and finished with a separator,
            // the separator may have been added, but we should never return a single separator for non-fully qualified paths.
            // e.g. "folder/../" => ""
            else if (!isFullyQualified && sb.Length == 1 && IsDirectorySeparator(sb[0]))
            {
                sb.Length = 0;
            }

            return true;
        }

        // Adjusts the length of the buffer to the position of the previous directory separator due to a double dot.
        private static bool TryUnwindBackToPreviousSeparator(ref ValueStringBuilder sb, ReadOnlySpan<char> path, int charsToSkip, int currPos)
        {
            bool onlyDotsAndSeparators = true;
            for (int pos = currPos; pos >= charsToSkip; pos--)
            {
                if (path[pos] != '.' && !IsDirectorySeparator(path[pos]))
                {
                    onlyDotsAndSeparators = false;
                }
            }
            if (onlyDotsAndSeparators)
            {
                return false;
            }

            int unwindPosition;
            for (unwindPosition = sb.Length - 1; unwindPosition >= charsToSkip; unwindPosition--)
            {
                if (IsDirectorySeparator(sb[unwindPosition]))
                {
                    // Avoid removing the root separator.
                    // e.g. "\\?\C:\tmp\..\" => "\\?\C:\" or "C:\tmp\.." => "C:\" or "C:\.." => "C:\"
                    sb.Length = (currPos + 3 >= path.Length && unwindPosition == charsToSkip) ? unwindPosition + 1 : unwindPosition;
                    break;
                }
            }
            // Never go beyond the root.
            // Or in the case of an unqualified path, if the initial segment was a folder
            // without a separator at the beginning, the resulting string is empty.
            if (unwindPosition < charsToSkip)
            {
                sb.Length = charsToSkip;
            }

            return true;
        }

        // If the character is a directory separator, ensure it is set to the current operating system's character.
        private static bool TryNormalizeSeparatorCharacter(ref char c)
        {
            if (c != DirectorySeparatorChar && c == AltDirectorySeparatorChar)
            {
                c = DirectorySeparatorChar;
                return true;
            }
            return false;
        }

        // Checks if the segment before the specified position in the path is a "/../" segment.
        private static bool IsPreviousSegmentDoubleDot(ReadOnlySpan<char> path, int currPos)
        {
            return currPos == 0 ||
                (currPos - 2 >= 0 && path[currPos - 2] == '.' && path[currPos - 1] == '.');
        }

        // CHecks if the segment after the specified position in the path is a "/../" segment.
        private static bool IsNextSegmentDoubleDot(ReadOnlySpan<char> path, int currPos)
        {
            return currPos + 2 < path.Length &&
                (currPos + 3 == path.Length || IsDirectorySeparator(path[currPos + 3])) &&
                path[currPos + 1] == '.' && path[currPos + 2] == '.';
        }

        // CHecks if the segment after the specified position in the path is a "/./" segment.
        private static bool IsNextSegmentSingleDot(ReadOnlySpan<char> path, int currPos)
        {
            return (currPos + 2 == path.Length || IsDirectorySeparator(path[currPos + 2])) &&
                path[currPos + 1] == '.';
        }
    }
}
