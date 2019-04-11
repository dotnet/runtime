// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RESERVE = 0x2000;
        internal const int MEM_RELEASE = 0x8000;
        internal const int MEM_FREE = 0x10000;
        internal const int PAGE_READWRITE = 0x04;

#if ENABLE_WINRT
        [DllImport("api-ms-win-core-memory-l1-1-3.dll", EntryPoint = "VirtualAllocFromApp")]
        internal static extern unsafe void* VirtualAlloc(void* BaseAddress, UIntPtr Size, int AllocationType, int Protection);
#else
        [DllImport(Libraries.Kernel32)]
        internal static extern unsafe void* VirtualAlloc(void* lpAddress, UIntPtr dwSize, int flAllocationType, int flProtect);
#endif
    }
}
