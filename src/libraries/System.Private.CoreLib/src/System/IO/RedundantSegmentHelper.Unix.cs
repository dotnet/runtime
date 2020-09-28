// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Text;

namespace System.IO
{
    internal static partial class RedundantSegmentHelper
    {
        // Attempts to remove redundant segments from a path.
        // Redundant segments are: ".", ".." or duplicate directory separators.
        // Returns true if the original path was modified.
        internal static bool TryRemoveRedundantSegments(ReadOnlySpan<char> originalPath, ref ValueStringBuilder sb)
        {
            Debug.Assert(originalPath.Length > 0);

            bool flippedSeparator = false;
            int rootLength = PathInternal.GetRootLength(originalPath);

            // Append characters that should not be touched
            if (rootLength == 1)
            {
                flippedSeparator |= AppendWithFlippedSeparators(originalPath[0], ref sb);
            }

            char c;
            // Start analysis at zero to ensure duplicate separators get skipped
            for (int currPos = 0; currPos < originalPath.Length; currPos++)
            {
                c = originalPath[currPos];

                bool isSeparator = PathInternal.IsDirectorySeparator(c);

                if (isSeparator && currPos + 1 < originalPath.Length)
                {
                    // Skip repeated separators, take only the last one.
                    // e.g. "parent//child" => "parent/child", or "parent/////child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(originalPath[currPos + 1]))
                    {
                        continue;
                    }

                    // Handle redundant segments
                    if (IsNextSegmentOnlyDots(originalPath, currPos, out int totalDots))
                    {
                        Debug.Assert(totalDots > 0);

                        // Skip the next segment if it's a single dot (current directory).
                        // Only keep it if it's at the beginning of the unqualified path.
                        // e.g. "parent/./child" => "parent/child", or "parent/." => "parent/" or "./other" => "other"
                        // or "./folder" => "./folder/, or "././folder" => "./folder"
                        if (totalDots == 1)
                        {
                            // The only case where we add it is if we are at the beginning and the root is not ""
                            if (rootLength == 0 && sb.Length == 0)
                            {
                                sb.Append('.');
                            }
                            currPos++;
                            continue;
                        }
                        // Skip the next segment if it's a double dot (backtrack to parent directory).
                        // e.g. "parent/child/../grandchild" => "parent/grandchild"
                        else if (totalDots == 2)
                        {
                            // Unqualified paths need to check if there is a valid folder segment before reaching the beginning and root is not "\"
                            // Fully qualified paths should succeed to backtrack, even when reaching the root
                            if (!TryBacktrackToPreviousSeparator(currPos, rootLength, rootLength > 0, ref sb))
                            {
                                // Before appending ".." we need to check if a separator needs to be added first:
                                // e.g. "..\.\.." => "..\.."  or  "\..\.\.." => "\..\.."  or  "path\..\.." => ".."
                                if ((sb.Length > rootLength) && !PathInternal.IsDirectorySeparator(sb[^1]))
                                {
                                    flippedSeparator |= AppendWithFlippedSeparators(originalPath[currPos], ref sb);
                                }
                                // Add the double dots
                                sb.Append("..");
                            }
                            currPos += 2;
                            continue;
                        }
                    }
                }

                // special case that can only happen at the beginning: do not add the dot if path is rooted
                // - "./" => "./"
                // - "/." -> "/"
                // - "." => "."
                // - "/folder/." => "/folder/"
                if (currPos == 0 && rootLength == 0 && c == '.' && sb.Length > rootLength &&
                   (currPos + 1 >= originalPath.Length || PathInternal.IsDirectorySeparator(originalPath[currPos + 1])))
                {
                    continue;
                }

                // Normalize the directory separator if needed
                if (isSeparator)
                {
                    if (sb.Length > rootLength && !PathInternal.IsDirectorySeparator(sb[^1]))
                    {
                        flippedSeparator |= AppendWithFlippedSeparators(originalPath[currPos], ref sb);
                    }
                }
                // Append all other path characters
                else
                {
                    sb.Append(c);
                }
            }

            // Final adjustments:

            // If the buffer contained information, but the path finished with a separator, the
            // separator may have been added, but we should never return a single separator for unqualified paths.
            // e.g. "folder/../" => ""
            if (rootLength == 0 && originalPath.Length > 0 && sb.Length == 1 && PathInternal.IsDirectorySeparator(sb[0]))
            {
                sb.Length = 0;
            }

            return flippedSeparator || sb.Length < originalPath.Length;
        }
    }
}
