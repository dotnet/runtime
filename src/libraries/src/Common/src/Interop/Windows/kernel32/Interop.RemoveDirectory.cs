// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use RemoveDirectory.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "RemoveDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool RemoveDirectoryPrivate(string path);

        internal static bool RemoveDirectory(string path)
        {
            path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            return RemoveDirectoryPrivate(path);
        }
    }
}
