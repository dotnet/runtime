// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged GC memory helpers
//

#include "common.h"
#include "gcenv.h"
#include "PalRedhawkCommon.h"
#include "CommonMacros.inl"
#include "GCMemoryHelpers.inl"

// This function clears a piece of memory in a GC safe way.
// Object-aligned memory is zeroed with no smaller than pointer-size granularity.
// We must make this guarantee whenever we clear memory in the GC heap that could contain object
// references.  The GC or other user threads can read object references at any time, clearing them bytewise can result
// in a read on another thread getting incorrect data.
// Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
// USAGE:  The caller is responsible for null-checking the reference.
COOP_PINVOKE_CDECL_HELPER(void *, RhpGcSafeZeroMemory, (void * mem, size_t size))
{
    // The caller must do the null-check because we cannot take an AV in the runtime and translate it to managed.
    ASSERT(mem != nullptr);

    InlineGcSafeZeroMemory(mem, size);

    // memset returns the destination buffer
    return mem;
}

#if defined(TARGET_X86) || defined(TARGET_AMD64) 
    // 
    // Memory writes are already ordered
    // 
    #define GCHeapMemoryBarrier() 
#else
    #define GCHeapMemoryBarrier() MemoryBarrier() 
#endif 

// Move memory, in a way that is compatible with a move onto the heap, but
// does not require the destination pointer to be on the heap.

COOP_PINVOKE_HELPER(void, RhBulkMoveWithWriteBarrier, (uint8_t* pDest, uint8_t* pSrc, size_t cbDest))
{
    // It is possible that the bulk write is publishing object references accessible so far only
    // by the current thread to shared memory.
    // The memory model requires that writes performed by current thread are observable no later
    // than the writes that will actually publish the references.
    GCHeapMemoryBarrier();

    if (pDest <= pSrc || pSrc + cbDest <= pDest)
        InlineForwardGCSafeCopy(pDest, pSrc, cbDest);
    else
        InlineBackwardGCSafeCopy(pDest, pSrc, cbDest);

    InlinedBulkWriteBarrier(pDest, cbDest);
}
