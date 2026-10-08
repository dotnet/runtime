// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriAllocator.h
//

#ifndef __SATORI_ALLOCATOR_H__
#define __SATORI_ALLOCATOR_H__

#include "common.h"
#include "../gc.h"
#include "SatoriUtil.h"

#include "SatoriRegionQueue.h"

class SatoriHeap;
class SatoriRegion;
class SatoriObject;
class SatoriAllocationContext;
class SatoriWorkChunk;
class SatoriWorkList;

enum AllocationTickKind
{
    Small = 0,
    Large = 1,
    Pinned = 2,
};

class SatoriAllocator
{
public:
    void Initialize(SatoriHeap* heap);
    Object* Alloc(SatoriAllocationContext* context, size_t size, uint32_t flags);

    SatoriRegion* GetRegion(size_t minSize);
    void AddRegion(SatoriRegion* region);
    void ReturnRegion(SatoriRegion* region);
    void ReturnRegionNoLock(SatoriRegion * region);

    void AllocationTickIncrement(AllocationTickKind isSmall, size_t totalAdded, SatoriObject* obj, size_t obj_size);
    void AllocationTickDecrement(size_t totalUnused);

    SatoriWorkChunk* TryGetWorkChunk();
    SatoriWorkChunk* GetWorkChunk();
    void ReturnWorkChunk(SatoriWorkChunk* chunk);

    void DeactivateSharedRegion(SatoriRegion* region, bool promoteAllRegions);
    void DeactivateSharedRegions(bool promoteAllRegions);

private:
    SatoriHeap* m_heap;
    SatoriRegionQueue* m_queues[Satori::ALLOCATOR_BUCKET_COUNT];
    SatoriWorkList* m_workChunks;

    SatoriRegion* m_regularRegion;
    SatoriRegion* m_largeRegion;
    SatoriRegion* m_pinnedRegion;
    SatoriRegion* m_immortalRegion;

    DECLSPEC_ALIGN(64)
    SatoriLock m_regularAllocLock;

    DECLSPEC_ALIGN(64)
    SatoriLock m_immortalAllocLock;

    DECLSPEC_ALIGN(64)
    SatoriLock m_largeAllocLock;

    DECLSPEC_ALIGN(64)
    SatoriLock m_pinnedAllocLock;

    // for event trace
    DECLSPEC_ALIGN(64)
    size_t m_largeAllocTickAmount;

    DECLSPEC_ALIGN(64)
    size_t m_pinnedAllocTickAmount;

    DECLSPEC_ALIGN(64)
    size_t m_smallAllocTickAmount;

private:
    DECLSPEC_ALIGN(64)
    volatile int32_t m_singePageAdders;

    void UpdateAllocStatsAndHelpIfNeeded(SatoriAllocationContext * context);
    SatoriObject* AllocRegular(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocRegularShared(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocLarge(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocLargeShared(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocHuge(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocPinned(SatoriAllocationContext* context, size_t size, uint32_t flags);
    SatoriObject* AllocImmortal(SatoriAllocationContext* context, size_t size, uint32_t flags);

    void TryGetRegularRegion(SatoriRegion*& region);
    bool AddMoreWorkChunks();

    static int SizeToBucket(size_t size)
    {
        _ASSERTE(size >= Satori::REGION_SIZE_GRANULARITY);

        DWORD highestBit;
#ifdef HOST_64BIT
        BitScanReverse64(&highestBit, size);
#else
        BitScanReverse(&highestBit, value);
#endif
        return min((int)highestBit - Satori::REGION_BITS, Satori::ALLOCATOR_BUCKET_COUNT - 1);
    }
};

#endif
