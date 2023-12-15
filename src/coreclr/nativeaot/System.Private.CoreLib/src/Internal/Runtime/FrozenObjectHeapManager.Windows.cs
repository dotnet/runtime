// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        private static void* ClrVirtualReserve(nuint size)
        {
            return Interop.Kernel32.VirtualAlloc(null, size, Interop.Kernel32.MemOptions.MEM_RESERVE, Interop.Kernel32.PageOptions.PAGE_READWRITE);
        }

        private static void* ClrVirtualCommit(void* pBase, nuint size)
        {
            return Interop.Kernel32.VirtualAlloc(pBase, size, Interop.Kernel32.MemOptions.MEM_COMMIT, Interop.Kernel32.PageOptions.PAGE_READWRITE);
        }

        private static void ClrVirtualFree(void* pBase, nuint size)
        {
            // We require the size parameter for Unix implementation sake.
            // The Win32 API ignores this parameter because we must pass zero.
            // If the caller passed zero, this is going to be broken on Unix
            // so let's at least assert that.
            Debug.Assert(size != 0);

            Interop.Kernel32.VirtualFree(pBase, 0, Interop.Kernel32.MemOptions.MEM_RELEASE);
        }
    }
}
