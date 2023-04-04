// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged GC memory helpers
//

#include "common.h"
#include "gcenv.h"
#include "PalRedhawkCommon.h"
#include "CommonMacros.inl"

#include "GCMemoryHelpers.h"
#include "GCMemoryHelpers.inl"

// This function clears a piece of memory in a GC safe way.  It makes the guarantee that it will clear memory in at
// least pointer sized chunks whenever possible.  Unaligned memory at the beginning and remaining bytes at the end are
// written bytewise. We must make this guarantee whenever we clear memory in the GC heap that could contain object
// references.  The GC or other user threads can read object references at any time, clearing them bytewise can result
// in a read on another thread getting incorrect data.
//
// USAGE:  The caller is responsible for hoisting any null reference exceptions to a place where the hardware exception
//         can be properly translated to a managed exception.
COOP_PINVOKE_CDECL_HELPER(void *, RhpInitMultibyte, (void * mem, size_t size))
{
    // The caller must do the null-check because we cannot take an AV in the runtime and translate it to managed.
    ASSERT(mem != nullptr);

    InlineGCSafeFillMemory(mem, size, 0);

    // memset returns the destination buffer
    return mem;
}

// Move memory, in a way that is compatible with a move onto the heap, but
// does not require the destination pointer to be on the heap.

COOP_PINVOKE_HELPER(void, RhBulkMoveWithWriteBarrier, (uint8_t* pDest, uint8_t* pSrc, size_t cbDest))
{
    if (pDest <= pSrc || pSrc + cbDest <= pDest)
        InlineForwardGCSafeCopy(pDest, pSrc, cbDest);
    else
        InlineBackwardGCSafeCopy(pDest, pSrc, cbDest);

    InlinedBulkWriteBarrier(pDest, cbDest);
}

void REDHAWK_CALLCONV RhpBulkWriteBarrier(void* pMemStart, uint32_t cbMemSize)
{
    InlinedBulkWriteBarrier(pMemStart, cbMemSize);
}
