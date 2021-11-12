// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
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
