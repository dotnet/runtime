// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        private static void* ClrVirtualReserve(nuint size)
        {
            // WASM has no page protection or separate reserve/commit operations.
            void* memory = Interop.Sys.AlignedAlloc(FOH_COMMIT_SIZE, size);
            if (memory != null)
            {
                NativeMemory.Clear(memory, size);
            }

            return memory;
        }

        private static void* ClrVirtualCommit(void* pBase, nuint size) => pBase;

        private static void ClrVirtualFree(void* pBase, nuint size) => Interop.Sys.AlignedFree(pBase);
    }
}
