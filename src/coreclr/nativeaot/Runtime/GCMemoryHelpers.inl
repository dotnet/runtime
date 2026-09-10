// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "volatile.h"
#include "writebarrier.h"

extern WriteBarrierFunctions g_writeBarrierFunctions;

//
// Unmanaged GC memory helpers
//

#ifdef HOST_64BIT
#define CLUMP_SIZE 0x800
#define LOG2_CLUMP_SIZE 11
#else
#define CLUMP_SIZE 0x400
#define LOG2_CLUMP_SIZE 10
#endif

// This function clears a piece of memory in a GC safe way.
// Object-aligned memory is zeroed with no smaller than pointer-size granularity.
// We must make this guarantee whenever we clear memory in the GC heap that could contain object
// references.  The GC or other user threads can read object references at any time, clearing them bytewise can result
// in a read on another thread getting incorrect data.
// Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
// USAGE:  The caller is responsible for null-checking the reference.
FORCEINLINE void InlineGcSafeZeroMemory(void * mem, size_t size)
{
    uint8_t * memBytes = (uint8_t *)mem;
    uint8_t * endBytes = &memBytes[size];

    // handle unaligned bytes at the beginning
    while (!IS_ALIGNED(memBytes, sizeof(void *)) && (memBytes < endBytes))
        *memBytes++ = 0;

    // now write pointer sized pieces
    // volatile ensures that this doesn't get optimized back into a memset call
    size_t nPtrs = (endBytes - memBytes) / sizeof(void *);
    volatile uintptr_t* memPtr = (uintptr_t*)memBytes;
    for (size_t i = 0; i < nPtrs; i++)
        *memPtr++ = 0;

    // handle remaining bytes at the end
    memBytes = (uint8_t*)memPtr;
    while (memBytes < endBytes)
        *memBytes++ = 0;
}

// These functions copy memory in a GC safe way.  They makes the guarantee
// that the memory is copies in at least pointer sized chunks.

FORCEINLINE void InlineForwardGCSafeCopy(void * dest, const void *src, size_t len)
{
    // All parameters must be pointer-size-aligned
    ASSERT(IS_ALIGNED(dest, sizeof(size_t)));
    ASSERT(IS_ALIGNED(src, sizeof(size_t)));
    ASSERT(IS_ALIGNED(len, sizeof(size_t)));

    size_t size = len;
    uint8_t * dmem = (uint8_t *)dest;
    uint8_t * smem = (uint8_t *)src;

    // regions must be non-overlapping
    ASSERT(dmem <= smem || smem + size <= dmem);

    // copy 4 pointers at a time
    while (size >= 4 * sizeof(size_t))
    {
        size -= 4 * sizeof(size_t);
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
        ((size_t *)dmem)[1] = ((size_t *)smem)[1];
        ((size_t *)dmem)[2] = ((size_t *)smem)[2];
        ((size_t *)dmem)[3] = ((size_t *)smem)[3];
        smem += 4 * sizeof(size_t);
        dmem += 4 * sizeof(size_t);
    }

    // copy 2 trailing pointers, if needed
    if ((size & (2 * sizeof(size_t))) != 0)
    {
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
        ((size_t *)dmem)[1] = ((size_t *)smem)[1];
        smem += 2 * sizeof(size_t);
        dmem += 2 * sizeof(size_t);
    }

    // finish with one pointer, if needed
    if ((size & sizeof(size_t)) != 0)
    {
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
    }
}

FORCEINLINE void InlineBackwardGCSafeCopy(void * dest, const void *src, size_t len)
{
    // All parameters must be pointer-size-aligned
    ASSERT(IS_ALIGNED(dest, sizeof(size_t)));
    ASSERT(IS_ALIGNED(src, sizeof(size_t)));
    ASSERT(IS_ALIGNED(len, sizeof(size_t)));

    size_t size = len;
    uint8_t * dmem = (uint8_t *)dest + len;
    uint8_t * smem = (uint8_t *)src + len;

    // regions must be non-overlapping
    ASSERT(smem <= dmem || dmem + size <= smem);

    // copy 4 pointers at a time
    while (size >= 4 * sizeof(size_t))
    {
        size -= 4 * sizeof(size_t);
        smem -= 4 * sizeof(size_t);
        dmem -= 4 * sizeof(size_t);
        ((size_t *)dmem)[3] = ((size_t *)smem)[3];
        ((size_t *)dmem)[2] = ((size_t *)smem)[2];
        ((size_t *)dmem)[1] = ((size_t *)smem)[1];
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
    }

    // copy 2 trailing pointers, if needed
    if ((size & (2 * sizeof(size_t))) != 0)
    {
        smem -= 2 * sizeof(size_t);
        dmem -= 2 * sizeof(size_t);
        ((size_t *)dmem)[1] = ((size_t *)smem)[1];
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
    }

    // finish with one pointer, if needed
    if ((size & sizeof(size_t)) != 0)
    {
        smem -= sizeof(size_t);
        dmem -= sizeof(size_t);
        ((size_t *)dmem)[0] = ((size_t *)smem)[0];
    }
}


#ifndef DACCESS_COMPILE
FORCEINLINE void InlineWriteBarrier(void * dst, void * ref)
{
    g_writeBarrierFunctions.write_barrier(
        g_writeBarrierFunctions.context,
        (void**)dst,
        ref);
}

FORCEINLINE void InlineCheckedWriteBarrier(void * dst, void * ref)
{
    g_writeBarrierFunctions.checked_write_barrier(
        g_writeBarrierFunctions.context,
        (void**)dst,
        ref);
}
#endif // DACCESS_COMPILE
