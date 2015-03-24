// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static class DllImports
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int GetLogicalDrives();

    [DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

    [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern bool GetVolumeInformation(String drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);

    [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern int GetDriveType(string drive);
}

