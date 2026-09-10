// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Unmanaged GC memory helpers
//

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "PalLimitedContext.h"
#include "CommonMacros.inl"
#include "GCMemoryHelpers.inl"

// This function clears a piece of memory in a GC safe way.
// Object-aligned memory is zeroed with no smaller than pointer-size granularity.
// We must make this guarantee whenever we clear memory in the GC heap that could contain object
// references.  The GC or other user threads can read object references at any time, clearing them bytewise can result
// in a read on another thread getting incorrect data.
// Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
// USAGE:  The caller is responsible for null-checking the reference.
FCIMPL2(void *, RhpGcSafeZeroMemory, void * mem, size_t size)
{
    // The caller must do the null-check because we cannot take an AV in the runtime and translate it to managed.
    ASSERT(mem != nullptr);

    InlineGcSafeZeroMemory(mem, size);

    // memset returns the destination buffer
    return mem;
}
FCIMPLEND

// Move memory, in a way that is compatible with a move onto the heap, but
// does not require the destination pointer to be on the heap.

FCIMPL3(void, RhBulkMoveWithWriteBarrier, uint8_t* pDest, uint8_t* pSrc, size_t cbDest)
{
    if (cbDest == 0 || pDest == pSrc)
        return;

    GCHeapUtilities::BulkMoveWithWriteBarrier(pDest, pSrc, cbDest);
}
FCIMPLEND
