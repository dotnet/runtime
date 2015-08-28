// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use MoveFile.
        /// </summary>
        [DllImport(Libraries.CoreFile_L2, EntryPoint = "MoveFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool MoveFileExPrivate(string src, string dst, uint flags);

        internal static bool MoveFile(string src, string dst)
        {
            src = PathInternal.EnsureExtendedPrefixOverMaxPath(src);
            dst = PathInternal.EnsureExtendedPrefixOverMaxPath(dst);
            return MoveFileExPrivate(src, dst, 2 /* MOVEFILE_COPY_ALLOWED */);
        }
    }
}
