// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/devio/storage-read-capacity
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STORAGE_READ_CAPACITY
        {
            internal uint Version;
            internal uint Size;
            internal uint BlockLength;
            internal long NumberOfBlocks;
            internal long DiskLength;
        }
    }
}
