// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFileAttributesEx.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "GetFileAttributesExW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetFileAttributesExPrivate(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        internal static bool GetFileAttributesEx(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation)
        {
            string? nameWithExtendedPrefix = PathInternal.EnsureExtendedPrefixIfNeeded(name);
            Debug.Assert(nameWithExtendedPrefix != null, "null not expected when non-null is passed"); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            return GetFileAttributesExPrivate(nameWithExtendedPrefix, fileInfoLevel, ref lpFileInformation);
        }
    }
}
