// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct WIN32_FIND_DATA
        {
            internal uint dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal uint dwReserved0;
            internal uint dwReserved1;
            private fixed char _cFileName[MAX_PATH];
            private fixed char _cAlternateFileName[14];

            internal ReadOnlySpan<char> cFileName =>
                MemoryMarshal.CreateReadOnlySpan(ref _cFileName[0], MAX_PATH);
        }
    }
}
