// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        static void* ClrVirtualReserve(nuint size)
        {
            return Interop.Kernel32.VirtualAlloc(null, size, Interop.Kernel32.MemOptions.MEM_RESERVE, Interop.Kernel32.PageOptions.PAGE_READWRITE);
        }

        static void* ClrVirtualCommit(void* pBase, nuint size)
        {
            return Interop.Kernel32.VirtualAlloc(pBase, size, Interop.Kernel32.MemOptions.MEM_COMMIT, Interop.Kernel32.PageOptions.PAGE_READWRITE);
        }

        static void ClrVirtualFree(void* pBase, nuint size)
        {
            Interop.Kernel32.VirtualFree(pBase, size, Interop.Kernel32.MemOptions.MEM_RELEASE);
        }
    }
}
