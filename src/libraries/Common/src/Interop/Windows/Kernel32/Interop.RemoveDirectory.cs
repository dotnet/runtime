// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use RemoveDirectory.
        /// </summary>
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "RemoveDirectoryW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static partial bool RemoveDirectoryPrivate(string path);

        internal static bool RemoveDirectory(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return RemoveDirectoryPrivate(path);
        }
    }
}
