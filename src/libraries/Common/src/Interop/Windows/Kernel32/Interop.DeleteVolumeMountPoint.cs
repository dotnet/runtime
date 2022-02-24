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
        /// WARNING: This method does not implicitly handle long paths. Use DeleteVolumeMountPoint.
        /// </summary>
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "DeleteVolumeMountPointW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial bool DeleteVolumeMountPointPrivate(string mountPoint);

        internal static bool DeleteVolumeMountPoint(string mountPoint)
        {
            mountPoint = PathInternal.EnsureExtendedPrefixIfNeeded(mountPoint);
            return DeleteVolumeMountPointPrivate(mountPoint);
        }
    }
}
