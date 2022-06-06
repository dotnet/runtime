// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    // mknod: https://man7.org/linux/man-pages/man2/mknod.2.html
    // makedev, major and minor: https://man7.org/linux/man-pages/man3/makedev.3.html
    internal static partial class Sys
    {
        internal static int CreateBlockDevice(string pathName, uint mode, uint major, uint minor)
        {
            return MkNod(pathName, mode | FileTypes.S_IFBLK, major, minor);
        }

        internal static int CreateCharacterDevice(string pathName, uint mode, uint major, uint minor)
        {
            return MkNod(pathName, mode | FileTypes.S_IFCHR, major, minor);
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_MkNod", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        private static partial int MkNod(string pathName, uint mode, uint major, uint minor);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetDeviceIdentifiers", SetLastError = true)]
        internal static unsafe partial void GetDeviceIdentifiers(ulong dev, uint* majorNumber, uint* minorNumber);
    }
}
