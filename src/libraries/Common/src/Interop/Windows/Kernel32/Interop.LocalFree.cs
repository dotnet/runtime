// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-localfree
        [LibraryImport(Libraries.Kernel32)]
        // [return: NativeTypeName("HLOCAL")]
        private static partial nint LocalFree(
            // [NativeTypeName("HLOCAL")]
            nint hMem);

        internal static unsafe void* LocalFree(void* ptr) => (void*)LocalFree((nint)ptr);
    }
}
