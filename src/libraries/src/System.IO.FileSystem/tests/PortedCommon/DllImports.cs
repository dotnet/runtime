// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static class DllImports
{
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int GetLogicalDrives();

    [DllImport("kernel32.dll", EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern bool GetDiskFreeSpaceEx(string drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

    [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern bool GetVolumeInformation(string drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);

    [DllImport("kernel32.dll", EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
    internal static extern int GetDriveType(string drive);
}

