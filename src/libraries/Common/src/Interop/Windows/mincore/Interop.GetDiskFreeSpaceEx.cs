// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        // NOTE: The out parameters are PULARGE_INTEGERs and may require
        // some byte munging magic.
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetDiskFreeSpaceExW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetDiskFreeSpaceEx(string drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);
    }
}
