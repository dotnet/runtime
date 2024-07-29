// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const uint LMEM_MOVEABLE = 0x0002;

        // https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-localrealloc
        [LibraryImport(Libraries.Kernel32)]
        // [return: NativeTypeName("HLOCAL")]
        private static partial nint LocalReAlloc(
            // [NativeTypeName("HLOCAL")]
            nint hMem,
            nuint uBytes,
            uint uFlags);

        internal static unsafe void* LocalReAlloc(void* ptr, nuint byteCount) =>
            (void*)LocalReAlloc((nint)ptr, byteCount, LMEM_MOVEABLE);
    }
}
