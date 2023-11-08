// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        static void* ClrVirtualReserve(nuint size)
        {
            // The shim will return null for failure
            return (void*)Interop.Sys.MMap(
                0,
                size,
                Interop.Sys.MemoryMappedProtections.PROT_NONE,
                Interop.Sys.MemoryMappedFlags.MAP_PRIVATE | Interop.Sys.MemoryMappedFlags.MAP_ANONYMOUS,
                new SafeFileHandle(-1, false),
                0);

        }

        static void* ClrVirtualCommit(void* pBase, nuint size)
        {
            int result = Interop.Sys.MProtect(
                (nint)pBase,
                size,
                Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE);

            return result == 0 ? pBase : null;
        }

        static void ClrVirtualFree(void* pBase, nuint size)
        {
            Interop.Sys.MUnmap((nint)pBase, size);
        }
    }
}
