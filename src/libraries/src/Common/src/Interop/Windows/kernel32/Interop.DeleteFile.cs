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
        /// WARNING: This method does not implicitly handle long paths. Use DeleteFile.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool DeleteFilePrivate(string path);

        internal static bool DeleteFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            return DeleteFilePrivate(path);
        }
    }
}
