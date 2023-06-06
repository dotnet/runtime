// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESSOR_NUMBER
        {
            public ushort Group;
            public byte Number;
            public byte Reserved;
        }

        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        internal static partial uint GetCurrentProcessorNumberEx(out PROCESSOR_NUMBER ProcNumber);
    }
}
