// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WIN32_FIND_DATA
        {
            internal uint dwFileAttributes;
            internal FILE_TIME ftCreationTime;
            internal FILE_TIME ftLastAccessTime;
            internal FILE_TIME ftLastWriteTime;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal uint dwReserved0;
            internal uint dwReserved1;
            private FileNameBuffer _cFileName;
            private InlineArray14<char> _cAlternateFileName;

            internal ReadOnlySpan<char> cFileName =>
                MemoryMarshal.CreateReadOnlySpan(ref _cFileName[0], MAX_PATH);

            [InlineArray(MAX_PATH)]
            private struct FileNameBuffer
            {
                private char _element0;
            }
        }
    }
}
