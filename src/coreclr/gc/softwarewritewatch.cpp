// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "env/gcenv.os.h"
#include "softwarewritewatch.h"

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#ifndef DACCESS_COMPILE

static_assert((static_cast<size_t>(1) << SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift) == WRITE_WATCH_UNIT_SIZE, "Unexpected WRITE_WATCH_UNIT_SIZE");

extern "C"
{
    uint8_t *g_gc_sw_ww_table = nullptr;
    bool g_gc_sw_ww_enabled_for_gc_heap = false;
}

void SoftwareWriteWatch::StaticClose()
{
    if (GetTable() == nullptr)
    {
        return;
    }

    g_gc_sw_ww_enabled_for_gc_heap = false;
    g_gc_sw_ww_table = nullptr;
}

bool SoftwareWriteWatch::GetDirtyFromBlock(
    uint8_t *block,
    uint8_t *firstPageAddressInBlock,
    size_t startByteIndex,
    size_t endByteIndex,
    void **dirtyPages,
    size_t *dirtyPageIndexRef,
    size_t dirtyPageCount,
    bool clearDirty)
{
    assert(block != nullptr);
    assert(ALIGN_DOWN(block, sizeof(size_t)) == block);
    assert(firstPageAddressInBlock == reinterpret_cast<uint8_t *>(GetPageAddress(block - GetTable())));
    assert(startByteIndex < endByteIndex);
    assert(endByteIndex <= sizeof(size_t));
    assert(dirtyPages != nullptr);
    assert(dirtyPageIndexRef != nullptr);

    size_t &dirtyPageIndex = *dirtyPageIndexRef;
    assert(dirtyPageIndex < dirtyPageCount);

    size_t dirtyBytes = *reinterpret_cast<size_t *>(block);
    if (dirtyBytes == 0)
    {
        return true;
    }

    if (startByteIndex != 0)
    {
        size_t numLowBitsToClear = startByteIndex * 8;
        dirtyBytes >>= numLowBitsToClear;
        dirtyBytes <<= numLowBitsToClear;
    }
    if (endByteIndex != sizeof(size_t))
    {
        size_t numHighBitsToClear = (sizeof(size_t) - endByteIndex) * 8;
        dirtyBytes <<= numHighBitsToClear;
        dirtyBytes >>= numHighBitsToClear;
    }

    while (dirtyBytes != 0)
    {
        DWORD bitIndex;
        static_assert(sizeof(size_t) <= 8, "Unexpected sizeof(size_t)");
        if (sizeof(size_t) == 8)
        {
            BitScanForward64(&bitIndex, static_cast<DWORD64>(dirtyBytes));
        }
        else
        {
            BitScanForward(&bitIndex, static_cast<DWORD>(dirtyBytes));
        }

        // Each byte is only ever set to 0 or 0xff
        assert(bitIndex % 8 == 0);
        size_t byteMask = static_cast<size_t>(0xff) << bitIndex;
        assert((dirtyBytes & byteMask) == byteMask);
        dirtyBytes ^= byteMask;

        DWORD byteIndex = bitIndex / 8;
        if (clearDirty)
        {
            // Clear only the bytes for which pages are recorded as dirty
            block[byteIndex] = 0;
        }

        void *pageAddress = firstPageAddressInBlock + byteIndex * WRITE_WATCH_UNIT_SIZE;
        assert(pageAddress >= GetHeapStartAddress());
        assert(pageAddress < GetHeapEndAddress());
        assert(dirtyPageIndex < dirtyPageCount);
        dirtyPages[dirtyPageIndex] = pageAddress;
        ++dirtyPageIndex;
        if (dirtyPageIndex == dirtyPageCount)
        {
            return false;
        }
    }
    return true;
}

void SoftwareWriteWatch::GetDirty(
    void *baseAddress,
    size_t regionByteSize,
    void **dirtyPages,
    size_t *dirtyPageCountRef,
    bool clearDirty,
    bool isRuntimeSuspended)
{
    VerifyCreated();
    VerifyMemoryRegion(baseAddress, regionByteSize);
    assert(dirtyPages != nullptr);
    assert(dirtyPageCountRef != nullptr);

    size_t dirtyPageCount = *dirtyPageCountRef;
    if (dirtyPageCount == 0)
    {
        return;
    }

    if (!isRuntimeSuspended)
    {
        // When a page is marked as dirty, a memory barrier is not issued after the write most of the time. Issue a memory
        // barrier on all active threads of the process now to make recent changes to dirty state visible to this thread.
        GCToOSInterface::FlushProcessWriteBuffers();
    }

    uint8_t *tableRegionStart;
    size_t tableRegionByteSize;
    TranslateToTableRegion(baseAddress, regionByteSize, &tableRegionStart, &tableRegionByteSize);
    uint8_t *tableRegionEnd = tableRegionStart + tableRegionByteSize;

    uint8_t *blockStart = ALIGN_DOWN(tableRegionStart, sizeof(size_t));
    assert(blockStart >= GetUntranslatedTable());
    uint8_t *blockEnd = ALIGN_UP(tableRegionEnd, sizeof(size_t));
    assert(blockEnd <= GetUntranslatedTableEnd());
    uint8_t *fullBlockEnd = ALIGN_DOWN(tableRegionEnd, sizeof(size_t));

    size_t dirtyPageIndex = 0;
    uint8_t *currentBlock = blockStart;
    uint8_t *firstPageAddressInCurrentBlock = reinterpret_cast<uint8_t *>(GetPageAddress(currentBlock - GetTable()));

    do
    {
        if (blockStart == fullBlockEnd)
        {
            if (GetDirtyFromBlock(
                    currentBlock,
                    firstPageAddressInCurrentBlock,
                    tableRegionStart - blockStart,
                    tableRegionEnd - fullBlockEnd,
                    dirtyPages,
                    &dirtyPageIndex,
                    dirtyPageCount,
                    clearDirty))
            {
                *dirtyPageCountRef = dirtyPageIndex;
            }
            break;
        }

        if (tableRegionStart != blockStart)
        {
            if (!GetDirtyFromBlock(
                    currentBlock,
                    firstPageAddressInCurrentBlock,
                    tableRegionStart - blockStart,
                    sizeof(size_t),
                    dirtyPages,
                    &dirtyPageIndex,
                    dirtyPageCount,
                    clearDirty))
            {
                break;
            }
            currentBlock += sizeof(size_t);
            firstPageAddressInCurrentBlock += sizeof(size_t) * WRITE_WATCH_UNIT_SIZE;
        }

        while (currentBlock < fullBlockEnd)
        {
            if (!GetDirtyFromBlock(
                    currentBlock,
                    firstPageAddressInCurrentBlock,
                    0,
                    sizeof(size_t),
                    dirtyPages,
                    &dirtyPageIndex,
                    dirtyPageCount,
                    clearDirty))
            {
                break;
            }
            currentBlock += sizeof(size_t);
            firstPageAddressInCurrentBlock += sizeof(size_t) * WRITE_WATCH_UNIT_SIZE;
        }
        if (currentBlock < fullBlockEnd)
        {
            break;
        }

        if (tableRegionEnd != fullBlockEnd &&
            !GetDirtyFromBlock(
                currentBlock,
                firstPageAddressInCurrentBlock,
                0,
                tableRegionEnd - fullBlockEnd,
                dirtyPages,
                &dirtyPageIndex,
                dirtyPageCount,
                clearDirty))
        {
            break;
        }

        *dirtyPageCountRef = dirtyPageIndex;
    } while (false);

    if (!isRuntimeSuspended && clearDirty && dirtyPageIndex != 0)
    {
        // When dirtying a page, the dirty state of the page is first checked to see if the page is already dirty. If already
        // dirty, the write to mark it as dirty is skipped. So, when the dirty state of a page is cleared, we need to make sure
        // the cleared state is visible to other threads that may dirty the page, before marking through objects in the page, so
        // that the GC will not miss marking through dirtied objects in the page. Issue a memory barrier on all active threads
        // of the process now.
        MemoryBarrier(); // flush writes from this thread first to guarantee ordering
        GCToOSInterface::FlushProcessWriteBuffers();
    }
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
