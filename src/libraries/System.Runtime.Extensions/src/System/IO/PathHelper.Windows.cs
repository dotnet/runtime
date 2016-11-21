// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    /// Wrapper to help with path normalization.
    /// </summary>
    unsafe internal class PathHelper
    {
        // Can't be over 8.3 and be a short name
        private const int MaxShortName = 12;

        private const char LastAnsi = (char)255;
        private const char Delete = (char)127;

        // Trim trailing white spaces, tabs etc but don't be aggressive in removing everything that has UnicodeCategory of trailing space.
        // string.WhitespaceChars will trim more aggressively than what the underlying FS does (for ex, NTFS, FAT).
        private static readonly char[] s_trimEndChars =
        {
            (char)0x9,          // Horizontal tab
            (char)0xA,          // Line feed
            (char)0xB,          // Vertical tab
            (char)0xC,          // Form feed
            (char)0xD,          // Carriage return
            (char)0x20,         // Space
            (char)0x85,         // Next line
            (char)0xA0          // Non breaking space
        };

        [ThreadStatic]
        private static StringBuffer t_fullPathBuffer;

        /// <summary>
        /// Normalize the given path.
        /// </summary>
        /// <remarks>
        /// Normalizes via Win32 GetFullPathName(). It will also trim all "typical" whitespace at the end of the path (see s_trimEndChars). Will also trim initial
        /// spaces if the path is determined to be rooted.
        /// 
        /// Note that invalid characters will be checked after the path is normalized, which could remove bad characters. (C:\|\..\a.txt -- C:\a.txt)
        /// </remarks>
        /// <param name="path">Path to normalize</param>
        /// <param name="checkInvalidCharacters">True to check for invalid characters</param>
        /// <param name="expandShortPaths">Attempt to expand short paths if true</param>
        /// <exception cref="ArgumentException">Thrown if the path is an illegal UNC (does not contain a full server/share) or contains illegal characters.</exception>
        /// <exception cref="PathTooLongException">Thrown if the path or a path segment exceeds the filesystem limits.</exception>
        /// <exception cref="FileNotFoundException">Thrown if Windows returns ERROR_FILE_NOT_FOUND. (See Win32Marshal.GetExceptionForWin32Error)</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if Windows returns ERROR_PATH_NOT_FOUND. (See Win32Marshal.GetExceptionForWin32Error)</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if Windows returns ERROR_ACCESS_DENIED. (See Win32Marshal.GetExceptionForWin32Error)</exception>
        /// <exception cref="IOException">Thrown if Windows returns an error that doesn't map to the above. (See Win32Marshal.GetExceptionForWin32Error)</exception>
        /// <returns>Normalized path</returns>
        internal static string Normalize(string path, bool checkInvalidCharacters, bool expandShortPaths)
        {
            // Get the full path
            StringBuffer fullPath = t_fullPathBuffer ?? (t_fullPathBuffer = new StringBuffer(PathInternal.MaxShortPath));
            try
            {
                GetFullPathName(path, fullPath);

                // Trim whitespace off the end of the string. Win32 normalization trims only U+0020.
                fullPath.TrimEnd(s_trimEndChars);

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

                // We can get UNCs as device paths through this code (e.g. \\.\UNC\), we won't validate them as there isn't
                // an easy way to normalize without extensive cost (we'd have to hunt down the canonical name for any device
                // path that contains UNC or  to see if the path was doing something like \\.\GLOBALROOT\Device\Mup\,
                // \\.\GLOBAL\UNC\, \\.\GLOBALROOT\GLOBAL??\UNC\, etc.
                bool specialPath = fullPath.Length > 1 && fullPath[0] == '\\' && fullPath[1] == '\\';
                bool isDevice = PathInternal.IsDevice(fullPath);
                bool possibleBadUnc = specialPath && !isDevice;
                uint index = specialPath ? 2u : 0;
                uint lastSeparator = specialPath ? 1u : 0;
                uint segmentLength;
                char* start = fullPath.CharPointer;
                char current;

                while (index < fullPath.Length)
                {
                    current = start[index];

                    // Try to skip deeper analysis. '?' and higher are valid/ignorable except for '\', '|', and '~'
                    if (current < '?' || current == '\\' || current == '|' || current == '~')
                    {
                        switch (current)
                        {
                            case '|':
                            case '>':
                            case '<':
                            case '\"':
                                if (checkInvalidCharacters) throw new ArgumentException(SR.Argument_InvalidPathChars);
                                foundTilde = false;
                                break;
                            case '~':
                                foundTilde = true;
                                break;
                            case '\\':
                                segmentLength = index - lastSeparator - 1;
                                if (segmentLength > (uint)PathInternal.MaxComponentLength)
                                    throw new PathTooLongException(SR.IO_PathTooLong + fullPath.ToString());
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
                                if (checkInvalidCharacters && current < ' ') throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));
                                break;
                        }
                    }

                    index++;
                }

                if (possibleBadUnc)
                    throw new ArgumentException(SR.Arg_PathIllegalUNC);

                segmentLength = fullPath.Length - lastSeparator - 1;
                if (segmentLength > (uint)PathInternal.MaxComponentLength)
                    throw new PathTooLongException(SR.IO_PathTooLong);

                if (foundTilde && segmentLength <= MaxShortName)
                    possibleShortPath = true;

                // Check for a short filename path and try and expand it. Technically you don't need to have a tilde for a short name, but
                // this is how we've always done this. This expansion is costly so we'll continue to let other short paths slide.
                if (expandShortPaths && possibleShortPath)
                {
                    return TryExpandShortFileName(fullPath, originalPath: path);
                }
                else
                {
                    if (fullPath.Length == (uint)path.Length && fullPath.StartsWith(path))
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
                // Clear the buffer
                fullPath.Free();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDosUnc(StringBuffer buffer)
        {
            return !PathInternal.IsDevice(buffer) && buffer.Length > 1 && buffer[0] == '\\' && buffer[1] == '\\';
        }

        private static void GetFullPathName(string path, StringBuffer fullPath)
        {
            // If the string starts with an extended prefix we would need to remove it from the path before we call GetFullPathName as
            // it doesn't root extended paths correctly. We don't currently resolve extended paths, so we'll just assert here.
            Debug.Assert(PathInternal.IsPartiallyQualified(path) || !PathInternal.IsExtended(path));

            // Historically we would skip leading spaces *only* if the path started with a drive " C:" or a UNC " \\"
            int startIndex = PathInternal.PathStartSkip(path);

            fixed (char* pathStart = path)
            {
                uint result = 0;
                while ((result = Interop.Kernel32.GetFullPathNameW(pathStart + startIndex, fullPath.CharCapacity, fullPath.GetHandle(), IntPtr.Zero)) > fullPath.CharCapacity)
                {
                    // Reported size (which does not include the null) is greater than the buffer size. Increase the capacity.
                    fullPath.EnsureCharCapacity(result);
                }

                if (result == 0)
                {
                    // Failure, get the error and throw
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == 0)
                        errorCode = Interop.Errors.ERROR_BAD_PATHNAME;
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
                }

                fullPath.Length = result;
            }
        }

        private static uint GetInputBuffer(StringBuffer content, bool isDosUnc, out StringBuffer buffer)
        {
            uint length = content.Length;

            length += isDosUnc
                ? (uint)PathInternal.UncExtendedPrefixLength - PathInternal.UncPrefixLength
                : PathInternal.DevicePrefixLength;

            buffer = new StringBuffer(length);

            if (isDosUnc)
            {
                // Put the extended UNC prefix (\\?\UNC\) in front of the path
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.UncExtendedPathPrefix);

                // Copy the source buffer over after the existing UNC prefix
                content.CopyTo(
                    bufferIndex: PathInternal.UncPrefixLength,
                    destination: buffer,
                    destinationIndex: PathInternal.UncExtendedPrefixLength,
                    count: content.Length - PathInternal.UncPrefixLength);

                // Return the prefix difference
                return (uint)PathInternal.UncExtendedPrefixLength - PathInternal.UncPrefixLength;
            }
            else
            {
                uint prefixSize = (uint)PathInternal.ExtendedPathPrefix.Length;
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.ExtendedPathPrefix);
                content.CopyTo(bufferIndex: 0, destination: buffer, destinationIndex: prefixSize, count: content.Length);
                return prefixSize;
            }
        }

        private static string TryExpandShortFileName(StringBuffer outputBuffer, string originalPath)
        {
            // We guarantee we'll expand short names for paths that only partially exist. As such, we need to find the part of the path that actually does exist. To
            // avoid allocating like crazy we'll create only one input array and modify the contents with embedded nulls.

            Debug.Assert(!PathInternal.IsPartiallyQualified(outputBuffer), "should have resolved by now");

            // We'll have one of a few cases by now (the normalized path will have already:
            //
            //  1. Dos path (C:\)
            //  2. Dos UNC (\\Server\Share)
            //  3. Dos device path (\\.\C:\, \\?\C:\)
            //
            // We want to put the extended syntax on the front if it doesn't already have it, which may mean switching from \\.\.
            //
            // Note that we will never get \??\ here as GetFullPathName() does not recognize \??\ and will return it as C:\??\ (or whatever the current drive is).

            uint rootLength = PathInternal.GetRootLength(outputBuffer);
            bool isDevice = PathInternal.IsDevice(outputBuffer);

            StringBuffer inputBuffer = null;
            bool isDosUnc = false;
            uint rootDifference = 0;
            bool wasDotDevice = false;

            // Add the extended prefix before expanding to allow growth over MAX_PATH
            if (isDevice)
            {
                // We have one of the following (\\?\ or \\.\)
                inputBuffer = new StringBuffer();
                inputBuffer.Append(outputBuffer);

                if (outputBuffer[2] == '.')
                {
                    wasDotDevice = true;
                    inputBuffer[2] = '?';
                }
            }
            else
            {
                isDosUnc = IsDosUnc(outputBuffer);
                rootDifference = GetInputBuffer(outputBuffer, isDosUnc, out inputBuffer);
            }

            rootLength += rootDifference;
            uint inputLength = inputBuffer.Length;

            bool success = false;
            uint foundIndex = inputBuffer.Length - 1;

            while (!success)
            {
                uint result = Interop.Kernel32.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);

                // Replace any temporary null we added
                if (inputBuffer[foundIndex] == '\0') inputBuffer[foundIndex] = '\\';

                if (result == 0)
                {
                    // Look to see if we couldn't find the file
                    int error = Marshal.GetLastWin32Error();
                    if (error != Interop.Errors.ERROR_FILE_NOT_FOUND && error != Interop.Errors.ERROR_PATH_NOT_FOUND)
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
                else if (result > outputBuffer.CharCapacity)
                {
                    // Not enough space. The result count for this API does not include the null terminator.
                    outputBuffer.EnsureCharCapacity(result);
                    result = Interop.Kernel32.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);
                }
                else
                {
                    // Found the path
                    success = true;
                    outputBuffer.Length = result;
                    if (foundIndex < inputLength - 1)
                    {
                        // It was a partial find, put the non-existent part of the path back
                        outputBuffer.Append(inputBuffer, foundIndex, inputBuffer.Length - foundIndex);
                    }
                }
            }

            // Strip out the prefix and return the string
            StringBuffer bufferToUse = success ? outputBuffer : inputBuffer;

            // Switch back from \\?\ to \\.\ if necessary
            if (wasDotDevice)
                bufferToUse[2] = '.';

            string returnValue = null;

            int newLength = (int)(bufferToUse.Length - rootDifference);
            if (isDosUnc)
            {
                // Need to go from \\?\UNC\ to \\?\UN\\
                bufferToUse[PathInternal.UncExtendedPrefixLength - PathInternal.UncPrefixLength] = '\\';
            }

            // We now need to strip out any added characters at the front of the string
            if (bufferToUse.SubstringEquals(originalPath, rootDifference, newLength))
            {
                // Use the original path to avoid allocating
                returnValue = originalPath;
            }
            else
            {
                returnValue = bufferToUse.Substring(rootDifference, newLength);
            }

            inputBuffer.Dispose();
            return returnValue;
        }
    }
}
