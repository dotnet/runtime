// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    /// <summary>
    /// Wrapper to help with path normalization.
    /// </summary>
    unsafe internal class PathHelper
    {
        // This should cover the vast majority of usages without adding undue memory pressure
        private const int MaxBuilderSize = 1024;

        // Can't be over 8.3 and be a short name
        private const int MaxShortName = 12;

        private const char LastAnsi = (char)255;
        private const char Delete = (char)127;

        // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // string.WhitespaceChars will trim more aggressively than what the underlying FS does (for ex, NTFS, FAT).
        private static readonly char[] s_trimEndChars = { (char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0 };

        // We don't use StringBuilderCache because the cached size is too small and we don't want to collide with other usages.
        [ThreadStatic]
        private static StringBuilder t_cachedOutputBuffer;

        internal string Normalize(string path, bool fullCheck, bool expandShortPaths)
        {
            StringBuilder fullPath = null;
            try
            {
                // Get the full path
                fullPath = this.GetFullPathName(path);

                if (fullCheck)
                {
                    // Trim whitespace off the end of the string. Win32 normalization trims only U+0020.
                    fullPath = fullPath.TrimEnd(s_trimEndChars);
                }

                if (fullPath.Length >= PathInternal.MaxLongPath)
                {
                    // Fullpath is genuinely too long
                    throw new PathTooLongException(SR.IO_PathTooLong);
                }

                // Checking path validity used to happen before getting the full path name. To avoid additional input allocation
                // (to trim trailing whitespace) we now do it after the Win32 call. This will allow legitimate paths through that
                // used to get kicked back (notably segments with invalid characters might get removed via "..").
                //
                // There is no way that GetLongPath can invalidate the path so we'll do this (cheaper) check before we attempt to
                // expand short file names.

                // Scan the path for:
                //
                //  - Illegal path characters.
                //  - Invalid UNC paths like \\, \\server, \\server\.
                //  - Segments that are too long (over MaxComponentLength)

                // As the path could be > 30K, we'll combine the validity scan. None of these checks are performed by the Win32
                // GetFullPathName() API.

                bool possibleShortPath = false;
                bool foundTilde = false;
                bool possibleBadUnc = this.IsUnc(fullPath);
                int index = possibleBadUnc ? 2 : 0;
                int lastSeparator = possibleBadUnc ? 1 : 0;
                int segmentLength;
                char current;

                while (index < fullPath.Length)
                {
                    current = fullPath[index];

                    switch (current)
                    {
                        case '|':
                        case '>':
                        case '<':
                        case '\"':
                            if (fullCheck) throw new ArgumentException(SR.Argument_InvalidPathChars, "path");
                            foundTilde = false;
                            break;
                        case '~':
                            foundTilde = true;
                            break;
                        case '\\':
                            segmentLength = index - lastSeparator - 1;
                            if (segmentLength > Path.MaxComponentLength)
                                throw new PathTooLongException(SR.IO_PathTooLong);
                            lastSeparator = index;

                            if (foundTilde)
                            {
                                if (segmentLength <= MaxShortName)
                                {
                                    // Possibly a short path.
                                    possibleShortPath = true;
                                }

                                foundTilde = false;
                            }

                            if (possibleBadUnc)
                            {
                                // If we're at the end of the path and this is the first separator, we're missing the share.
                                // Otherwise we're good, so ignore UNC tracking from here.
                                if (index == fullPath.Length - 1)
                                    throw new ArgumentException(SR.Arg_PathIllegalUNC);
                                else
                                    possibleBadUnc = false;
                            }

                            break;

                        default:
                            if (fullCheck && current < ' ') throw new ArgumentException(SR.Argument_InvalidPathChars, "path");

                            // Do some easy filters for invalid short names.
                            if (foundTilde && !IsPossibleShortChar(current)) foundTilde = false;
                            break;
                    }

                    index++;
                }

                if (possibleBadUnc)
                    throw new ArgumentException(SR.Arg_PathIllegalUNC);

                segmentLength = fullPath.Length - lastSeparator - 1;
                if (segmentLength > Path.MaxComponentLength)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                if (foundTilde && segmentLength <= MaxShortName)
                    possibleShortPath = true;

                // Check for a short filename path and try and expand it. Technically you don't need to have a tilde for a short name, but
                // this is how we've always done this. This expansion is costly so we'll continue to let other short paths slide.
                if (expandShortPaths && possibleShortPath)
                {
                    return TryExpandShortFileName(fullPath);
                }
                else
                {
                    if (fullPath.Length == path.Length && fullPath.StartsWithOrdinal(path))
                    {
                        // If we have the exact same string we were passed in, don't bother to allocate another string from the StringBuilder.
                        return path;
                    }
                    else
                    {
                        return fullPath.ToString();
                    }
                }
            }
            finally
            {
                if (fullPath != null && fullPath.Capacity <= MaxBuilderSize)
                {
                    // Stash for reuse
                    t_cachedOutputBuffer = fullPath;
                }
            }
        }

        private static bool IsPossibleShortChar(char value)
        {
            // Normal restrictions apply, also cannot be a-z, +, ;, =, [,], 127, above 255, or comma.
            // Lower case has to be let through as the file system isn't case sensitive and will match against the upper case.
            return value >= ' '
                && value <= LastAnsi
                && value != '+'
                && value != ';'
                && value != '='
                && value != '['
                && value != ']'
                && value != ','
                && value != Delete;
        }

        private bool IsUnc(StringBuilder builder)
        {
            return builder.Length > 1 && builder[0] == '\\' && builder[1] == '\\';
        }

        private StringBuilder GetOutputBuffer(int minimumCapacity)
        {
            StringBuilder local = t_cachedOutputBuffer;
            if (local == null)
            {
                local = new StringBuilder(minimumCapacity);
            }
            else
            {
                local.Clear();
                local.EnsureCapacity(minimumCapacity);
            }

            return local;
        }

        private StringBuilder GetFullPathName(string path)
        {
            // Historically we would skip leading spaces *only* if the path started with a drive " C:" or a UNC " \\"
            int startIndex = PathInternal.PathStartSkip(path);
            int capacity = path.Length;

            if (PathInternal.IsRelative(path))
            {
                // If the initial path is relative the final path will likely be no more than the current directory length (which can only
                // be MaxPath) so we'll pick that as a reasonable start.
                capacity += PathInternal.MaxShortPath;
            }
            else
            {
                // If the string starts with an extended prefix we would need to remove it from the path before we call GetFullPathName as
                // it doesn't root extended paths correctly. We don't currently resolve extended paths, so we'll just assert here.
                Debug.Assert(!PathInternal.IsExtended(path));
            }

            StringBuilder outputBuffer = this.GetOutputBuffer(capacity);

            fixed (char* pathStart = path)
            {
                int result = 0;
                while ((result = Interop.mincore.GetFullPathNameW(pathStart + startIndex, outputBuffer.Capacity + 1, outputBuffer, IntPtr.Zero)) > outputBuffer.Capacity)
                {
                    // Reported size (which does not include the null) is greater than the buffer size. Increase the capacity.
                    outputBuffer.Capacity = result;
                }

                if (result == 0)
                {
                    // Failure, get the error and throw
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == 0)
                        errorCode = Interop.mincore.Errors.ERROR_BAD_PATHNAME;
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
                }
            }

            return outputBuffer;
        }

        private int GetInputBuffer(StringBuilder content, bool isUnc, out char[] buffer)
        {
            int length = content.Length;
            length += isUnc ? PathInternal.UncExtendedPrefixToInsert.Length : PathInternal.ExtendedPathPrefix.Length;
            buffer = new char[length];

            if (isUnc)
            {
                PathInternal.UncExtendedPathPrefix.CopyTo(0, buffer, 0, PathInternal.UncExtendedPathPrefix.Length);
                int prefixDifference = PathInternal.UncExtendedPathPrefix.Length - PathInternal.UncPathPrefix.Length;
                content.CopyTo(prefixDifference, buffer, PathInternal.ExtendedPathPrefix.Length, content.Length - prefixDifference);
                return prefixDifference;
            }
            else
            {
                int prefixSize = PathInternal.ExtendedPathPrefix.Length;
                PathInternal.ExtendedPathPrefix.CopyTo(0, buffer, 0, prefixSize);
                content.CopyTo(0, buffer, prefixSize, content.Length);
                return prefixSize;
            }
        }

        private string TryExpandShortFileName(StringBuilder outputBuffer)
        {
            // We guarantee we'll expand short names for paths that only partially exist. As such, we need to find the part of the path that actually does exist. To
            // avoid allocating like crazy we'll create only one input array and modify the contents with embedded nulls.

            Debug.Assert(!PathInternal.IsRelative(outputBuffer), "should have resolved by now");
            Debug.Assert(!PathInternal.IsExtended(outputBuffer), "expanding short names expects normal paths");

            // Add the extended prefix before expanding to allow growth over MAX_PATH
            char[] inputBuffer = null;
            int rootLength = PathInternal.GetRootLength(outputBuffer);
            bool isUnc = this.IsUnc(outputBuffer);
            int rootDifference = this.GetInputBuffer(outputBuffer, isUnc, out inputBuffer);
            rootLength += rootDifference;
            int inputLength = inputBuffer.Length;

            bool success = false;
            int foundIndex = inputBuffer.Length - 1;

            while (!success)
            {
                int result = Interop.mincore.GetLongPathNameW(inputBuffer, outputBuffer, outputBuffer.Capacity + 1);

                // Replace any temporary null we added
                if (inputBuffer[foundIndex] == '\0') inputBuffer[foundIndex] = '\\';

                if (result == 0)
                {
                    // Look to see if we couldn't find the file
                    int error = Marshal.GetLastWin32Error();
                    if (error != Interop.mincore.Errors.ERROR_FILE_NOT_FOUND && error != Interop.mincore.Errors.ERROR_PATH_NOT_FOUND)
                    {
                        // Some other failure, give up
                        break;
                    }

                    // We couldn't find the path at the given index, start looking further back in the string.
                    foundIndex--;

                    for (; foundIndex > rootLength && inputBuffer[foundIndex] != '\\'; foundIndex--) ;
                    if (foundIndex == rootLength)
                    {
                        // Can't trim the path back any further
                        break;
                    }
                    else
                    {
                        // Temporarily set a null in the string to get Windows to look further up the path
                        inputBuffer[foundIndex] = '\0';
                    }
                }
                else if (result > outputBuffer.Capacity)
                {
                    // Not enough space. The result count for this API does not include the null terminator.
                    outputBuffer.EnsureCapacity(result);
                    result = Interop.mincore.GetLongPathNameW(inputBuffer, outputBuffer, outputBuffer.Capacity + 1);
                }
                else
                {
                    // Found the path
                    success = true;
                    if (foundIndex < inputLength - 1)
                    {
                        // It was a partial find, put the non-existant part of the path back
                        outputBuffer.Append(inputBuffer, foundIndex, inputBuffer.Length - foundIndex);
                    }
                }
            }

            // Strip out the prefix and return the string
            if (success)
            {
                if (isUnc)
                {
                    // Need to go from \\?\UNC\ to \\?\UN\\
                    outputBuffer[PathInternal.UncExtendedPathPrefix.Length - 1] = '\\';
                    return outputBuffer.ToString(rootDifference, outputBuffer.Length - rootDifference);
                }
                else
                {
                    return outputBuffer.ToString(rootDifference, outputBuffer.Length - rootDifference);
                }
            }
            else
            {
                // Failed to get and expanded path, clean up our input
                if (isUnc)
                {
                    // Need to go from \\?\UNC\ to \\?\UN\\
                    inputBuffer[PathInternal.UncExtendedPathPrefix.Length - 1] = '\\';
                    return new string(inputBuffer, rootDifference, inputBuffer.Length - rootDifference);
                }
                else
                {
                    return new string(inputBuffer, rootDifference, inputBuffer.Length - rootDifference);
                }
            }
        }
    }
}
