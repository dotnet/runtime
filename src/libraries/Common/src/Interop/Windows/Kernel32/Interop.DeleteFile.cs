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
        /// WARNING: This method does not implicitly handle long paths. Use DeleteFile.
        /// </summary>
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "DeleteFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool DeleteFilePrivate(string path);
#else
        [DllImport(Libraries.Kernel32, EntryPoint = "DeleteFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFilePrivate(string path);
#endif

        internal static bool DeleteFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixIfNeeded(path);
            return DeleteFilePrivate(path);
        }
    }
}
