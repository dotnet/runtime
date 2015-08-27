// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool DeleteFilePrivate(string path);

        internal static bool DeleteFile(string path)
        {
            path = PathInternal.AddExtendedPathPrefixForLongPaths(path);
            return DeleteFilePrivate(path);
        }
    }
}
