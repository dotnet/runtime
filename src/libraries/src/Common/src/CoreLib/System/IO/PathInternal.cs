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
        /// Returns true if the path starts in a directory separator.
        /// </summary>
        internal static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path) => path.Length > 0 && IsDirectorySeparator(path[0]);

#if MS_IO_REDIST
        internal static string EnsureTrailingSeparator(string path)
            => EndsInDirectorySeparator(path) ? path : path + DirectorySeparatorCharAsString;

        internal static bool EndsInDirectorySeparator(string path)
            => !string.IsNullOrEmpty(path) && IsDirectorySeparator(path[path.Length - 1]);
#else
        internal static string EnsureTrailingSeparator(string path)
            => Path.EndsInDirectorySeparator(path.AsSpan()) ? path : path + DirectorySeparatorCharAsString;
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
        /// Try to remove relative segments from the given path (without combining with a root).
        /// </summary>
        /// <param name="path">Input path</param>
        /// <param name="rootLength">The length of the root of the given path</param>
        internal static string RemoveRelativeSegments(string path, int rootLength)
        {
            Span<char> initialBuffer = stackalloc char[260 /* PathInternal.MaxShortPath */];
            ValueStringBuilder sb = new ValueStringBuilder(initialBuffer);

            if (RemoveRelativeSegments(path.AsSpan(), rootLength, ref sb))
            {
                path = sb.ToString();
            }

            sb.Dispose();
            return path;
        }

        /// <summary>
        /// Try to remove relative segments from the given path (without combining with a root).
        /// </summary>
        /// <param name="path">Input path</param>
        /// <param name="rootLength">The length of the root of the given path</param>
        /// <param name="sb">String builder that will store the result</param>
        /// <returns>"true" if the path was modified</returns>
        internal static bool RemoveRelativeSegments(ReadOnlySpan<char> path, int rootLength, ref ValueStringBuilder sb)
        {
            Debug.Assert(rootLength > 0);
            bool flippedSeparator = false;

            int skip = rootLength;
            // We treat "\.." , "\." and "\\" as a relative segment. We want to collapse the first separator past the root presuming
            // the root actually ends in a separator. Otherwise the first segment for RemoveRelativeSegments
            // in cases like "\\?\C:\.\" and "\\?\C:\..\", the first segment after the root will be ".\" and "..\" which is not considered as a relative segment and hence not be removed.
            if (PathInternal.IsDirectorySeparator(path[skip - 1]))
                skip--;

            // Remove "//", "/./", and "/../" from the path by copying each character to the output, 
            // except the ones we're removing, such that the builder contains the normalized path 
            // at the end.
            if (skip > 0)
            {
                sb.Append(path.Slice(0, skip));
            }

            for (int i = skip; i < path.Length; i++)
            {
                char c = path[i];

                if (PathInternal.IsDirectorySeparator(c) && i + 1 < path.Length)
                {
                    // Skip this character if it's a directory separator and if the next character is, too,
                    // e.g. "parent//child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(path[i + 1]))
                    {
                        continue;
                    }

                    // Skip this character and the next if it's referring to the current directory,
                    // e.g. "parent/./child" => "parent/child"
                    if ((i + 2 == path.Length || PathInternal.IsDirectorySeparator(path[i + 2])) &&
                        path[i + 1] == '.')
                    {
                        i++;
                        continue;
                    }

                    // Skip this character and the next two if it's referring to the parent directory,
                    // e.g. "parent/child/../grandchild" => "parent/grandchild"
                    if (i + 2 < path.Length &&
                        (i + 3 == path.Length || PathInternal.IsDirectorySeparator(path[i + 3])) &&
                        path[i + 1] == '.' && path[i + 2] == '.')
                    {
                        // Unwind back to the last slash (and if there isn't one, clear out everything).
                        int s;
                        for (s = sb.Length - 1; s >= skip; s--)
                        {
                            if (PathInternal.IsDirectorySeparator(sb[s]))
                            {
                                sb.Length = (i + 3 >= path.Length && s == skip) ? s + 1 : s; // to avoid removing the complete "\tmp\" segment in cases like \\?\C:\tmp\..\, C:\tmp\..
                                break;
                            }
                        }
                        if (s < skip)
                        {
                            sb.Length = skip;
                        }

                        i += 2;
                        continue;
                   }
                }

                // Normalize the directory separator if needed
                if (c != PathInternal.DirectorySeparatorChar && c == PathInternal.AltDirectorySeparatorChar)
                {
                    c = PathInternal.DirectorySeparatorChar;
                    flippedSeparator = true;
                }

                sb.Append(c);
            }

            // If we haven't changed the source path, return the original
            if (!flippedSeparator && sb.Length == path.Length)
            {
                return false;
            }

            // We may have eaten the trailing separator from the root when we started and not replaced it
            if (skip != rootLength && sb.Length < rootLength)
            {
                sb.Append(path[rootLength - 1]);
            }

            return true;
        }
    }
}
