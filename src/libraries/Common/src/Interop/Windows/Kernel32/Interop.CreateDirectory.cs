// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use CreateDirectory.
        /// </summary>
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateDirectoryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateDirectoryPrivate(
            string path,
            ref SECURITY_ATTRIBUTES lpSecurityAttributes);

        internal static bool CreateDirectory(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes)
        {
            // If length is greater than `MaxShortDirectoryPath` we add a extended prefix to get around the legacy character limitation
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);

            return CreateDirectoryPrivate(path, ref lpSecurityAttributes);
        }
    }
}
