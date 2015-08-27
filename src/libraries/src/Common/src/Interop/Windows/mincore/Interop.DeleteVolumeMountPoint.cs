// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "DeleteVolumeMountPointW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool DeleteVolumeMountPointPrivate(string mountPoint);


        internal static bool DeleteVolumeMountPoint(string mountPoint)
        {
            mountPoint = PathInternal.AddExtendedPathPrefixForLongPaths(mountPoint);
            return DeleteVolumeMountPointPrivate(mountPoint);
        }
    }
}
