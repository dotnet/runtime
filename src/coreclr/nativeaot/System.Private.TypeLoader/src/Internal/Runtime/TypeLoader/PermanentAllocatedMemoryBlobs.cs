// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.Augments;

using Internal.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class PermanentAllocatedMemoryBlobs
    {
        // Various functions in the type loader need to create permanent pointers for various purposes.

        private static PermanentlyAllocatedMemoryRegions_Uint_In_IntPtr s_uintCellValues = new PermanentlyAllocatedMemoryRegions_Uint_In_IntPtr();
        private static PermanentlyAllocatedMemoryRegions_IntPtr_In_IntPtr s_pointerIndirectionCellValues = new PermanentlyAllocatedMemoryRegions_IntPtr_In_IntPtr();

        private class PermanentlyAllocatedMemoryRegions_Uint_In_IntPtr
        {
            private LowLevelDictionary<uint, IntPtr> _allocatedBlocks = new LowLevelDictionary<uint, IntPtr>();
            private Lock _lock = new Lock();

            public unsafe IntPtr GetMemoryBlockForValue(uint value)
            {
                using (LockHolder.Hold(_lock))
                {
                    IntPtr result;
                    if (_allocatedBlocks.TryGetValue(value, out result))
                    {
                        return result;
                    }
                    result = MemoryHelpers.AllocateMemory(IntPtr.Size);
                    *(uint*)(result.ToPointer()) = value;
                    _allocatedBlocks.Add(value, result);
                    return result;
                }
            }
        }

        private class PermanentlyAllocatedMemoryRegions_IntPtr_In_IntPtr
        {
            private LowLevelDictionary<IntPtr, IntPtr> _allocatedBlocks = new LowLevelDictionary<IntPtr, IntPtr>();
            private Lock _lock = new Lock();

            public unsafe IntPtr GetMemoryBlockForValue(IntPtr value)
            {
                using (LockHolder.Hold(_lock))
                {
                    IntPtr result;
                    if (_allocatedBlocks.TryGetValue(value, out result))
                    {
                        return result;
                    }
                    result = MemoryHelpers.AllocateMemory(IntPtr.Size);
                    *(IntPtr*)(result.ToPointer()) = value;
                    _allocatedBlocks.Add(value, result);
                    return result;
                }
            }
        }

        public static IntPtr GetPointerToUInt(uint value)
        {
            return s_uintCellValues.GetMemoryBlockForValue(value);
        }

        public static IntPtr GetPointerToIntPtr(IntPtr value)
        {
            return s_pointerIndirectionCellValues.GetMemoryBlockForValue(value);
        }
    }
}
