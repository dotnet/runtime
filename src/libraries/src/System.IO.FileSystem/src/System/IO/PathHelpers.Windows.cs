// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO
{
    internal static partial class PathHelpers
    {
        // Trim trailing whitespace, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // string.WhitespaceChars will trim more aggressively than what the underlying FS does (for ex, NTFS, FAT).
        internal static readonly char[] TrimEndChars = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };
        internal static readonly char[] TrimStartChars = { ' ' };

        internal static bool ShouldReviseDirectoryPathToCurrent(string path)
        {
            // In situations where this method is invoked, "<DriveLetter>:" should be special-cased
            // to instead go to the current directory.
            return path.Length == 2 && path[1] == ':';
        }

        // ".." can only be used if it is specified as a part of a valid File/Directory name. We disallow
        //  the user being able to use it to move up directories. Here are some examples eg
        //    Valid: a..b  abc..d
        //    Invalid: ..ab   ab..  ..   abc..d\abc..
        //
        internal static void CheckSearchPattern(string searchPattern)
        {
            for (int index = 0; (index = searchPattern.IndexOf("..", index, StringComparison.Ordinal)) != -1; index += 2)
            {
                // Terminal ".." or "..\". File and directory names cannot end in "..".
                if (index + 2 == searchPattern.Length ||
                    PathInternal.IsDirectorySeparator(searchPattern[index + 2]))
                {
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern, nameof(searchPattern));
                }
            }
        }

        internal static string NormalizeSearchPattern(string searchPattern)
        {
            Debug.Assert(searchPattern != null);

            // Win32 normalization trims only U+0020.
            string tempSearchPattern = searchPattern.TrimEnd(PathHelpers.TrimEndChars);

            // Make this corner case more useful, like dir
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        internal static string GetFullSearchString(string fullPath, string searchPattern)
        {
            Debug.Assert(fullPath != null);
            Debug.Assert(searchPattern != null);

            ThrowIfEmptyOrRootedPath(searchPattern);
            string tempStr = Path.Combine(fullPath, searchPattern);

            // If path ends in a trailing slash (\), append a * or we'll get a "Cannot find the file specified" exception
            char lastChar = tempStr[tempStr.Length - 1];
            if (PathInternal.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + "*";
            }

            return tempStr;
        }

        internal static string TrimEndingDirectorySeparator(string path) =>
            EndsInDirectorySeparator(path) ?
                path.Substring(0, path.Length - 1) :
                path;
    }
}
