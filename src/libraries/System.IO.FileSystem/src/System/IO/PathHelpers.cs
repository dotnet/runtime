// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.IO
{
    // Many of the helper methods in System.IO.Path are internal to mscorlib.
    // 
    // These members are taken from src\NDP\clr\src\BCL\System\IO\Path.cs
    // where an appropriate public member to port to does not exist, or has
    // different behavior we cannot use for application compat.
    internal static class PathHelpers
    {
        // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // String.WhitespaceChars will trim more aggressively than what the underlying FS does (for ex, NTFS, FAT).    
        internal static readonly char[] TrimEndChars = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };
        internal static readonly char[] TrimStartChars = { ' ' };

        // Array of the separator chars
        internal static readonly char[] DirectorySeparatorChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        // String-representation of the directory-separator character, used when appending the character to another
        // string so as to avoid the boxing of the character when calling String.Concat(..., object).
        internal static readonly string DirectorySeparatorCharAsString = new string(Path.DirectorySeparatorChar, 1);

        // Gets the length of the root DirectoryInfo or whatever DirectoryInfo markers
        // are specified for the first part of the DirectoryInfo name.
        // 
        internal static int GetRootLength(String path)
        {
            CheckInvalidPathChars(path);

            int i = 0;
            int length = path.Length;

            if (length >= 1 && (IsDirectorySeparator(path[0])))
            {
                // handles UNC names and directories off current drive's root.
                i = 1;
                if (length >= 2 && (IsDirectorySeparator(path[1])))
                {
                    i = 2;
                    int n = 2;
                    while (i < length && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
                }
            }
            else if (length >= 2 && path[1] == Path.VolumeSeparatorChar)
            {
                // handles A:\foo.
                i = 2;
                if (length >= 3 && (IsDirectorySeparator(path[2]))) i++;
            }
            return i;
        }

        // ".." can only be used if it is specified as a part of a valid File/Directory name. We disallow
        //  the user being able to use it to move up directories. Here are some examples eg 
        //    Valid: a..b  abc..d
        //    Invalid: ..ab   ab..  ..   abc..d\abc..
        //
        internal static void CheckSearchPattern(String searchPattern)
        {
            int index;
            while ((index = searchPattern.IndexOf("..", StringComparison.Ordinal)) != -1)
            {
                if (index + 2 == searchPattern.Length) // Terminal ".." . Files names cannot end in ".."
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern);

                if (IsDirectorySeparator(searchPattern[index + 2]))
                    throw new ArgumentException(SR.Arg_InvalidSearchPattern);

                searchPattern = searchPattern.Substring(index + 2);
            }
        }

        internal static void CheckInvalidPathChars(String path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (PathInternal.HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(SR.Argument_InvalidPathChars);
        }

        // System.IO.Path has both public Combine and internal InternalCombine
        // members.  InternalCombine performs these extra validations on the second 
        // argument.  This provides a convenient helper to maintain this extra
        // validation when porting code from Path.InternalCombine to Path.Combine.
        internal static void ThrowIfEmptyOrRootedPath(string path2)
        {
            if (path2 == null)
                throw new ArgumentNullException("path2");
            if (path2.Length == 0)
                throw new ArgumentException(SR.Argument_PathEmpty, "path2");
            if (Path.IsPathRooted(path2))
                throw new ArgumentException(SR.Arg_Path2IsRooted, "path2");
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
        }

        internal static bool EndsInDirectorySeparator(String path)
        {
            return path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
        }

        internal static string GetFullPathInternal(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            string pathTrimmed = path.TrimStart(TrimStartChars).TrimEnd(TrimEndChars);

            return Path.GetFullPath(Path.IsPathRooted(pathTrimmed) ? pathTrimmed : path);
        }

        // this is a lightweight version of GetDirectoryName that doesn't renormalize
        internal static string GetDirectoryNameInternal(string path)
        {
            string directory, file;
            SplitDirectoryFile(path, out directory, out file);

            // file is null when we reach the root
            return (file == null) ? null : directory;
        }

        internal static void SplitDirectoryFile(string path, out string directory, out string file)
        {
            directory = null;
            file = null;

            // assumes a validated full path
            if (path != null)
            {
                int length = path.Length;
                int rootLength = GetRootLength(path);

                // ignore a trailing slash
                if (length > rootLength && EndsInDirectorySeparator(path))
                    length--;

                // find the pivot index between end of string and root
                for (int pivot = length - 1; pivot >= rootLength; pivot--)
                {
                    if (IsDirectorySeparator(path[pivot]))
                    {
                        directory = path.Substring(0, pivot);
                        file = path.Substring(pivot + 1, length - pivot - 1);
                        return;
                    }
                }

                // no pivot, return just the trimmed directory
                directory = path.Substring(0, length);
            }

            return;
        }
    }
}
