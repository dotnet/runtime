// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetVolumeInformationW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetVolumeInformation(
            string drive,
            char* volumeName,
            int volumeNameBufLen,
            int* volSerialNumber,
            int* maxFileNameLen,
            out int fileSystemFlags,
            char* fileSystemName,
            int fileSystemNameBufLen);

        internal const uint FILE_SUPPORTS_ENCRYPTION = 0x00020000;
    }
}
