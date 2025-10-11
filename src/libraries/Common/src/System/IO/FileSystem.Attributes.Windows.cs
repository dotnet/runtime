// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static bool DirectoryExists(string? fullPath)
        {
            return DirectoryExists(fullPath, out _);
        }

        private static bool DirectoryExists(string? path, out int lastError)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            lastError = FillAttributeInfo(path, ref data, returnErrorOnNotFound: true);

            return
                (lastError == 0) &&
                (data.dwFileAttributes != Interop.Kernel32.FileAttributes.INVALID_FILE_ATTRIBUTES) &&
                ((data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        public static bool FileExists(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = default;
            int errorCode = FillAttributeInfo(fullPath, ref data, returnErrorOnNotFound: true);

            return
                (errorCode == 0) &&
                (data.dwFileAttributes != Interop.Kernel32.FileAttributes.INVALID_FILE_ATTRIBUTES) &&
                ((data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0);
        }

        /// <summary>
        /// Returns 0 on success, otherwise a Win32 error code.  Note that
        /// classes should use -1 as the uninitialized state for dataInitialized.
        /// </summary>
        /// <param name="path">The file path from which the file attribute information will be filled.</param>
        /// <param name="data">A struct that will contain the attribute information.</param>
        /// <param name="returnErrorOnNotFound">Return the error code for not found errors?</param>
        internal static int FillAttributeInfo(string? path, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool returnErrorOnNotFound)
        {
            int errorCode = Interop.Errors.ERROR_SUCCESS;

            // Neither GetFileAttributes or FindFirstFile like trailing separators
            path = PathInternal.TrimEndingDirectorySeparator(path);

            using (DisableMediaInsertionPrompt.Create())
            {
                // Using 'GetFileAttributesEx' to get file attributes of a pipe
                // will inevitably open that pipe making it useless for a consumer,
                // thus we need to handle this case separately
                if (IsPipePath(path))
                {
                    errorCode = GetFileAttributeInfoUsingFindFileApi(path, ref data);
                }
                else if (!Interop.Kernel32.GetFileAttributesEx(path, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data))
                {
                    errorCode = Marshal.GetLastPInvokeError();

                    if (!IsPathUnreachableError(errorCode))
                    {
                        // Assert so we can track down other cases (if any) to add to our test suite
                        Debug.Assert(errorCode == Interop.Errors.ERROR_ACCESS_DENIED || errorCode == Interop.Errors.ERROR_SHARING_VIOLATION || errorCode == Interop.Errors.ERROR_SEM_TIMEOUT,
                            $"Unexpected error code getting attributes {errorCode} from path {path}");

                        // Files that are marked for deletion will not let you GetFileAttributes,
                        // ERROR_ACCESS_DENIED is given back without filling out the data struct.
                        // FindFirstFile, however, will. Historically we always gave back attributes
                        // for marked-for-deletion files.
                        //
                        // Another case where enumeration works is with special system files such as
                        // pagefile.sys that give back ERROR_SHARING_VIOLATION on GetAttributes.
                        //
                        // Ideally we'd only try again for known cases due to the potential performance
                        // hit. The last attempt to do so baked for nearly a year before we found the
                        // pagefile.sys case. As such we're probably stuck filtering out specific
                        // cases that we know we don't want to retry on.

                        errorCode = GetFileAttributeInfoUsingFindFileApi(path!, ref data);
                    }
                }
            }

            if (errorCode != Interop.Errors.ERROR_SUCCESS && !returnErrorOnNotFound)
            {
                switch (errorCode)
                {
                    case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    case Interop.Errors.ERROR_PATH_NOT_FOUND:
                    case Interop.Errors.ERROR_NOT_READY: // Removable media not ready
                        // Return default value for backward compatibility
                        data.dwFileAttributes = Interop.Kernel32.FileAttributes.INVALID_FILE_ATTRIBUTES;
                        return Interop.Errors.ERROR_SUCCESS;
                }
            }

            return errorCode;

            static int GetFileAttributeInfoUsingFindFileApi(string path, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data)
            {
                Interop.Kernel32.WIN32_FIND_DATA findData = default;
                using (SafeFindHandle handle = Interop.Kernel32.FindFirstFile(path, ref findData))
                {
                    if (handle.IsInvalid)
                    {
                        return Marshal.GetLastPInvokeError();
                    }
                    else
                    {
                        data.PopulateFrom(ref findData);
                        return Interop.Errors.ERROR_SUCCESS;
                    }
                }
            }
        }

        /// <summary>
        /// Tells whether a given path is a Windows pipe path.
        /// Examples of pipe paths are:
        /// <list type="bullet">
        /// <item>
        /// <c>\\.\pipe\pipeName</c> - local pipe path
        /// </item>
        /// <item>
        /// <c>\\serverName\pipe\pipeName</c> - remote pipe path
        /// </item>
        /// </list>
        /// </summary>
        private static bool IsPipePath([NotNullWhen(true)] string? path)
        {
            if (path is null)
            {
                return false;
            }

            ReadOnlySpan<char> pathSpan = path.AsSpan();
            if (!pathSpan.StartsWith(@"\\"))
            {
                return false;
            }

            pathSpan = pathSpan.Slice(2);
            Span<Range> segments = stackalloc Range[3];
            int written = pathSpan.Split(segments, '\\');

            // 3 segments of a pipe path:
            // 1) '.' or 'serverName'
            // 2) Constant 'pipe' segment
            // 3) Pipe name
            return written == 3 && pathSpan[segments[1]].SequenceEqual("pipe");
        }

        internal static bool IsPathUnreachableError(int errorCode) =>
            errorCode is
                Interop.Errors.ERROR_FILE_NOT_FOUND or
                Interop.Errors.ERROR_PATH_NOT_FOUND or
                Interop.Errors.ERROR_NOT_READY or
                Interop.Errors.ERROR_INVALID_NAME or
                Interop.Errors.ERROR_BAD_PATHNAME or
                Interop.Errors.ERROR_BAD_NETPATH or
                Interop.Errors.ERROR_BAD_NET_NAME or
                Interop.Errors.ERROR_INVALID_PARAMETER or
                Interop.Errors.ERROR_NETWORK_UNREACHABLE or
                Interop.Errors.ERROR_NETWORK_ACCESS_DENIED or
                Interop.Errors.ERROR_INVALID_HANDLE or     // eg from \\.\CON
                Interop.Errors.ERROR_FILENAME_EXCED_RANGE; // Path is too long
    }
}
