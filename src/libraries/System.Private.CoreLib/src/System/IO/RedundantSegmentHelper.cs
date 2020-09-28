// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Diagnostics;
using System.Text;

namespace System.IO
{
    internal static partial class RedundantSegmentHelper
    {
        // Checks if the previous segment consists of only one dot and it's the first segment after the root
        // currPos is expected to be at a separator
        private static bool IsPreviousSegmentFirstSingleDot(ReadOnlySpan<char> fullPath, int charsToSkip, int currPos)
        {
            return currPos - 1 >= charsToSkip &&
                   fullPath[currPos - 1] == '.' &&
                   currPos - 2 < charsToSkip;
        }

        // Checks if the segment before the specified position in the path is a ".." segment.
        // currPos is expected to be located at a separator.
        private static bool IsPreviousSegmentDoubleDot(ReadOnlySpan<char> path, int rootLength, int currPos)
        {
            return currPos - 1 > rootLength && path[currPos - 1] == '.' &&
                    currPos - 2 >= rootLength && path[currPos - 2] == '.' &&
                    (currPos - 3 < rootLength || PathInternal.IsDirectorySeparator(path[currPos - 3]));
        }

        // Checks if the next segment consists of only dots
        private static bool IsNextSegmentOnlyDots(ReadOnlySpan<char> path, int currPos, out int totalDots)
        {
            totalDots = 0;
            if (currPos >= path.Length - 1)
            {
                return false;
            }
            currPos += 1; // Because path[currPos] is a separator
            while (currPos < path.Length && !PathInternal.IsDirectorySeparator(path[currPos]))
            {
                if (path[currPos] != '.')
                {
                    totalDots = 0;
                    return false;
                }
                currPos++;
                totalDots++;
            }
            return totalDots > 0;
        }

        // If the character is a directory separator, ensure it is set to the current operating system's character.
        private static bool TryNormalizeSeparatorCharacter(ref char c)
        {
            if (c != PathInternal.DirectorySeparatorChar && c == PathInternal.AltDirectorySeparatorChar)
            {
                c = PathInternal.DirectorySeparatorChar;
                return true;
            }
            return false;
        }

        // Inserts the specified character into the string builder
        private static bool AppendWithFlippedSeparators(char c, ref ValueStringBuilder sb)
        {
            bool flippedSeparator = TryNormalizeSeparatorCharacter(ref c);
            sb.Append(c);
            return flippedSeparator;
        }

        // Inserts the characters of the specified span into the string builder
        private static bool AppendWithFlippedSeparators(ReadOnlySpan<char> value, ref ValueStringBuilder sb)
        {
            bool flippedSeparator = false;
            for (int i = 0; i < value.Length; i++)
            {
                flippedSeparator |= AppendWithFlippedSeparators(value[i], ref sb);
            }
            return flippedSeparator;
        }

        // Adjusts the length of the buffer to the position of the previous valid directory separator due to a "..".
        // So if the previous segment is "..", it means it wasn't processed in a previous loop on purpose
        // due to the path being unqualified up to the current position, in which case we do nothing.
        // e.g. "../.." => "../.."  or  "../../folder/../../" => "../../../"
        // The previous segment is a backtrackable segment if it's a folder name.
        // e.g. "folder/.." => ""  or  "folder/folder/./../" => "folder/"
        private static bool TryBacktrackToPreviousSeparator(int currPos, int charsToSkip, bool isRootedWithSeparator, ref ValueStringBuilder sb)
        {
            // Special case: when nothing has been added and the root consists of "/", assume the ".." was successfully backtracked because it's a valid root
            if (sb.Length == charsToSkip && isRootedWithSeparator)
            {
                return true;
            }
            else if (sb.Length > charsToSkip &&
                !IsPreviousSegmentDoubleDot(sb.AsSpan(), charsToSkip, sb.Length) &&
                !IsPreviousSegmentFirstSingleDot(sb.AsSpan(), charsToSkip, sb.Length))
            {
                // Remove the separator
                if (PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                {
                    sb.Length--;
                }
                // Backtrack until reaching any limit
                while (sb.Length > charsToSkip && !PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                {
                    sb.Length--;
                }

                // Remove the previous separator, it may not be re-added outside
                if (sb.Length > charsToSkip && PathInternal.IsDirectorySeparator(sb[sb.Length - 1]))
                {
                    sb.Length--;
                }

                return true;
            }

            return false;
        }
    }
}
