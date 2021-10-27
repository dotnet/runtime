// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
	// Even though csc will by default use a sequential layout, a CS0649 warning as error
    // is produced for un-assigned fields when no StructLayout is specified.
    //
    // Explicitly saying Sequential disables that warning/error for consumers which only
    // use Stat in debug builds.
    [StructLayout(LayoutKind.Sequential)]
    internal static partial class Kernel32
    {
        internal struct BY_HANDLE_FILE_INFORMATION
        {
            internal uint dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint dwVolumeSerialNumber;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal uint nNumberOfLinks;
            internal uint nFileIndexHigh;
            internal uint nFileIndexLow;
        }
    }
}