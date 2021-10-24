// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

#pragma warning disable CS0649
internal static partial class Interop
{
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
#pragma warning restore CS0649