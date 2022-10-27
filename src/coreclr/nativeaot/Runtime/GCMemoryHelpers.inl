// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "volatile.h"

//
// Unmanaged GC memory helpers
//

#if defined(HOST_64BIT)
static const int card_byte_shift = 11;
static const int card_bundle_byte_shift = 21;
#else
static const int card_byte_shift = 10;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#error Manually managed card bundles are currently only implemented for 64 bit hosts.
#endif
#endif


// This function fills a piece of memory in a GC safe way.  It makes the guarantee
// that it will fill memory in at least pointer sized chunks whenever possible.
// Unaligned memory at the beginning and remaining bytes at the end are written bytewise.
// We must make this guarantee whenever we clear memory in the GC heap that could contain
// object references.  The GC or other user threads can read object references at any time,
// clearing them bytewise can result in a read on another thread getting incorrect data.
FORCEINLINE void InlineGCSafeFillMemory(void * mem, size_t size, size_t pv)
{
    uint8_t * memBytes = (uint8_t *)mem;
    uint8_t * endBytes = &memBytes[size];

    // handle unaligned bytes at the beginning
    while (!IS_ALIGNED(memBytes, sizeof(void *)) && (memBytes < endBytes))
        *memBytes++ = (uint8_t)pv;

    // now write pointer sized pieces
    // volatile ensures that this doesn't get optimized back into a memset call
    size_t nPtrs = (endBytes - memBytes) / sizeof(void *);
    volatile uintptr_t* memPtr = (uintptr_t*)memBytes;
    for (size_t i = 0; i < nPtrs; i++)
        *memPtr++ = pv;

    // handle remaining bytes at the end
    memBytes = (uint8_t*)memPtr;
    while (memBytes < endBytes)
        *memBytes++ = (uint8_t)pv;
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
#ifdef WRITE_BARRIER_CHECK
extern uint8_t* g_GCShadow;
extern uint8_t* g_GCShadowEnd;
typedef DPTR(uint8_t)   PTR_uint8_t;
extern "C" {
    GPTR_DECL(uint8_t, g_lowest_address);
    GPTR_DECL(uint8_t, g_highest_address);
}
#endif

typedef DPTR(uint32_t)   PTR_uint32_t;
extern "C" {
    GPTR_DECL(uint32_t, g_card_table);
}
static const uint32_t INVALIDGCVALUE = 0xcccccccd;

FORCEINLINE void InlineWriteBarrier(void * dst, void * ref)
{
    if (((uint8_t*)ref >= g_ephemeral_low) && ((uint8_t*)ref < g_ephemeral_high))
    {
        // volatile is used here to prevent fetch of g_card_table from being reordered
        // with g_lowest/highest_address check above. See comment in code:gc_heap::grow_brick_card_tables.
        uint8_t* pCardByte = (uint8_t *)VolatileLoadWithoutBarrier(&g_card_table) + ((size_t)dst >> LOG2_CLUMP_SIZE);
        if (*pCardByte != 0xFF)
            *pCardByte = 0xFF;
    }
}

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
extern "C" uint32_t* g_card_bundle_table;
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
extern "C" bool g_sw_ww_enabled_for_gc_heap;
extern "C" uint8_t* g_write_watch_table;

static const int SoftwareWriteWatchAddressToTableByteIndexShift = 12;

// In accordance with the SoftwareWriteWatch scheme, marks a range of addresses
// as dirty, starting at the given address and with the given length.
inline static void SoftwareWriteWatchSetDirtyRegion(void* address, size_t length)
{
    // We presumably have just memcopied something to this address, so it can't be null.
    assert(address != nullptr);

    // The "base index" is the first index in the SWW table that covers the target
    // region of memory.
    size_t base_index = reinterpret_cast<size_t>(address) >> SoftwareWriteWatchAddressToTableByteIndexShift;

    // The "end_index" is the last index in the SWW table that covers the target
    // region of memory.
    uint8_t* end_pointer = reinterpret_cast<uint8_t*>(address) + length - 1;
    size_t end_index = reinterpret_cast<size_t>(end_pointer) >> SoftwareWriteWatchAddressToTableByteIndexShift;

    // We'll mark the entire region of memory as dirty by memsetting all entries in
    // the SWW table between the start and end indexes.
    memset(&g_write_watch_table[base_index], ~0, end_index - base_index + 1);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

FORCEINLINE void InlineCheckedWriteBarrier(void * dst, void * ref)
{
    // if the dst is outside of the heap (unboxed value classes) then we
    //      simply exit
    if (((uint8_t*)dst < g_lowest_address) || ((uint8_t*)dst >= g_highest_address))
        return;

    InlineWriteBarrier(dst, ref);
}

FORCEINLINE void InlinedBulkWriteBarrier(void* pMemStart, size_t cbMemSize)
{
    // Check whether the writes were even into the heap. If not there's no card update required.
    // Also if the size is smaller than a pointer, no write barrier is required.
    // This case can occur with universal shared generic code where the size
    // is not known at compile time.
    if (pMemStart < g_lowest_address || (pMemStart >= g_highest_address) || (cbMemSize < sizeof(uintptr_t)))
    {
        return;
    }

#ifdef WRITE_BARRIER_CHECK
    // Perform shadow heap updates corresponding to the gc heap updates that immediately preceded this helper
    // call.

    // If g_GCShadow is 0, don't perform the check.
    if (g_GCShadow != NULL)
    {
        // Compute the shadow heap address corresponding to the beginning of the range of heap addresses modified
        // and in the process range check it to make sure we have the shadow version allocated.
        uintptr_t* shadowSlot = (uintptr_t*)(g_GCShadow + ((uint8_t*)pMemStart - g_lowest_address));
        if (shadowSlot < (uintptr_t*)g_GCShadowEnd)
        {
            // Iterate over every pointer sized slot in the range, copying data from the real heap to the shadow heap.
            // As we perform each copy we need to recheck the real heap contents with an ordered read to ensure we're
            // not racing with another heap updater. If we discover a race we invalidate the corresponding shadow heap
            // slot using a special well-known value so that this location will not be tested during the next shadow
            // heap validation.

            uintptr_t* realSlot = (uintptr_t*)pMemStart;
            ptrdiff_t slotCount = (ptrdiff_t)(cbMemSize / sizeof(uintptr_t));
            ASSERT(slotCount < (uintptr_t*)g_GCShadowEnd - shadowSlot);
            do
            {
                // Update shadow slot from real slot.
                uintptr_t realValue = *realSlot;
                *shadowSlot = realValue;
                // Memory barrier to ensure the next read is ordered wrt to the shadow heap write we just made.
                PalMemoryBarrier();

                // Read the real slot contents again. If they don't agree with what we just wrote then someone just raced
                // with us and updated the heap again. In such cases we invalidate the shadow slot.
                if (*realSlot != realValue)
                {
                    *shadowSlot = INVALIDGCVALUE;
                }

                realSlot++;
                shadowSlot++;
                slotCount--;
            }
            while (slotCount > 0);
        }
    }

#endif // WRITE_BARRIER_CHECK

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    if (g_sw_ww_enabled_for_gc_heap)
    {
        SoftwareWriteWatchSetDirtyRegion(pMemStart, cbMemSize);
    }
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

    // Compute the starting card address and the number of bytes to write (groups of 8 cards). We could try
    // for further optimization here using aligned 32-bit writes but there's some overhead in setup required
    // and additional complexity. It's not clear this is warranted given that a single byte of card table
    // update already covers 1K of object space (2K on 64-bit platforms). It's also not worth probing that
    // 1K/2K range to see if any of the pointers appear to be non-ephemeral GC references. Given the size of
    // the area the chances are high that at least one interesting GC refenence is present.

    size_t startAddress = (size_t)pMemStart;
    size_t endAddress = startAddress + cbMemSize;
    size_t startingClump = startAddress >> LOG2_CLUMP_SIZE;
    size_t endingClump = (endAddress + CLUMP_SIZE - 1) >> LOG2_CLUMP_SIZE;

    // calculate the number of clumps to mark (round_up(end) - start)
    size_t clumpCount = endingClump - startingClump;
    // VolatileLoadWithoutBarrier() is used here to prevent fetch of g_card_table from being reordered
    // with g_lowest/highest_address check at the beginning of this function.
    uint8_t* card = ((uint8_t*)VolatileLoadWithoutBarrier(&g_card_table)) + startingClump;

    // Fill the cards. To avoid cache line thrashing we check whether the cards have already been set before
    // writing.
    do
    {
        if (*card != 0xff)
        {
            *card = 0xff;
        }

        card++;
        clumpCount--;
    }
    while (clumpCount != 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    size_t startBundleByte = startAddress >> card_bundle_byte_shift;
    size_t endBundleByte = (endAddress + (1 << card_bundle_byte_shift) - 1) >> card_bundle_byte_shift;
    size_t bundleByteCount = endBundleByte - startBundleByte;

    uint8_t* pBundleByte = ((uint8_t*)VolatileLoadWithoutBarrier(&g_card_bundle_table)) + startBundleByte;

    do
    {
        if (*pBundleByte != 0xFF)
        {
            *pBundleByte = 0xFF;
        }

        pBundleByte++;
        bundleByteCount--;
    } while (bundleByteCount != 0);
#endif
}
#endif // DACCESS_COMPILE
