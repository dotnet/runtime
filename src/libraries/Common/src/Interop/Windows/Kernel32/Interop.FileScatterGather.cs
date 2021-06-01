// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        internal unsafe struct FILE_SEGMENT_ELEMENT
        {
            [FieldOffset(0)]
            public IntPtr Buffer;
            [FieldOffset(0)]
            public ulong Alignment;
        }

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int ReadFileScatter(
            SafeHandle handle,
            FILE_SEGMENT_ELEMENT* segments,
            int numBytesToRead,
            IntPtr reserved_mustBeZero,
            NativeOverlapped* overlapped);

        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFileGather(
            SafeHandle handle,
            FILE_SEGMENT_ELEMENT* segments,
            int numBytesToWrite,
            IntPtr reserved_mustBeZero,
            NativeOverlapped* overlapped);
    }
}
