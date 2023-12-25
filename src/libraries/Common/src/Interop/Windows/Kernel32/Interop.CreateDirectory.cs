// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use CreateDirectory.
        /// </summary>
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateDirectoryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool CreateDirectoryPrivate(
            string path,
            SECURITY_ATTRIBUTES* lpSecurityAttributes);

        internal static unsafe bool CreateDirectory(string path, SECURITY_ATTRIBUTES* lpSecurityAttributes)
        {
            // We always want to add for CreateDirectory to get around the legacy 248 character limitation
            path = PathInternal.EnsureExtendedPrefix(path);
            return CreateDirectoryPrivate(path, lpSecurityAttributes);
        }
    }
}
