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

            // Paths that begin with "\??\" or "\\?\" are by definition normalized and should not get segments removed
            // If the user wants segments removed, they can pass a sliced span of the string that excludes the prefix
            if (PathInternal.IsExtended(originalPath))
            {
                return false;
            }

            // GetRootLength can calculate the total root that includes a device/extended prefix and
            // also unqualified roots like a rootless drive + any folder after that
            int charsToSkip = PathInternal.GetRootLength(originalPath);
            bool flippedSeparator = false;

            bool isDeviceUNCPath = PathInternal.IsDeviceUNC(originalPath);
            bool isDevicePath = PathInternal.IsDevice(originalPath);

            int prefixLength = isDevicePath ? PathInternal.DevicePrefixLength : 0;
            ReadOnlySpan<char> pathWithoutPrefix = originalPath.Slice(prefixLength);

            // In a path like "\\.\C:..\folder\subfolder\file.txt":
            // - PathInternal.GetRootLength(originalPath) will return "\\.\C:..\", which includes the extra redundant segment.
            // - PathInternal.GetRootLength(pathWithoutPrefix) will return "C:", which will help to determine later if a path was rooted or not.
            int prefixAndRootLength = prefixLength + PathInternal.GetRootLength(pathWithoutPrefix);

            // In a path like "\\.\C:..\folder\subfolder\file.txt":
            // - PathInternal.GetRootLength(originalPath) will return "\\.\C:..\", which includes the extra redundant segment.
            // - PathInternal.GetRootLength(pathWithoutPrefix) will return "C:", which will help to determine later if a path was rooted or not.
            int prefixAndRootLength = prefixLength + PathInternal.GetRootLength(pathWithoutPrefix);

            // Append characters that should not be touched:
            // - "C:" - known drive, unknown root
            // - "\" - known root, unknown drive
            // - "C:\" - known root, known drive
            // - "\\.\UNC\Server\Share\" (or any device prefix)
            // - "\\Server\Share\"
            // The chars to skip may also include additional segments that GetRootLength considers part of the root.
            if (charsToSkip > 0)
            {
                flippedSeparator |= AppendWithFlippedSeparators(originalPath.Slice(0, charsToSkip), ref sb);

                // Special case: UNC paths exclude the trailing separator, make sure to consider it as part of the root,
                // that way we ensure initial trailing separators are removed in the main loop
                if (originalPath.Length > charsToSkip && PathInternal.IsDirectorySeparator(originalPath[charsToSkip]))
                {
                    if (sb.Length > 0 && !PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                    {
                        flippedSeparator |= AppendWithFlippedSeparators(originalPath[charsToSkip], ref sb);
                    }
                    charsToSkip++;
                    prefixAndRootLength++;
                }
            }


            // For the string after the device prefix, if any, save if the path is rooted,
            // so we determine if double dots should be accumulated at the beginning or not.
            // For example:
            // - "\\Server\Share\" should be considered rooted
            // - "\\.\UNC\Anything" should be considered rooted
            // - "\\.\C:Users" is not rooted (separator missing after drive)
            // - "\Users" is rooted (even if drive is unknown)
            // - "\\.\C:..\ is not rooted"
            int totalDots;
            bool isRootKnown = isDeviceUNCPath ||
                                (sb.Length > 0 &&
                                 sb.Length >= prefixAndRootLength &&
                                 PathInternal.IsDirectorySeparator(sb[prefixAndRootLength - 1]));

            // Make sure we analyze a string that begins in a separator so we ensure we can remove trailing separators after it
            ReadOnlySpan<char> path = originalPath.Slice(isRootKnown ? charsToSkip - 1 : charsToSkip);

            char c;
            for (int currPos = 0; currPos < path.Length; currPos++)
            {
                c = path[currPos];

                bool isSeparator = PathInternal.IsDirectorySeparator(c);

                if (isSeparator && currPos + 1 < path.Length)
                {
                    // Skip repeated separators, take only the last one.
                    // e.g. "parent//child" => "parent/child", or "parent/////child" => "parent/child"
                    if (PathInternal.IsDirectorySeparator(path[currPos + 1]))
                    {
                        continue;
                    }

                    // Handle redundant segments
                    if (IsNextSegmentOnlyDots(path, currPos, out totalDots))
                    {
                        Debug.Assert(totalDots > 0);

                        // Skip the next segment if it's a single dot (current directory).
                        // Only keep it if it's at the beginning of the unqualified path.
                        // e.g. "parent/./child" => "parent/child", or "parent/." => "parent/" or "./other" => "other"
                        // or "./folder" => "./folder/, or "././folder" => "./folder"
                        if (totalDots == 1)
                        {
                            // The only case where we add it is if we are at the beginning and the root is not ""
                            if (!isRootKnown && sb.Length == 0)
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
                            if (!TryBacktrackToPreviousSeparator(currPos, charsToSkip, isRootKnown, ref sb))
                            {
                                // Before appending ".." we need to check if a separator needs to be added first:
                                // e.g. "..\.\.." => "..\.."  or  "\..\.\.." => "\..\.."  or  "C:path\..\.." => "C:.."
                                if ((sb.Length > charsToSkip) && !PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                                {
                                    flippedSeparator |= AppendWithFlippedSeparators(path[currPos], ref sb);
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
                // - "C:." => "C:."
                // - "C:\." => "C:\"
                // - "//./C:folder/." => "//./C:/folder/"
                if (currPos == 0 && !isRootKnown && c == '.' && sb.Length > prefixAndRootLength && (currPos + 1 >= path.Length || PathInternal.IsDirectorySeparator(path[currPos + 1])))
                {
                    continue;
                }

                // Normalize the directory separator if needed
                if (isSeparator)
                {
                    if (sb.Length > charsToSkip && !PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                    {
                        // Only remove trailing dots from paths without a device prefix
                        if (!isDevicePath)
                        {
                            TryRemoveTrailingDotsFromPreviousSegment(charsToSkip, ref sb);
                        }
                        flippedSeparator |= AppendWithFlippedSeparators(path[currPos], ref sb);
                    }
                }
                // Append all other path characters
                else
                {
                    sb.Append(c);
                }
            }

            // Final adjustments:

            // Paths without a device prefix cannot have trailing dots
            if (!isDevicePath && sb.Length > charsToSkip)
            {
                // Final segments with 3 or more dots, which do not end in a separator, need to get removed
                if (!PathInternal.IsDirectorySeparator(sb[sb.Length - 1]) &&
                    IsPreviousSegmentOnlyDots(sb.AsSpan(), charsToSkip, sb.Length - 1, out totalDots) &&
                    totalDots > 2)
                {
                    TryBacktrackToPreviousSeparator(sb.Length - 1, charsToSkip, isRootKnown, ref sb);
                    // to keep this case consistent with Windows, make sure to re-add the separator
                    if (sb.Length > charsToSkip && !PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                    {
                        sb.Append(PathInternal.DirectorySeparatorChar);
                    }
                }
                // Final segments that have trailing dots need the dots removed
                TryRemoveTrailingDotsFromPreviousSegment(charsToSkip, ref sb);
            }

            // If the buffer contained information, but the path finished with a separator, the
            // separator may have been added, but we should never return a single separator for unqualified paths.
            // e.g. "folder/../" => ""
            if (!isRootKnown && path.Length > 0 && sb.Length == 1 && PathInternal.IsDirectorySeparator(sb[0]))
            {
                sb.Length = 0;
            }

            return flippedSeparator || sb.Length < originalPath.Length;
        }

        // Checks if there is a previous segment and it ends with dots, then removes them.
        private static void TryRemoveTrailingDotsFromPreviousSegment(int charsToSkip, ref ValueStringBuilder sb)
        {
            if (sb.Length == 0)
            {
                return;
            }

            bool separator = PathInternal.IsDirectorySeparator(sb[sb.Length - 1]);
            if (separator)
            {
                // "x/" or "/x/" exit early
                if (sb.Length - 2 >= 0 && !PathInternal.IsDirectorySeparator(sb[sb.Length - 2]))
                {
                    // Temporarily remove it
                    sb.Length -= 1;
                }
                else
                {
                    return;
                }
            }

            int i = sb.Length - 1;

            int trailingDots = 0;
            while (i > charsToSkip && i >= 0 && !PathInternal.IsDirectorySeparator(sb[i]))
            {
                if (sb[i] == '.')
                {
                    trailingDots++;
                }
                // If anything other than a dot is found, we finish
                // The loop already knows to exit if a dir separator is found, to prevent removing dot-only segments
                else
                {
                    if (trailingDots > 0)
                    {
                        sb.Length -= trailingDots;
                    }
                    break;
                }
                i--;
            }

            // Place back the trailing separator
            if (separator)
            {
                sb.Append(PathInternal.DirectorySeparatorChar);
            }
        }

        // Checks if there is a previous segment and if it consists of only dots
        private static bool IsPreviousSegmentOnlyDots(ReadOnlySpan<char> fullPath, int charsToSkip, int currPos, out int totalDots)
        {
            totalDots = 0;
            if (currPos < charsToSkip)
            {
                return false;
            }

            int pos = currPos;
            if (PathInternal.IsDirectorySeparator(fullPath[pos]))
            {
                pos -= 1;
            }

            while (pos >= charsToSkip && !PathInternal.IsDirectorySeparator(fullPath[pos]))
            {
                if (fullPath[pos] != '.')
                {
                    totalDots = 0;
                    return false;
                }
                pos--;
                totalDots++;
            }
            return totalDots > 0;
        }
    }
}
