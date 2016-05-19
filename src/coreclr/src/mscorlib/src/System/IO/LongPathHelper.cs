// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace System.IO
{
    /// <summary>
    /// Wrapper to help with path normalization.
    /// </summary>
    internal class LongPathHelper
    {
        // Can't be over 8.3 and be a short name
        private const int MaxShortName = 12;

        private const char LastAnsi = (char)255;
        private const char Delete = (char)127;

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
        [System.Security.SecurityCritical]
        unsafe internal static string Normalize(string path, uint maxPathLength, bool checkInvalidCharacters, bool expandShortPaths)
        {
            // Get the full path
            StringBuffer fullPath = t_fullPathBuffer ?? (t_fullPathBuffer = new StringBuffer(PathInternal.MaxShortPath));
            try
            {
                GetFullPathName(path, fullPath);

                // Trim whitespace off the end of the string. Win32 normalization trims only U+0020.
                fullPath.TrimEnd(Path.TrimEndChars);

                if (fullPath.Length >= maxPathLength)
                {
                    // Fullpath is genuinely too long
                    throw new PathTooLongException();
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

                // As the path could be > 60K, we'll combine the validity scan. None of these checks are performed by the Win32
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
                                if (checkInvalidCharacters) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
                                // No point in expanding a bad path
                                foundTilde = false;
                                break;
                            case '~':
                                foundTilde = true;
                                break;
                            case '\\':
                                segmentLength = index - lastSeparator - 1;
                                if (segmentLength > (uint)PathInternal.MaxComponentLength)
                                    throw new PathTooLongException();
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
                                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegalUNC"));
                                    else
                                        possibleBadUnc = false;
                                }

                                break;

                            default:
                                if (checkInvalidCharacters && current < ' ') throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
                                break;
                        }
                    }

                    index++;
                }

                if (possibleBadUnc)
                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegalUNC"));

                segmentLength = fullPath.Length - lastSeparator - 1;
                if (segmentLength > (uint)PathInternal.MaxComponentLength)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));

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

        [System.Security.SecurityCritical]
        unsafe private static void GetFullPathName(string path, StringBuffer fullPath)
        {
            // If the string starts with an extended prefix we would need to remove it from the path before we call GetFullPathName as
            // it doesn't root extended paths correctly. We don't currently resolve extended paths, so we'll just assert here.
            Contract.Assert(PathInternal.IsPartiallyQualified(path) || !PathInternal.IsExtended(path));

            // Historically we would skip leading spaces *only* if the path started with a drive " C:" or a UNC " \\"
            int startIndex = PathInternal.PathStartSkip(path);

            fixed (char* pathStart = path)
            {
                uint result = 0;
                while ((result = Win32Native.GetFullPathNameW(pathStart + startIndex, fullPath.CharCapacity, fullPath.GetHandle(), IntPtr.Zero)) > fullPath.CharCapacity)
                {
                    // Reported size (which does not include the null) is greater than the buffer size. Increase the capacity.
                    fullPath.EnsureCharCapacity(result);
                }

                if (result == 0)
                {
                    // Failure, get the error and throw
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == 0)
                        errorCode = Win32Native.ERROR_BAD_PATHNAME;
                    __Error.WinIOError(errorCode, path);
                }

                fullPath.Length = result;
            }
        }

        [System.Security.SecurityCritical]
        unsafe internal static string GetLongPathName(StringBuffer path)
        {
            using (StringBuffer outputBuffer = new StringBuffer(path.Length))
            {
                uint result = 0;
                while ((result = Win32Native.GetLongPathNameW(path.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity)) > outputBuffer.CharCapacity)
                {
                    // Reported size (which does not include the null) is greater than the buffer size. Increase the capacity.
                    outputBuffer.EnsureCharCapacity(result);
                }

                if (result == 0)
                {
                    // Failure, get the error and throw
                    GetErrorAndThrow(path.ToString());
                }

                outputBuffer.Length = result;
                return outputBuffer.ToString();
            }
        }

        [System.Security.SecurityCritical]
        unsafe internal static string GetLongPathName(string path)
        {
            using (StringBuffer outputBuffer = new StringBuffer((uint)path.Length))
            {
                uint result = 0;
                while ((result = Win32Native.GetLongPathNameW(path, outputBuffer.GetHandle(), outputBuffer.CharCapacity)) > outputBuffer.CharCapacity)
                {
                    // Reported size (which does not include the null) is greater than the buffer size. Increase the capacity.
                    outputBuffer.EnsureCharCapacity(result);
                }

                if (result == 0)
                {
                    // Failure, get the error and throw
                    GetErrorAndThrow(path);
                }

                outputBuffer.Length = result;
                return outputBuffer.ToString();
            }
        }

        [System.Security.SecurityCritical]
        private static void GetErrorAndThrow(string path)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == 0)
                errorCode = Win32Native.ERROR_BAD_PATHNAME;
            __Error.WinIOError(errorCode, path);
        }

        // It is significantly more complicated to get the long path with minimal allocations if we're injecting the extended dos path prefix. The implicit version
        // should match up with what is in CoreFx System.Runtime.Extensions.
#if !FEATURE_IMPLICIT_LONGPATH
        [System.Security.SecuritySafeCritical]
        private unsafe static string TryExpandShortFileName(StringBuffer outputBuffer, string originalPath)
        {
            // We guarantee we'll expand short names for paths that only partially exist. As such, we need to find the part of the path that actually does exist. To
            // avoid allocating like crazy we'll create only one input array and modify the contents with embedded nulls.

            Contract.Assert(!PathInternal.IsPartiallyQualified(outputBuffer), "should have resolved by now");

            using (StringBuffer inputBuffer = new StringBuffer(outputBuffer))
            {
                bool success = false;
                uint lastIndex = outputBuffer.Length - 1;
                uint foundIndex = lastIndex;
                uint rootLength = PathInternal.GetRootLength(outputBuffer);

                while (!success)
                {
                    uint result = Win32Native.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);

                    // Replace any temporary null we added
                    if (inputBuffer[foundIndex] == '\0') inputBuffer[foundIndex] = '\\';

                    if (result == 0)
                    {
                        // Look to see if we couldn't find the file
                        int error = Marshal.GetLastWin32Error();
                        if (error != Win32Native.ERROR_FILE_NOT_FOUND && error != Win32Native.ERROR_PATH_NOT_FOUND)
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
                    }
                    else
                    {
                        // Found the path
                        success = true;
                        outputBuffer.Length = result;
                        if (foundIndex < lastIndex)
                        {
                            // It was a partial find, put the non-existant part of the path back
                            outputBuffer.Append(inputBuffer, foundIndex, inputBuffer.Length - foundIndex);
                        }
                    }
                }

                StringBuffer bufferToUse = success ? outputBuffer : inputBuffer;

                if (bufferToUse.SubstringEquals(originalPath))
                {
                    // Use the original path to avoid allocating
                    return originalPath;
                }

                return bufferToUse.ToString();
            }
        }
#else // !FEATURE_IMPLICIT_LONGPATH

        private static uint GetInputBuffer(StringBuffer content, bool isDosUnc, out StringBuffer buffer)
        {
            uint length = content.Length;

            length += isDosUnc ? (uint)PathInternal.UncExtendedPrefixToInsert.Length : (uint)PathInternal.ExtendedPathPrefix.Length;
            buffer = new StringBuffer(length);

            if (isDosUnc)
            {
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.UncExtendedPathPrefix);
                uint prefixDifference = (uint)(PathInternal.UncExtendedPathPrefix.Length - PathInternal.UncPathPrefix.Length);
                content.CopyTo(bufferIndex: prefixDifference, destination: buffer, destinationIndex: (uint)PathInternal.ExtendedPathPrefix.Length, count: content.Length - prefixDifference);
                return prefixDifference;
            }
            else
            {
                uint prefixSize = (uint)PathInternal.ExtendedPathPrefix.Length;
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.ExtendedPathPrefix);
                content.CopyTo(bufferIndex: 0, destination: buffer, destinationIndex: prefixSize, count: content.Length);
                return prefixSize;
            }
        }

        [System.Security.SecuritySafeCritical]
        private static string TryExpandShortFileName(StringBuffer outputBuffer, string originalPath)
        {
            // We'll have one of a few cases by now (the normalized path will have already:
            //
            //  1. Dos path (C:\)
            //  2. Dos UNC (\\Server\Share)
            //  3. Dos device path (\\.\C:\, \\?\C:\)
            //
            // We want to put the extended syntax on the front if it doesn't already have it, which may mean switching from \\.\.

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
                // We will never get \??\ here as GetFullPathName() does not recognize \??\ and will return it as C:\??\ (or whatever the current drive is).
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
                // \\Server\Share, but not \\.\ or \\?\.
                // We need to know this to be able to push \\?\UNC\ on if required
                isDosUnc = outputBuffer.Length > 1 && outputBuffer[0] == '\\' && outputBuffer[1] == '\\' && !PathInternal.IsDevice(outputBuffer);
                rootDifference = GetInputBuffer(outputBuffer, isDosUnc, out inputBuffer);
            }

            rootLength += rootDifference;
            uint inputLength = inputBuffer.Length;

            bool success = false;
            uint foundIndex = inputBuffer.Length - 1;

            while (!success)
            {
                uint result = Win32Native.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);

                // Replace any temporary null we added
                if (inputBuffer[foundIndex] == '\0') inputBuffer[foundIndex] = '\\';

                if (result == 0)
                {
                    // Look to see if we couldn't find the file
                    int error = Marshal.GetLastWin32Error();
                    if (error != Win32Native.ERROR_FILE_NOT_FOUND && error != Win32Native.ERROR_PATH_NOT_FOUND)
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
                    result = Win32Native.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);
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
            if (wasDotDevice)
                bufferToUse[2] = '.';

            string returnValue = null;

            int newLength = (int)(bufferToUse.Length - rootDifference);
            if (isDosUnc)
            {
                // Need to go from \\?\UNC\ to \\?\UN\\
                bufferToUse[(uint)PathInternal.UncExtendedPathPrefix.Length - 1] = '\\';
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
#endif // FEATURE_IMPLICIT_LONGPATH
    }
}