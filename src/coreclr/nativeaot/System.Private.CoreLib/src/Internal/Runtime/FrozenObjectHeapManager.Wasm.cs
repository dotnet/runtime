// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal unsafe partial class FrozenObjectHeapManager
    {
        private static void* ClrVirtualReserve(nuint size)
        {
            _ = size;
            return null;
        }

        private static void* ClrVirtualCommit(void* pBase, nuint size)
        {
            _ = pBase;
            _ = size;
            return null;
        }

        private static void ClrVirtualFree(void* pBase, nuint size)
        {
            _ = pBase;
            _ = size;
        }
    }
}
