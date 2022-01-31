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
COOP_PINVOKE_CDECL_HELPER(void *, RhpInitMultibyte, (void * mem, int c, size_t size))
{
    // The caller must do the null-check because we cannot take an AV in the runtime and translate it to managed.
    ASSERT(mem != nullptr);

    uintptr_t  bv = (uint8_t)c;
    uintptr_t  pv = 0;

    if (bv != 0)
    {
        pv =
#if (POINTER_SIZE == 8)
            bv << 7*8 | bv << 6*8 | bv << 5*8 | bv << 4*8 |
#endif
            bv << 3*8 | bv << 2*8 | bv << 1*8 | bv;
    }

    InlineGCSafeFillMemory(mem, size, pv);

    // memset returns the destination buffer
    return mem;
}


// This is a GC-safe variant of memcpy.  It guarantees that the object references in the GC heap are updated atomically.
// This is required for type safety and proper operation of the background GC.
//
// USAGE:   1) The caller is responsible for performing the appropriate bulk write barrier.
//          2) The caller is responsible for hoisting any null reference exceptions to a place where the hardware
//             exception can be properly translated to a managed exception.  This is handled by RhpCopyMultibyte.
//          3) The caller must ensure that all three parameters are pointer-size-aligned.  This should be the case for
//             value types which contain GC refs anyway, so if you want to copy structs without GC refs which might be
//             unaligned, then you must use RhpCopyMultibyteNoGCRefs.
COOP_PINVOKE_CDECL_HELPER(void *, memcpyGCRefs, (void * dest, const void *src, size_t len))
{
    // null pointers are not allowed (they are checked by RhpCopyMultibyte)
    ASSERT(dest != nullptr);
    ASSERT(src != nullptr);

    InlineForwardGCSafeCopy(dest, src, len);

    // memcpy returns the destination buffer
    return dest;
}

// This is a GC-safe variant of memcpy.  It guarantees that the object references in the GC heap are updated atomically.
// This is required for type safety and proper operation of the background GC.
// Writebarrier is included.
//
// USAGE:
//          1) The caller is responsible for hoisting any null reference exceptions to a place where the hardware
//             exception can be properly translated to a managed exception.  This is handled by RhpCopyMultibyte.
//          2) The caller must ensure that all three parameters are pointer-size-aligned.  This should be the case for
//             value types which contain GC refs anyway, so if you want to copy structs without GC refs which might be
//             unaligned, then you must use RhpCopyMultibyteNoGCRefs.
COOP_PINVOKE_CDECL_HELPER(void *, memcpyGCRefsWithWriteBarrier, (void * dest, const void *src, size_t len))
{
    // null pointers are not allowed (they are checked by RhpCopyMultibyteWithWriteBarrier)
    ASSERT(dest != nullptr);
    ASSERT(src != nullptr);

    InlineForwardGCSafeCopy(dest, src, len);
    InlinedBulkWriteBarrier(dest, len);

    // memcpy returns the destination buffer
    return dest;
}

// Same as memcpyGCRefsWithWriteBarrier, except it checks if memory might contain GC pointers
// and if so dispatches to memcpyGCRefsWithWriteBarrier and if not uses traditional memcpy
COOP_PINVOKE_CDECL_HELPER(void *, memcpyAnyWithWriteBarrier, (void * dest, const void *src, size_t len))
{
    // null pointers are not allowed (they are checked by RhpCopyMultibyteWithWriteBarrier)
    ASSERT(dest != nullptr);
    ASSERT(src != nullptr);

    // Use GC safe copy whenever there might be GC pointers
    if (IS_ALIGNED(dest, sizeof(size_t)) && IS_ALIGNED(src, sizeof(size_t)) && IS_ALIGNED(len, sizeof(size_t)))
    {
        return memcpyGCRefsWithWriteBarrier(dest, src, len);
    }

    return memcpy(dest, src, len);
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

void GCSafeCopyMemoryWithWriteBarrier(void * dest, const void *src, size_t len)
{
    InlineForwardGCSafeCopy(dest, src, len);
    InlinedBulkWriteBarrier(dest, len);
}

void REDHAWK_CALLCONV RhpBulkWriteBarrier(void* pMemStart, uint32_t cbMemSize)
{
    InlinedBulkWriteBarrier(pMemStart, cbMemSize);
}
