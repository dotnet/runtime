// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        private const uint LMEM_FIXED = 0x000;
        private const uint LMEM_ZEROINIT = 0x0040;
        private const uint LPTR = LMEM_FIXED | LMEM_ZEROINIT;

        // https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-localalloc
        [LibraryImport(Libraries.Kernel32)]
        // [return: NativeTypeName("HLOCAL")]
        private static partial nint LocalAlloc(uint uFlags, nuint uBytes);

        internal static unsafe void* LocalAlloc(nuint byteCount) =>
            (void*)LocalAlloc(LMEM_FIXED, byteCount);

        internal static unsafe void* LocalAllocZeroed(nuint byteCount) =>
            (void*)LocalAlloc(LPTR, byteCount);
    }
}
