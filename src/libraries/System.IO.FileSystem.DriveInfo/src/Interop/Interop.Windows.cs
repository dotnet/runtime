// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    // From WinBase.h
    internal const uint SEM_FAILCRITICALERRORS = 1;

    // Error codes from WinError.h
    internal const int ERROR_SUCCESS = 0x0;
    internal const int ERROR_INVALID_FUNCTION = 0x1;
    internal const int ERROR_FILE_NOT_FOUND = 0x2;
    internal const int ERROR_PATH_NOT_FOUND = 0x3;
    internal const int ERROR_ACCESS_DENIED = 0x5;
    internal const int ERROR_INVALID_HANDLE = 0x6;
    internal const int ERROR_INVALID_DATA = 0xD;
    internal const int ERROR_INVALID_DRIVE = 0xF;
    internal const int ERROR_NO_MORE_FILES = 0x12;
    internal const int ERROR_NOT_READY = 0x15;
    internal const int ERROR_SHARING_VIOLATION = 0x20;
    internal const int ERROR_FILE_EXISTS = 0x50;
    internal const int ERROR_INVALID_PARAMETER = 0x57;
    internal const int ERROR_BROKEN_PIPE = 0x6D;
    internal const int ERROR_INVALID_NAME = 0x7B;
    internal const int ERROR_BAD_PATHNAME = 0xA1;
    internal const int ERROR_ALREADY_EXISTS = 0xB7;
    internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
    internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
    internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
    internal const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation

    internal static partial class mincore
    {
        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern int GetDriveType(String drive);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetVolumeInformation(String drive, [Out]StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out]StringBuilder fileSystemName, int fileSystemNameBufLen);

        // NOTE: The out parameters are PULARGE_INTEGERs and may require
        // some byte munging magic.
        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        [DllImport("api-ms-win-core-kernel32-legacy-l1-1-0.dll", EntryPoint = "SetVolumeLabelW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName);

        [DllImport("api-ms-win-core-errorhandling-l1-1-0.dll")]
        internal extern static uint SetErrorMode(uint uMode);
    }
}
