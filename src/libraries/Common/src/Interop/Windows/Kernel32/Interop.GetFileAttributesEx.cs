// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFileAttributesEx.
        /// </summary>
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetFileAttributesExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetFileAttributesExPrivate(
            string? name,
            GET_FILEEX_INFO_LEVELS fileInfoLevel,
            ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        internal static bool GetFileAttributesEx(string? name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation)
        {
            name = PathInternal.EnsureExtendedPrefixIfNeeded(name);
            return GetFileAttributesExPrivate(name, fileInfoLevel, ref lpFileInformation);
        }
    }
}
