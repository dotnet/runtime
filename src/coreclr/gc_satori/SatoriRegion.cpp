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
// SatoriRegion.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../env/gcenv.ee.h"
#include "../gceventstatus.h"

#if !defined(_DEBUG)
//#pragma optimize("gty", on)
#endif

#include "SatoriGC.h"
#include "SatoriAllocator.h"
#include "SatoriRecycler.h"
#include "SatoriUtil.h"
#include "SatoriObject.h"
#include "SatoriObject.inl"
#include "SatoriRegion.h"
#include "SatoriRegion.inl"
#include "SatoriPage.h"
#include "SatoriPage.inl"
#include "SatoriQueue.h"
#include "SatoriWorkChunk.h"

const int SatoriRegion::MAX_LARGE_OBJ_SIZE = Satori::REGION_SIZE_GRANULARITY - Satori::MIN_FREE_SIZE - offsetof(SatoriRegion, m_firstObject);

SatoriRegion* SatoriRegion::InitializeAt(SatoriPage* containingPage, size_t address, size_t regionSize, size_t committed, size_t used)
{
    _ASSERTE(used <= committed);
    _ASSERTE(regionSize % Satori::REGION_SIZE_GRANULARITY == 0);

    SatoriRegion* result = (SatoriRegion*)address;

    committed = max(address, committed);
    used = max(address, used);

    // make sure the header + minZero is comitted
    ptrdiff_t toCommit = (size_t)&result->m_syncBlock + SatoriUtil::MinZeroInitSize() - committed;
    if (toCommit > 0)
    {
        toCommit = ALIGN_UP(toCommit, SatoriUtil::CommitGranularity());
        _ASSERTE(committed + (size_t)toCommit <= address + regionSize);
        if (!GCToOSInterface::VirtualCommit((void*)committed, toCommit))
        {
            // OOM
            return nullptr;
        }

        containingPage->Heap()->IncBytesCommitted(toCommit);
        committed += toCommit;
    }

    _ASSERTE(BITMAP_START * sizeof(size_t) == offsetof(SatoriRegion, m_firstObject) / sizeof(size_t) / 8);

    // clear the header if was used before
    size_t zeroUpTo = min(used, (size_t)&result->m_syncBlock);
    memset((void*)address, 0, zeroUpTo - address);

    // consider the header dirty for the future zeroing.
    used = max((size_t)&result->m_syncBlock, used);

    result->m_end = address + regionSize;
    result->m_committed = min(committed, address + regionSize);
    result->m_used = min(used, address + regionSize);
    result->m_containingPage = containingPage;

    result->m_allocStart = (size_t)&result->m_firstObject;
    result->m_allocEnd = result->End();
    result->m_occupancy = result->m_allocEnd - result->m_allocStart;
    result->m_demotedOccupancy = 0;
    result->m_escapeFunc = nullptr;
    result->m_generation = -1;

    result->m_containingPage->OnRegionInitialized(result);
    return result;
}

SatoriAllocator* SatoriRegion::Allocator()
{
    return m_containingPage->Heap()->Allocator();
}

SatoriRecycler* SatoriRegion::Recycler()
{
    return m_containingPage->Heap()->Recycler();
}

// rearm cards for a tenured region (ex: after en-masse promotion)
void SatoriRegion::RearmCardsForTenured()
{
    _ASSERTE(Generation() == 2);
    m_containingPage->ResetCardsForRange(Start(), End(), /* tenured */ true);
    HasUnmarkedDemotedObjects() = false;

    FreeDemotedTrackers();
}

void SatoriRegion::FreeDemotedTrackers()
{
    while (DemotedObjects())
    {
        SatoriWorkChunk* gen2Objects = DemotedObjects();
        DemotedObjects() = gen2Objects->Next();
        gen2Objects->SetNext(nullptr);
        gen2Objects->Clear();
        Allocator()->ReturnWorkChunk(gen2Objects);
    }

    m_demotedOccupancy = 0;
}

// reset all cards when the region will no longer be tenured.
void SatoriRegion::ResetCardsForEphemeral()
{
    _ASSERTE(Generation() == 2);
    m_containingPage->ResetCardsForRange(Start(), End(), /* tenured */ false);
}

void SatoriRegion::MakeBlank()
{
    _ASSERTE(NothingMarked());

    if (m_generation == 2)
    {
        this->ResetCardsForEphemeral();
    }
    else
    {
        m_containingPage->WipeGroupsForRange(Start(), End());
    }

    m_ownerThreadTag = 0;
    m_escapeFunc = EscapeFn;
    m_generation = -1;
    m_occupancyAtReuse = 0;

    // m_end                    stays the same
    // m_containingPage         stays the same

    m_reusableFor = ReuseLevel::None;
    _ASSERTE(!m_allocatingOwnerAttachmentPoint);
    _ASSERTE(!m_gen2Objects);

    m_allocStart = (size_t)&m_firstObject;
    m_allocEnd = End();

    // m_used                    stays the same
    // m_committed               stays the same

    m_escapedSize = 0;
    _ASSERTE(!m_markStack);

    m_allocBytesAtCollect = 0;

    _ASSERTE(!m_finalizableTrackers);
    _ASSERTE(!m_finalizableTrackersLock);

    m_sweepsSinceLastAllocation = 0;

    // m_prev
    // m_next
    // m_containingQueue        all stay the same

    // assume all space reserved to allocations will be used
    // (we will revert what will be unused)
    m_occupancy = m_allocEnd - m_allocStart;
    // this may be not 0 if we demoted without creating trackers before Gen2 gc
    // and then all objects died in Gen2, so we clear this here.
    m_demotedOccupancy = 0;
    m_objCount = 0;

    m_unfinishedAllocationCount = 0;

    m_hasPinnedObjects = false;
    m_hasFinalizables = false;
    _ASSERTE(!m_hasPendingFinalizables);
    m_doNotSweep = false;
    m_isPreSwept = false;
    m_isRelocated = false;
    _ASSERTE(!m_acceptedPromotedObjects);
    _ASSERTE(!m_individuallyPromoted);
    _ASSERTE(!m_hasUnmarkedDemotedObjects);

#if _DEBUG
    m_hasMarksSet = false;
#endif

    //clear index and free list
    ClearFreeLists();
    ClearIndex();

#ifdef JUNK_FILL_FREE_SPACE
    memset(&m_syncBlock, 0xFE, m_used - (size_t)&m_syncBlock);
#endif
}

bool SatoriRegion::ValidateBlank()
{
    if (!IsAllocating())
    {
        _ASSERTE(false);
        return false;
    }

    if (Start() < m_containingPage->RegionsStart() || End() > m_containingPage->End())
    {
        _ASSERTE(false);
        return false;
    }

    if (Size() % Satori::REGION_SIZE_GRANULARITY != 0)
    {
        _ASSERTE(false);
        return false;
    }

    if (m_committed > End() || m_committed < (size_t)&m_syncBlock)
    {
        _ASSERTE(false);
        return false;
    }

    if (m_used > m_committed || m_used < (size_t)&m_syncBlock)
    {
        _ASSERTE(false);
        return false;
    }

    if (m_allocStart != (size_t)&m_firstObject || m_allocEnd != m_end)
    {
        _ASSERTE(false);
        return false;
    }

    if (!ValidateIndexEmpty())
    {
        return false;
    }

    if (m_finalizableTrackers)
    {
        _ASSERTE(false);
        return false;
    }

    return true;
}

bool SatoriRegion::ValidateIndexEmpty()
{
    for (int i = 0; i < Satori::INDEX_LENGTH; i += Satori::INDEX_LENGTH / 4)
    {
        _ASSERTE(m_index[i] == 0);
        if (m_index[i] != 0)
        {
            return false;
        }
    }

    return true;
}

size_t SatoriRegion::StartAllocating(size_t minAllocSize)
{
    _ASSERTE(!IsAllocating());

    SatoriFreeListObject* freeObj = nullptr;
    DWORD bucket;

    // Free list objects are bucketized by capacity (not by size!).
    // A free list obj of capacity X can accept allocation
    // of an object of size X with MIN_FREE_SIZE remaining
    // for the parseability terminator.

    // skip buckets that certainly will not fit.
    if (minAllocSize <= Satori::MIN_FREELIST_CAPACITY)
    {
        bucket = 0;
    }
    else
    {
        BitScanReverse64(&bucket, minAllocSize);
        bucket = bucket - Satori::MIN_FREELIST_CAPACITY_BITS;
        _ASSERTE(bucket >= 0 && bucket < Satori::FREELIST_COUNT);

        // this bucket _may_ contain large enough free objects.
        // we will opportunistically test the first one, but will not go further.
        freeObj = m_freeLists[bucket];
        if (freeObj)
        {
            size_t capacity = freeObj->FreeObjCapacity();
            if (capacity >= minAllocSize)
            {
                goto hasObj;
            }
        }

        // skip to the next bucket, which will certainly fit if nonempty
        bucket++;
    }

    for (; bucket < Satori::FREELIST_COUNT; bucket++)
    {
        freeObj = m_freeLists[bucket];
        if (freeObj)
        {
            goto hasObj;
        }
    }

    return 0;

hasObj:
    SatoriFreeListObject* next = freeObj->m_nextInFreeList;
    m_freeLists[bucket] = next;
    if (next == nullptr)
    {
        m_freeListTails[bucket] = nullptr;
    }

    size_t freeObjSize = freeObj->FreeObjSize();
    _ASSERTE(freeObjSize >= minAllocSize + Satori::MIN_FREE_SIZE);
    _ASSERTE(freeObj->FreeObjCapacity() ==  freeObjSize - Satori::MIN_FREE_SIZE);

    m_freeListCapacities[bucket] -= (freeObjSize - Satori::MIN_FREE_SIZE);
    _ASSERTE(m_freeLists[bucket] != nullptr || m_freeListCapacities[bucket] == 0);
    m_allocStart = freeObj->Start();
    m_allocEnd = m_allocStart + freeObjSize;
    // Assume all will be used.
    // We will deduct back the unused part when we stop allocating.
    SetOccupancy(m_occupancy + freeObjSize);
    ClearIndicesForAllocRange();
    _ASSERTE(GetAllocRemaining() >= minAllocSize);
    m_sweepsSinceLastAllocation = 0;

    return m_allocStart;
}

void SatoriRegion::StopAllocating(size_t allocPtr)
{
    _ASSERTE(IsAllocating());
    _ASSERTE(allocPtr != 0);

    // Make unused allocation span parseable
    // Unbuffered allocs like Huge, will consume whole allocation.
    // Buffered/FreeList allocs will have extra space enough to fit a terminator.
    if (allocPtr != m_allocEnd)
    {
        m_used = max(m_used, allocPtr + Satori::MIN_FREE_SIZE);
        size_t unused = m_allocEnd - allocPtr;
        _ASSERTE(m_occupancy >= unused);
        SetOccupancy(m_occupancy - unused);
        SatoriObject* freeObj = SatoriObject::FormatAsFree(allocPtr, unused);
        ReturnFreeSpace(freeObj, unused);
    }

    m_allocStart = m_allocEnd = 0;
}

void SatoriRegion::StopAllocating()
{
    StopAllocating(m_allocStart);
}

void SatoriRegion::AddFreeSpace(SatoriObject* freeObj, size_t size)
{
    _ASSERTE(freeObj->Size() == size);
    _ASSERTE(size >= Satori::MIN_FREE_SIZE);
    // capacity is smaller than size to make sure the span can always be made parseable
    // after allocating objects in it.
    size_t capacity = size - Satori::MIN_FREE_SIZE;
    if (capacity < Satori::MIN_FREELIST_CAPACITY)
    {
        return;
    }

    SatoriFreeListObject* freeListObj = (SatoriFreeListObject*)freeObj;
    DWORD bucket;
    BitScanReverse64(&bucket, capacity);
    bucket -= (Satori::MIN_FREELIST_CAPACITY_BITS);
    _ASSERTE(bucket >= 0);
    _ASSERTE(bucket < Satori::FREELIST_COUNT);

    _ASSERTE(m_freeLists[bucket] != nullptr || m_freeListCapacities[bucket] == 0);
    _ASSERTE(capacity == freeListObj->FreeObjCapacity());
    m_freeListCapacities[bucket] += capacity;

    // insert at the tail
    freeListObj->m_nextInFreeList = nullptr;
    if (m_freeLists[bucket] == nullptr)
    {
        m_freeLists[bucket] = m_freeListTails[bucket] = freeListObj;
        return;
    }

    SatoriFreeListObject* tailObj = m_freeListTails[bucket];
    _ASSERTE(tailObj);
    tailObj->m_nextInFreeList = freeListObj;
    m_freeListTails[bucket] = freeListObj;
}

void SatoriRegion::ReturnFreeSpace(SatoriObject* freeObj, size_t size)
{
    _ASSERTE(freeObj->Size() == size);
    _ASSERTE(size >= Satori::MIN_FREE_SIZE);
    // capacity is smaller than size to make sure the span can always be made parseable
    // after allocating objects in it.
    size_t capacity = size - Satori::MIN_FREE_SIZE;
    if (capacity < Satori::MIN_FREELIST_CAPACITY)
    {
        return;
    }

    SatoriFreeListObject* freeListObj = (SatoriFreeListObject*)freeObj;
    DWORD bucket;
    BitScanReverse64(&bucket, capacity);
    bucket -= (Satori::MIN_FREELIST_CAPACITY_BITS);
    _ASSERTE(bucket >= 0);
    _ASSERTE(bucket < Satori::FREELIST_COUNT);

    _ASSERTE(m_freeLists[bucket] != nullptr || m_freeListCapacities[bucket] == 0);
    _ASSERTE(capacity == freeListObj->FreeObjCapacity());
    m_freeListCapacities[bucket] += capacity;

    // insert at the head, since we are returning what we recently took.
    freeListObj->m_nextInFreeList = m_freeLists[bucket];

    if (m_freeLists[bucket] == nullptr)
    {
        m_freeListTails[bucket] = freeListObj;
    }

    m_freeLists[bucket] = freeListObj;
}

bool SatoriRegion::HasFreeSpaceInTopBucket()
{
    return m_freeLists[Satori::FREELIST_COUNT - 1];
}

size_t SatoriRegion::FreeSpaceInTopNBuckets(int n)
{
    size_t result = 0;
    for (int bucket = Satori::FREELIST_COUNT -  n; bucket < Satori::FREELIST_COUNT; bucket++)
    {
        result += m_freeListCapacities[bucket];
    }

    return result;
}

int SatoriRegion::GetMaxFreeBucket()
{
    for (int bucket = Satori::FREELIST_COUNT - 1; bucket >= 0; bucket--)
    {
        if (m_freeLists[bucket])
        {
            return bucket;
        }
    }

    return -1;
}

void SatoriRegion::SplitCore(size_t regionSize, size_t& nextStart, size_t& nextCommitted, size_t& nextUsed)
{
    _ASSERTE(regionSize % Satori::REGION_SIZE_GRANULARITY == 0);
    _ASSERTE(regionSize < this->Size());
    _ASSERTE(m_allocEnd == m_end);
    _ASSERTE(m_allocStart == m_firstObject.Start());

    size_t newEnd = m_end - regionSize;
    nextStart = newEnd;
    nextCommitted = m_committed;
    nextUsed = m_used;

    m_end = newEnd;
    m_committed = min(newEnd, m_committed);
    m_used = min(newEnd, m_used);
    m_allocEnd = newEnd;
    m_occupancy = m_allocEnd - m_allocStart;

    _ASSERTE(Size() >= Satori::REGION_SIZE_GRANULARITY);
}

void SatoriRegion::UndoSplitCore(size_t regionSize, size_t nextStart, size_t nextCommitted, size_t nextUsed)
{
    _ASSERTE(m_allocEnd == m_end);
    _ASSERTE(m_allocStart == m_firstObject.Start());
    _ASSERTE(nextStart == m_end);

    m_end += regionSize;
    m_committed = max(m_committed, nextCommitted);
    m_used = max(m_used, nextUsed);
    m_allocEnd = m_end;
    m_occupancy = m_allocEnd - m_allocStart;
}

SatoriRegion* SatoriRegion::TrySplit(size_t regionSize)
{
    size_t nextStart, nextCommitted, nextUsed;
    SplitCore(regionSize, nextStart, nextCommitted, nextUsed);

    // format the rest as a new region 
    SatoriRegion* result = InitializeAt(m_containingPage, nextStart, regionSize, nextCommitted, nextUsed);
    if (result == nullptr)
    {
        // OOM
        this->UndoSplitCore(regionSize, nextStart, nextCommitted, nextUsed);
    }
    else
    {
        _ASSERTE(result->ValidateBlank());
    }

    return result;
}

SatoriPage* SatoriRegion::ContainingPage()
{
    return m_containingPage;
}

SatoriRegion* SatoriRegion::NextInPage()
{
    return m_containingPage->NextInPage(this);
}

bool SatoriRegion::CanCoalesceWithNext()
{
    SatoriRegion* next = NextInPage();
    if (next)
    {
        auto queue = VolatileLoadWithoutBarrier(&next->m_containingQueue);
        if (queue && queue->Kind() == QueueKind::Allocator)
        {
            return true;
        }
    }

    return false;
}

bool SatoriRegion::Coalesce(SatoriRegion* next)
{
    _ASSERTE(next->m_containingQueue == nullptr);
    _ASSERTE(next->m_containingPage == m_containingPage);
    _ASSERTE(next->m_prev == next->m_next);
    _ASSERTE(m_end == next->Start());
    _ASSERTE(ValidateBlank());
    _ASSERTE(next->ValidateBlank());

    size_t next_end = next->m_end;
    size_t next_allocEnd = next->m_allocEnd;

    if (m_committed == next->Start())
    {
        m_committed = next->m_committed;
        m_used = next->m_used;
    }
    else
    {
        _ASSERTE(next->m_committed > next->Start());
        size_t toDecommit = next->m_committed - next->Start();
        _ASSERTE(toDecommit % SatoriUtil::CommitGranularity() == 0);
        if (!GCToOSInterface::VirtualDecommit(next, toDecommit))
        {
            return false;
        }

        m_containingPage->Heap()->DecBytesCommitted(toDecommit);
    }

    m_end = next_end;
    m_allocEnd = next_allocEnd;
    m_containingPage->OnRegionInitialized(this);

    return true;
}

bool SatoriRegion::TryCoalesceWithNext()
{
    SatoriRegion* next = NextInPage();
    if (next)
    {
        auto queue = VolatileLoadWithoutBarrier(&next->m_containingQueue);
        if (queue && queue->Kind() == QueueKind::Allocator)
        {
            if (queue->TryRemove(next))
            {
                if(Coalesce(next))
                {
                    return true;
                }

                // could not coalesce, put the next back.
                queue->Enqueue(next);
            }
        }
    }

    return false;
}

bool SatoriRegion::CanDecommit()
{
    size_t decommitStart = ALIGN_UP((size_t)&m_syncBlock, SatoriUtil::CommitGranularity());
    if (decommitStart > m_committed)
    {
        _ASSERTE(SatoriUtil::CommitGranularity() > Satori::REGION_SIZE_GRANULARITY);
        return false;
    }

    size_t decommitSize = m_committed - decommitStart;
    return decommitSize > 0;
}

bool SatoriRegion::TryDecommit()
{
    size_t decommitStart = ALIGN_UP((size_t)&m_syncBlock, SatoriUtil::CommitGranularity());
    if (decommitStart > m_committed)
    {
        _ASSERTE(SatoriUtil::CommitGranularity() > Satori::REGION_SIZE_GRANULARITY);
        return false;
    }

    size_t decommitSize = m_committed - decommitStart;
    if (decommitSize > 0)
    {
        if (GCToOSInterface::VirtualDecommit((void*)decommitStart, decommitSize))
        {
            m_committed = decommitStart;
            m_used = min(m_used, decommitStart);
            m_containingPage->Heap()->DecBytesCommitted(decommitSize);
            return true;
        }
    }

    return false;
}

void SatoriRegion::TryCommit()
{
    if (m_committed < m_end)
    {
        size_t toCommit =  m_end - m_committed;
        if (GCToOSInterface::VirtualCommit((void*)m_committed, toCommit))
        {
            m_containingPage->Heap()->IncBytesCommitted(toCommit);
            m_committed = m_end;
        }
    }
}

void SatoriRegion::ZeroInitAndLink(SatoriRegion* prev)
{
    if (m_used > (size_t)&m_syncBlock)
    {
        memset(&m_syncBlock, 0, m_used - (size_t)&m_syncBlock);
    }

    m_next = prev;
    if (prev)
    {
        prev->m_prev = this;
    }
}

size_t SatoriRegion::Allocate(size_t size, bool zeroInitialize)
{
    _ASSERTE(m_containingQueue == nullptr);

    size_t chunkStart = m_allocStart;
    size_t chunkEnd = chunkStart + size;
    if (chunkEnd <= m_allocEnd - Satori::MIN_FREE_SIZE)
    {
        // commit a bit more - in case we need to append a parseability terminator.
        size_t ensureCommitted = chunkEnd + Satori::MIN_FREE_SIZE;
        if (ensureCommitted > m_committed)
        {
            size_t newComitted = ALIGN_UP(ensureCommitted, SatoriUtil::CommitGranularity());
            size_t toCommit = newComitted - m_committed;
            if (!GCToOSInterface::VirtualCommit((void*)m_committed, toCommit))
            {
                return 0;
            }

            m_containingPage->Heap()->IncBytesCommitted(toCommit);
            m_committed = newComitted;
        }

        if (zeroInitialize)
        {
            size_t zeroUpTo = min(m_used, chunkEnd);
            ptrdiff_t zeroInitCount = zeroUpTo - chunkStart;
            if (zeroInitCount > 0)
            {
                memset((void*)chunkStart, 0, zeroInitCount);
            }
        }

        m_allocStart = chunkEnd;
        m_used = max(m_used, chunkEnd);
        return chunkStart;
    }

    return 0;
}

size_t SatoriRegion::AllocateHuge(size_t size, bool zeroInitialize)
{
    _ASSERTE(ValidateBlank());
    _ASSERTE(GetAllocRemaining() >= size);
    _ASSERTE(m_allocEnd > Start() + Satori::REGION_SIZE_GRANULARITY);

    size_t chunkStart = m_allocStart;
    size_t chunkEnd = chunkStart + size;

    // in rare cases the object does not cross into the last granule.
    // (when the allocation is huge only because of parseability padding).
    // in such case pad it in front.
    if (chunkEnd < End() - Satori::REGION_SIZE_GRANULARITY)
    {
        chunkStart += Satori::MIN_FREE_SIZE;
        chunkEnd += Satori::MIN_FREE_SIZE;
    }

    // huge allocation should cross the end of the first granule.
    _ASSERTE(chunkEnd > Start() + Satori::REGION_SIZE_GRANULARITY);
    _ASSERTE(chunkStart <= Start() + Satori::REGION_SIZE_GRANULARITY);

    size_t ensureCommitted = chunkEnd + Satori::MIN_FREE_SIZE;
    if (ensureCommitted > m_committed)
    {
        size_t newComitted = ALIGN_UP(ensureCommitted, SatoriUtil::CommitGranularity());
        size_t toCommit = newComitted - m_committed;
        if (!GCToOSInterface::VirtualCommit((void*)m_committed, toCommit))
        {
            return 0;
        }

        m_containingPage->Heap()->IncBytesCommitted(toCommit);
        m_committed = newComitted;
    }

    if (zeroInitialize)
    {
        size_t zeroInitStart = chunkStart;
        size_t zeroUpTo = min(m_used, chunkEnd);
        ptrdiff_t zeroInitCount = zeroUpTo - zeroInitStart;
        if (zeroInitCount > 0)
        {
            memset((void*)zeroInitStart, 0, zeroInitCount);
        }
    }

    m_used = max(m_used, chunkEnd);

    // if did not allocate from start, there could be MIN_FREE_SIZE fillable space before the obj.
    m_allocEnd = chunkStart;

    // space after chunkEnd is a waste, no normal object can start there.
    // if there is dirty space after the chunkEnd,
    // zero-out what would be the next obj MT in debug - to catch if we walk there by accident.
#ifdef _DEBUG
    if (m_used - chunkEnd > sizeof(size_t))
    {
        *((size_t*)chunkEnd) = 0;
    }
#endif
    _ASSERTE(m_allocEnd < Start() + Satori::REGION_SIZE_GRANULARITY);
    return chunkStart;
}

// Finds an object that contains the given location.
//
// Assumptions that caller must arrange or handle:
//  - we may return a Free object here.
//  - locations before the first object and after the last object match to the First/Last objects
//  - location must not be inside unparseable unconsumed budget when actively allocating.
//
// Typical uses:
//  - precise root marking.
//         not in allocating mode (not attached to allocation context, parsable)
//         always provides real refs into real objects.
//  - conservative root marking
//         not in allocating mode
//         can give refs pointing to Free
//  - escape checks for array copying.
//         always provides refs into real objects
//         may be in allocation mode, but uses the region owned by current thread, thus no allocations could happen concurrently.
//  - iterating over card table - 
//         not in allocating mode
//         can give refs pointing to Free. (because of card granularity)
SatoriObject* SatoriRegion::FindObject(size_t location)
{
    _ASSERTE(m_generation >= 0);
    _ASSERTE(location >= Start());
    _ASSERTE(location < End());
    _ASSERTE(m_unfinishedAllocationCount == 0);

    location = min(location, Start() + Satori::REGION_SIZE_GRANULARITY);

    // start search from the first object or after unparseable alloc gap
    SatoriObject* o = (IsAllocating() && (location >= m_allocEnd)) ?
                                 (SatoriObject*)m_allocEnd :
                                 FirstObject();

    // use a better start obj if we have one in the index
    size_t limit = LocationToIndex(o->Start());
    for (size_t current = LocationToIndex(location); current > limit; current--)
    {
        int offset = m_index[current];
        if (offset)
        {
            SatoriObject* indexed = (SatoriObject*)(Start() + offset);
            if (indexed->Start() > o->Start())
            {
                o = indexed;
                if (o->End() > location)
                {
                    return o;
                }
            }

            break;
        }
    }

    // walk to the location and update index on the way
    SatoriObject* next = o->Next();
    while (next->Start() <= location)
    {
        SetIndicesForObject(o, next->Start());

        o = next;
        next = next->Next();
    }

    return o;
}

template <bool isConservative>
void SatoriRegion::MarkFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags)
{
    SatoriRegion* region = (SatoriRegion*)sc->_unused1;
    size_t location = (size_t)*ppObject;

    // ignore objects outside of the current region.
    // this also rejects nulls and byrefs pointing to stack.
    if (location < (size_t)region->FirstObject() || location >= region->End())
    {
        return;
    }

    SatoriObject* o = (SatoriObject*)location;
    if (flags & GC_CALL_INTERIOR)
    {
        o = region->FindObject(location);
        if (isConservative && o->IsFree())
        {
            return;
        }
    }

    if (!region->IsMarked(o))
    {
        region->SetMarked(o);
        region->m_occupancy += o->Size();
        region->PushToMarkStackIfHasPointers(o);
    }

    if (flags & GC_CALL_PINNED)
    {
        if (!region->IsEscaped(o))
        {
            region->SetPinned(o);
        }
    }
}

inline void SatoriRegion::PushToMarkStackIfHasPointers(SatoriObject* obj)
{
    _ASSERTE(obj->SameRegion(this));
    _ASSERTE(!obj->GetNextInLocalMarkStack());

    if (obj->RawGetMethodTable()->ContainsGCPointersOrCollectible())
    {
        obj->SetNextInLocalMarkStack(m_markStack);
        _ASSERTE(m_markStack == obj->GetNextInLocalMarkStack());
        m_markStack = (int32_t)(obj->Start() - this->Start());
    }
}

inline SatoriObject* SatoriRegion::PopFromMarkStack()
{
    if (m_markStack != 0)
    {
        SatoriObject* o = (SatoriObject*)(this->Start() + m_markStack);
        m_markStack = o->GetNextInLocalMarkStack();
        o->ClearNextInLocalMarkStack();
        return o;
    }

    return nullptr;
}

bool SatoriRegion::AnyExposed(size_t first, size_t length)
{
    _ASSERTE(length % 8 == 0);

    size_t last = first + length - sizeof(size_t);
    _ASSERTE(((SatoriObject*)first)->SameRegion(this));
    _ASSERTE(((SatoriObject*)last)->SameRegion(this));

    size_t bitmapIndexF;
    size_t maskF = (size_t)-1 << ((SatoriObject*)first)->GetMarkBitAndWord(&bitmapIndexF);

    size_t bitmapIndexL;
    size_t maskL = (size_t)-1 >> (63 - ((SatoriObject*)last)->GetMarkBitAndWord(&bitmapIndexL));

    if (bitmapIndexF == bitmapIndexL)
    {
        return m_bitmap[bitmapIndexF] & (maskF & maskL);
    }

    if (m_bitmap[bitmapIndexF] & maskF)
    {
        return true;
    }

    for (size_t i = bitmapIndexF + 1; i < bitmapIndexL; i++)
    {
        if (m_bitmap[i])
        {
            return true;
        }
    }

    return m_bitmap[bitmapIndexL] & maskL;
}

void SatoriRegion::EscapeRecursively(SatoriObject* o)
{
    _ASSERTE(this->IsEscapeTrackedByCurrentThread());
    _ASSERTE(o->SameRegion(this));

    if (IsEscaped(o))
    {
        return;
    }

    SetEscaped(o);
    m_escapedSize += (int32_t)o->Size();

    // now recursively mark all the objects reachable from escaped object.
    do
    {
        _ASSERTE(IsEscaped(o));
        o->ForEachObjectRef(
            [&](SatoriObject** ref)
            {
                // no refs should be exposed yet, except if the ref is the first field,
                // we use that to escape whole object.
                _ASSERTE(!IsExposed(ref) || ((size_t)o == (size_t)ref - sizeof(size_t)));

                // mark ref location as exposed
                SetExposed(ref);

                // recursively escape all currently reachable objects
                SatoriObject* child = *ref;
                if (child->SameRegion(this) && !IsEscaped(child))
                {
                    SetEscaped(child);
                    m_escapedSize += (int32_t)child->Size();
                    PushToMarkStackIfHasPointers(child);
                }
            }
        );

        o = PopFromMarkStack();
    } while (o);
}

void SatoriRegion::EscapeAll()
{
    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;
    for (SatoriObject* o = FirstObject(); o->Start() < objLimit;)
    {
        size_t size = o->Size();
        if (!o->IsFree())
        {
            EscapeShallow(o, size);
        }

        o = (SatoriObject*)(o->Start() + size);
    }
}

// do not recurse into children
// used when escaping all objects in the region anyways
void SatoriRegion::EscapeShallow(SatoriObject* o, size_t size)
{
    _ASSERTE(o->SameRegion(this));
    _ASSERTE(!IsEscaped(o));
    _ASSERTE(!IsPinned(o));

    //NB: we are not checking if we exceed the escape limit here.
    //    it is possible, if we have a lot of stack roots, but highly unlikely and otherwise ok.
    //    typically objects have died and we have fewer escapes than before the GC,
    //    so we do not bother to check
    SetEscaped(o);
    m_escapedSize += (int32_t)size;

    o->ForEachObjectRef(
        [&](SatoriObject** ref)
        {
            // no refs should be exposed yet, except if the ref is the first field,
            // we use that to escape whole object.
            _ASSERTE(!IsExposed(ref) || ((size_t)o == (size_t)ref - sizeof(size_t)));

            // mark ref location as exposed
            SetExposed(ref);
        },
        size
    );
}

void SatoriRegion::SetOccupancy(size_t occupancy, int32_t objCount)
{
    _ASSERTE(objCount == 0 || occupancy != 0);
    _ASSERTE(occupancy <= (Size() - offsetof(SatoriRegion, m_firstObject)));
    m_occupancy = occupancy;
    m_objCount = objCount;
}

void SatoriRegion::SetOccupancy(size_t occupancy)
{
    _ASSERTE(m_objCount == 0 || occupancy != 0);
    _ASSERTE(occupancy <= (Size() - offsetof(SatoriRegion, m_firstObject)));
    m_occupancy = occupancy;
}

// NB: dst is unused, it is just to avoid argument shuffle in x64 barriers
void SatoriRegion::EscapeFn(SatoriObject** dst, SatoriObject* src, SatoriRegion* region)
{
    region->EscapeRecursively(src);
    if (region->m_escapedSize > Satori::MAX_ESCAPE_SIZE)
    {
        region->StopEscapeTracking();
    }
}

bool SatoriRegion::ThreadLocalCollect(size_t allocBytes)
{
    if (m_escapedSize > Satori::MAX_ESCAPE_SIZE)
    {
        return false;
    }

    if (allocBytes - m_allocBytesAtCollect < Satori::REGION_SIZE_GRANULARITY / 4)
    {
        // this is too soon. last collection did not buy us as much as we wanted
        // either due to fragmentation or unfortunate allocation pattern in this thread.
        // we will not collect this region, lest it keeps coming back.
        return false;
    }

    m_allocBytesAtCollect = allocBytes;

    size_t count = Recycler()->IncrementGen0Count();
    FIRE_EVENT(GCStart_V2, (int)count, 0, gc_reason::reason_alloc_soh, gc_etw_type_ngc);

    bool shouldCollect = ThreadLocalMark();
    if (shouldCollect)
    {
        ThreadLocalPlan();
        ThreadLocalUpdatePointers();
        ThreadLocalCompact();
    }

    // schedule pending finalizables if we have them
    // must be after compaction, since pend may escape objects.
    if (HasPendingFinalizables())
    {
        // We do not interrupt "checkFinalizables" if surv set is too large,
        // thus if we have unreachables, we have all of them and can pend them.
        ThreadLocalPendFinalizables();
    }

    FIRE_EVENT(GCEnd_V1, (int)count, 0);
    return shouldCollect;
}

bool SatoriRegion::ThreadLocalMark()
{
    Verify();

    bool checkFinalizables = m_finalizableTrackers != nullptr;

    // since this region is thread-local, we have only two kinds of live roots:
    //- those that are reachable from the current stack and
    //- those that are reachable from outside of the region (escaped)

    // mark escaped objects:
    // for every set escape bit, set the corresponding mark bit
    // nothing should be marked in the region, so we can find escapes via bit scan
    size_t bitmapIndex = BITMAP_START;
    int markBitOffset = 0;

#ifdef _DEBUG
    size_t escaped = 0;
#endif
    while (bitmapIndex < BITMAP_LENGTH)
    {
        DWORD step;
        if (BitScanForward64(&step, m_bitmap[bitmapIndex] >> markBitOffset))
        {
            // got an escape bit. its mark bit is at -1
            markBitOffset += step - 1;

            // set the mark bit
            m_bitmap[bitmapIndex + (markBitOffset >> 6)] |= ((size_t)1 << (markBitOffset & 63));

            SatoriObject* o = ObjectForMarkBit(bitmapIndex, markBitOffset);
#ifdef _DEBUG
            o->Validate();
            escaped += o->Size();
#endif

            // skip the object
            markBitOffset = o->Next()->GetMarkBitAndWord(&bitmapIndex);
        }
        else
        {
            // skip empty mark words
            markBitOffset = 0;
            while (++bitmapIndex < BITMAP_LENGTH && m_bitmap[bitmapIndex] == 0) {}
        }
    }

#ifdef _DEBUG
    _ASSERTE(escaped == this->m_escapedSize);
#endif

    // We expect high mortality rate of Gen0 objects.
    // If surviving set is large, then collection will be more expensive with diminishing results.
    // We will not continue with collection if we see that too much of thread local objects are alive.
    // The criteria does not need to be very precise. (anything in the order of 1/8 and 1/16 seems
    // to yield similar results)
    const size_t maxSurv = Satori::REGION_SIZE_GRANULARITY / 8;

    // Temporarily use m_occupancy to estimate surviving size.
    // If we proceed with collection, m_occupancy will be recomputed anyways.
    size_t uncollectedOccupancy = m_occupancy;
    m_occupancy = 0;

    // mark roots for the current stack
    ScanContext sc;
    sc.promotion = TRUE;
    sc._unused1 = this;

    if (SatoriUtil::IsConservativeMode())
        GCToEEInterface::GcScanCurrentStackRoots((promote_func*)MarkFn<true>, &sc);
    else
        GCToEEInterface::GcScanCurrentStackRoots((promote_func*)MarkFn<false>, &sc);

    // now recursively mark all the objects reachable from the stack roots.
    SatoriObject* o = PopFromMarkStack();

 propagateMarks:
    while (o && m_occupancy < maxSurv)
    {
        _ASSERTE(IsMarked(o));
        _ASSERTE(!IsEscaped(o));
        _ASSERTE(!o->IsFree());
        o->ForEachObjectRef(
            [this](SatoriObject** ref)
            {
                SatoriObject* child = *ref;
                if (child->SameRegion(this) && !IsMarked(child))
                {
                    SetMarked(child);
                    m_occupancy += child->Size();
                    PushToMarkStackIfHasPointers(child);
                }
            },
            /* includeCollectibleAllocator */ true
        );
        o = PopFromMarkStack();
    }

    if (m_occupancy >= maxSurv)
    {
        // surviving set is too large.
        while (PopFromMarkStack()){};
        m_occupancy = uncollectedOccupancy;
        return false;
    }

    if (checkFinalizables)
    {
        // unreachable finalizables:
        // - if finalized eagerly or suppressed -> drop from the list, this is garbage now.
        // else
        // - mark ref as finalizer pending to be queued after compaction.
        // - mark obj as reachable
        // - push to mark stack
        // - trace through reachable again to mark children that are now F-reachable
        ForEachFinalizableThreadLocal(
            [this](SatoriObject* finalizable)
            {
                _ASSERTE(((size_t)finalizable & Satori::FINALIZATION_PENDING) == 0);
                if (!IsMarked(finalizable))
                {
                    // NOTE: if finalization is suppressed or runs eagerly, we do not need to track any longer.
                    //       The object was never scheduled (or it'd be escaped) and now unreachable so noone can re-register it.
                    if (finalizable->IsFinalizationSuppressed() ||
                        GCToEEInterface::EagerFinalized(finalizable))
                    {
                        // stop tracking
                        return (SatoriObject*)nullptr;
                    }
                    else
                    {
                        // keep alive
                        SetMarked(finalizable);
                        m_occupancy += finalizable->Size();
                        PushToMarkStackIfHasPointers(finalizable);
                        (size_t&)finalizable |= Satori::FINALIZATION_PENDING_ANY;
                        HasPendingFinalizables() = true;
                    }
                }

                return finalizable;
            }
        );

        o = PopFromMarkStack();
        checkFinalizables = false;
        goto propagateMarks;
    }

    Verify(/*allowMarked*/ true);
    return true;
}

void SatoriRegion::ThreadLocalPlan()
{
    // object can be in 3 states here:
    //  - Free:
    //      not marked (incl. free objects, which are never marked)
    //
    //  - Unmovable:
    //      escaped or pinned
    //
    //  - Movable
    //      marked, but not escaped or pinned
    //
    // we will slide all movable objects towards the region start - as long as they fit between unmovables
    //

    // planning will coalesce free objects.
    // that does not invalidate the index, but it will if we fill free objects with junk.
#ifdef JUNK_FILL_FREE_SPACE
    ClearIndex();
#endif

    // what we want to move
    SatoriObject* moveable;
    size_t moveableSize = 0;

    // space to move to
    size_t dstStart;
    size_t dstEnd = 0;

    // stats
    size_t occupancy = 0;
    int32_t objCount = 0;

    // moveable: starts at first movable and reachable, as long as there is any free space to slide in
    size_t lastMarkedEnd = FirstObject()->Start();
    moveable = SkipUnmarked(FirstObject());
    bool sawFree = false;
    while (true)
    {
        size_t skipped = moveable->Start() - lastMarkedEnd;
        if (skipped)
        {
            if (!sawFree)
            {
                // new gap from here
                dstEnd = lastMarkedEnd;
            }

            sawFree = true;
            SatoriObject::FormatAsFree(lastMarkedEnd, skipped);
        }

        if (moveable->Start() >= End())
        {
            // nothing left to move
            _ASSERTE(moveable->Start() == End());
            goto Exit;
        }

        moveableSize = moveable->Size();
        objCount++;
        occupancy += moveableSize;

        if (sawFree && !IsEscapedOrPinned(moveable))
        {
            // reachable and moveable and saw free space. we can move this one.
            break;
        }

        lastMarkedEnd = moveable->Start() + moveableSize;
        moveable = SkipUnmarked((SatoriObject*)lastMarkedEnd);
    }

    while (true)
    {
        // dst start: skip unmovables
        for (dstStart = dstEnd; dstStart < End(); dstStart += ((SatoriObject*)dstStart)->Size())
        {
            if (!IsEscapedOrPinned((SatoriObject*)dstStart))
            {
                break;
            }
        }

        if (dstStart >= End())
        {
            // no more space could be found
            goto Exit;
        }

        // dst end: skip until next unmovable
        for (dstEnd = dstStart; dstEnd < End(); dstEnd = SkipUnmarked(((SatoriObject*)dstEnd)->Next())->Start())
        {
            if (IsEscapedOrPinned((SatoriObject*)dstEnd))
            {
                break;
            }
        }

        // plan relocs for movables as long as there is space
        while (dstEnd - dstStart >= moveableSize + Satori::MIN_FREE_SIZE ||
            dstEnd - dstStart == moveableSize)
        {
            _ASSERTE(moveable->Start() >= dstStart);
            int32_t reloc = (int32_t)(moveable->Start() - dstStart);
            if (reloc != 0)
            {
                moveable->SetLocalReloc(reloc);
            }

            dstStart += moveableSize;

            // moveable: skip unmarked or pinned
            while (true)
            {
                lastMarkedEnd = moveable->Start() + moveableSize;
                moveable = SkipUnmarked((SatoriObject*)lastMarkedEnd);

                size_t skipped = moveable->Start() - lastMarkedEnd;
                if (skipped)
                {
                    SatoriObject::FormatAsFree(lastMarkedEnd, skipped);
                }

                if (moveable->Start() >= End())
                {
                    // nothing left to move
                    _ASSERTE(moveable->Start() == End());
                    goto Exit;
                }

                moveableSize = moveable->Size();
                objCount++;
                occupancy += moveableSize;

                if (!IsEscapedOrPinned(moveable))
                {
                    // reachable and moveable. we can move this.
                    break;
                }
            }
        }

        // here we have something to move, but it does not fit in the current gap.
        // we need to find another gap
        // Note: gap will not cross ahead of the current moveable, because moveables are valid gaps.
        //       the gap may reach the current movable, which would result in a trivial 0-distance move
    }

Exit:
    SetOccupancy(occupancy, objCount);
}

template <bool isConservative>
void SatoriRegion::UpdateFn(PTR_PTR_Object ppObject, ScanContext* sc, uint32_t flags)
{
    SatoriRegion* region = (SatoriRegion*)sc->_unused1;
    size_t location = (size_t)*ppObject;

    // ignore objects otside of the current region.
    // this also rejects nulls and byrefs pointing to stack.
    if (location < (size_t)region->FirstObject() || location >= region->End())
    {
        return;
    }

    SatoriObject* o = (SatoriObject*)location;
    if (flags & GC_CALL_INTERIOR)
    {
        o = region->FindObject(location);
        if (isConservative && o->IsFree())
        {
            return;
        }
    }

    size_t reloc = o->GetLocalReloc();
    if (reloc)
    {
        *ppObject = (Object*)(((size_t)*ppObject) - reloc);
        _ASSERTE(((SatoriObject*)(*ppObject))->SameRegion(region));
    }
};

void SatoriRegion::ThreadLocalUpdatePointers()
{
    // update stack roots
    ScanContext sc;
    sc.promotion = FALSE;
    sc._unused1 = this;

    if (SatoriUtil::IsConservativeMode())
        GCToEEInterface::GcScanCurrentStackRoots((promote_func*)UpdateFn<true>, &sc);
    else
        GCToEEInterface::GcScanCurrentStackRoots((promote_func*)UpdateFn<false>, &sc);

    // go through all live objects and update pointers if targets are planned for relocation.
    size_t bitmapIndex = BITMAP_START;
    int markBitOffset = 0;
    while (bitmapIndex < BITMAP_LENGTH)
    {
        DWORD step;
        if (BitScanForward64(&step, m_bitmap[bitmapIndex] >> markBitOffset))
        {
            // got reachable object
            markBitOffset += step;

            int escapedOffset = markBitOffset + 1;
            bool escaped = m_bitmap[bitmapIndex + (escapedOffset >> 6)] & ((size_t)1 << (escapedOffset & 63));
            SatoriObject* o = ObjectForMarkBit(bitmapIndex, markBitOffset);
            if (escaped)
            {
                _ASSERTE(ObjectForMarkBit(bitmapIndex, markBitOffset)->GetLocalReloc() == 0);
            }
            else
            {
                o->ForEachObjectRef(
                    [this](SatoriObject** ppObject)
                    {
                        SatoriObject* child = *ppObject;
                        // ignore objects otside of the current region, we are not relocating those.
                        // (this also rejects nulls)
                        if (child->SameRegion(this))
                        {
                            size_t reloc = child->GetLocalReloc();
                            if (reloc)
                            {
                                *ppObject = (SatoriObject*)((size_t)*ppObject - reloc);
                                _ASSERTE((*ppObject)->SameRegion(this));
                            }
                        }
                    }
                );
            }

            // skip the object
            markBitOffset = o->Next()->GetMarkBitAndWord(&bitmapIndex);
        }
        else
        {
            // skip empty mark words
            markBitOffset = 0;
            while (++bitmapIndex < BITMAP_LENGTH && m_bitmap[bitmapIndex] == 0) {}
        }
    }

    // update finalizables if we have them
    if (m_finalizableTrackers)
    {
        ForEachFinalizableThreadLocal(
            [&](SatoriObject* finalizable)
            {
                // save the pending bit, will reapply back later
                size_t finalizePending = (size_t)finalizable & Satori::FINALIZATION_PENDING_ANY;
                (size_t&)finalizable &= ~Satori::FINALIZATION_PENDING_ANY;

                size_t reloc = finalizable->GetLocalReloc();
                if (reloc)
                {
                    finalizable = (SatoriObject*)((size_t)finalizable - reloc);
                    _ASSERTE(finalizable->SameRegion(this));
                }

                (size_t&)finalizable |= finalizePending;
                return finalizable;
            }
        );
    }
}

void SatoriRegion::ThreadLocalCompact()
{
    SatoriObject* s1;
    SatoriObject* s2 = FirstObject();
    SatoriObject* d1 = FirstObject();
    SatoriObject* d2 = FirstObject();

    size_t foundFree = 0;

    // we will be building new free lists
    ClearFreeLists();
    // compacting may invalidate indices, since objects move around.
    ClearIndex();

    // when d1 reaches the end everything is copied or swept
    while (d1->Start() < End())
    {
        // find next relocatable object
        int32_t reloc = 0;
        // "Next" is ok here because we coalesce unmarked objs when planning.
        for (s1 = s2; s1->Start() < End(); s1 = s1->Next())
        {
            reloc = s1->GetLocalReloc();
            _ASSERTE(reloc >= 0);
            if (reloc != 0)
            {
                break;
            }
        }

        // sweep all objects up to the relocation target
        size_t relocTarget = s1->Start() - reloc;
        while (d1->Start() < relocTarget)
        {
            _ASSERTE(d1->Start() <= d2->Start());
            d2 = SkipUnmarked(d2, relocTarget);

            if (d1 != d2)
            {
                size_t freeSpace = d2->Start() - d1->Start();
                SatoriObject* freeObj = SatoriObject::FormatAsFree(d1->Start(), freeSpace);
                SetIndicesForObject(freeObj, d2->Start());
                AddFreeSpace(freeObj, freeSpace);
                foundFree += freeSpace;

                d1 = d2;
                if (d1->Start() == relocTarget)
                {
                    break;
                }
            }

            // clear Mark/Pinned, keep escaped, reloc should be 0, this object will stay around
            _ASSERTE(d1->GetLocalReloc() == 0);
            ClearPinnedAndMarked(d1);
            SatoriObject* next = d1->Next();
            // opportunistically mark the index if d1 is indexable
            SetIndicesForObject(d1, next->Start());
            d2 = d1 = next;
        }

        // find the end of the run of relocatable objects with the same reloc distance (we can copy all at once)
        s2 = s1;
        while (s2->Start() < End())
        {
            if (reloc != s2->GetLocalReloc())
            {
                break;
            }

            // clear mark and reloc. (escape should not be set, since we are relocating)
            // pre-relocation copy of object need to clear the mark, so that the object could be swept if not overwritten.
            // the relocated copy is not swept so clearing is also correct.
            s2->ClearMarkCompactStateForRelocation();
            SatoriObject* next = s2->Next();
            // opportunistically mark the index if s2 is indexable
            SetIndicesForObject((SatoriObject*)(s2->Start() - reloc), next->Start() - reloc);
            s2 = next;
        }

        // move d2 to the first object after the future end of the relocated run
        // [d1, d2) will be the gap that the run is targeting, but it may not fill the whole gap.
        // for our purposes the extra space is as good as a free object, we will move d1 to that
        size_t relocTargetEnd = s2->Start() - reloc;
        d1 = (SatoriObject*)relocTargetEnd;
        while (d2->Start() < relocTargetEnd)
        {
            d2 = d2->Next();
        }

        if (reloc)
        {
            // moving objects with syncblocks, thus the "- sizeof(size_t)" adjustment
            memmove(
                (void*)(relocTarget - sizeof(size_t)),
                (void*)(s1->Start() - sizeof(size_t)),
                s2->Start() - s1->Start());
        }
    }

    Verify();

    _ASSERTE((Satori::REGION_SIZE_GRANULARITY - offsetof(SatoriRegion, m_firstObject) - foundFree) == m_occupancy);
}

NOINLINE void SatoriRegion::ClearPinned(SatoriObject* o)
{
    if (!IsEscaped(o))
    {
        ClearMarked(o + MarkOffset::Pinned);
    }
}

NOINLINE void SatoriRegion::SetIndicesForObjectCore(size_t start, size_t end)
{
    size_t i = LocationToIndex(start) + 1;
    size_t lastIndex = LocationToIndex(end);
    int objOffset = (int)(start - Start());
    do
    {
        m_index[i++] = objOffset;
    } while (i <= lastIndex);
}

enum class FinalizationPendState
{
    PendRegular,
    HasCritical,
    PendCritical,
};

void SatoriRegion::ThreadLocalPendFinalizables()
{
    HasPendingFinalizables() = false;

    FinalizationPendState pendState = FinalizationPendState::PendRegular;
    bool missedRegularPend = false;
    SatoriFinalizationQueue* q = m_containingPage->Heap()->FinalizationQueue();

tryAgain:
    ForEachFinalizableThreadLocal(
        [&](SatoriObject* finalizable)
        {
            if ((size_t)finalizable & Satori::FINALIZATION_PENDING_ANY)
            {
                SatoriObject* o = (SatoriObject*)((size_t)finalizable & ~Satori::FINALIZATION_PENDING_ANY);
                if (pendState == FinalizationPendState::PendRegular && o->RawGetMethodTable()->HasCriticalFinalizer())
                {
                    // within the same finalization set CF must finalize after ordinary F objects
                    pendState = FinalizationPendState::HasCritical;
                    return finalizable;
                }

                // by enqueing we are making the object available to the finalization thread
                // we do not do this earlier when marking since we want give a chance
                // for the obj tree to be compacted.
                EscapeRecursively(o);

                // In a rare case when an F object could not pend, we cannot pend any CFs from the same finalization set
                // lest they may run earlier than F objects from the same set.
                // In such case we will just allow CFs to stay alive/escaped and finalization-tracked
                // until global GC sorts this out.
                if (missedRegularPend && o->RawGetMethodTable()->HasCriticalFinalizer())
                {
                    finalizable = o;
                }
                else if (q->TryScheduleForFinalization(o))
                {
                    // this tracker has served its purpose.
                    finalizable = nullptr;
                }
                else
                {
                    if (pendState != FinalizationPendState::PendCritical)
                    {
                        missedRegularPend = true;
                    }

                    finalizable = o;
                }
            }

            return finalizable;
        }
    );

    if (pendState == FinalizationPendState::HasCritical)
    {
        pendState = FinalizationPendState::PendCritical;
        goto tryAgain;
    }

    GCToEEInterface::EnableFinalization(true);
}

bool SatoriRegion::RegisterForFinalization(SatoriObject* finalizable)
{
    _ASSERTE(finalizable->SameRegion(this));
    _ASSERTE(this->m_hasFinalizables || this->IsAttachedToAllocatingOwner());

    LockFinalizableTrackers();

    if (!m_finalizableTrackers || !m_finalizableTrackers->TryPush(finalizable))
    {
        m_hasFinalizables = true;
        SatoriWorkChunk* WorkChunk = Allocator()->GetWorkChunk();
        if (!WorkChunk)
        {
            // OOM 
            UnlockFinalizableTrackers();
            return false;
        }

        WorkChunk->SetNext(m_finalizableTrackers);
        m_finalizableTrackers = WorkChunk;
        WorkChunk->Push(finalizable);
    }

    UnlockFinalizableTrackers();
    return true;
}

// Finalizable trackers are generally accessed exclusively, when EE is stopped.
// The only case where we can have contention is when a user thread re-registers
// concurrently with another thread doing the same or
// concurrently with a thread local collection.
// It is extremely unlikely to have such contention, so a simplest spinlock is ok
void SatoriRegion::LockFinalizableTrackers()
{
    while (Interlocked::CompareExchange(&m_finalizableTrackersLock, 1, 0) != 0)
    {
        YieldProcessor();
    };
}

void SatoriRegion::UnlockFinalizableTrackers()
{
    VolatileStore(&m_finalizableTrackersLock, 0);
}

void SatoriRegion::CompactFinalizableTrackers()
{
    SatoriWorkChunk* oldTrackers = m_finalizableTrackers;
    m_finalizableTrackers = nullptr;

    while (oldTrackers)
    {
        SatoriWorkChunk* current = oldTrackers;
        oldTrackers = oldTrackers->Next();

        if (m_finalizableTrackers)
        {
            while (m_finalizableTrackers->HasSpace() && current->Count())
            {
                SatoriObject* o = current->Pop();
                if (o)
                {
                    m_finalizableTrackers->Push(o);
                }
            }
        }

        if (current->Count() > 0 && current->Compact() > 0)
        {
            current->SetNext(m_finalizableTrackers);
            m_finalizableTrackers = current;
            continue;
        }

        current->SetNext(nullptr);
        Allocator()->ReturnWorkChunk(current);
    }
}

SatoriObject* SatoriRegion::SkipUnmarked(SatoriObject* from)
{
    //SatoriObject* tmp = from;
    //while (tmp->Start() < this->End() && !tmp->IsMarked())
    //    tmp = tmp->Next();

    //return tmp;

    _ASSERTE(from->Start() < End());
    size_t bitmapIndex;
    int markBitOffset = from->GetMarkBitAndWord(&bitmapIndex);

    DWORD offset;
    if (BitScanForward64(&offset, m_bitmap[bitmapIndex] >> markBitOffset))
    {
        // got mark bit
        markBitOffset += offset;
    }
    else
    {
        markBitOffset = 0;
        do
        {
            bitmapIndex++;
            if (BitScanForward64(&offset, m_bitmap[bitmapIndex]))
            {
                // got mark bit
                markBitOffset = offset;
                break;
            }
        } while (bitmapIndex < SatoriRegion::BITMAP_LENGTH);
    }

    return ObjectForMarkBit(bitmapIndex, markBitOffset);
}

// clears the mark bit
SatoriObject* SatoriRegion::SkipUnmarkedAndClear(SatoriObject* from)
{
    _ASSERTE(from->Start() < End());
    size_t bitmapIndex;
    int markBitOffset = from->GetMarkBitAndWord(&bitmapIndex);

    DWORD offset;
    if (BitScanForward64(&offset, m_bitmap[bitmapIndex] >> markBitOffset))
    {
        // got mark bit
        markBitOffset += offset;
    }
    else
    {
        markBitOffset = 0;
        do
        {
            bitmapIndex++;
            if (BitScanForward64(&offset, m_bitmap[bitmapIndex]))
            {
                // got mark bit
                markBitOffset = offset;
                break;
            }
        } while (bitmapIndex < SatoriRegion::BITMAP_LENGTH);
    }

    m_bitmap[bitmapIndex] &= ~((size_t)1 << markBitOffset);
    return ObjectForMarkBit(bitmapIndex, markBitOffset);
}

SatoriObject* SatoriRegion::SkipUnmarked(SatoriObject* from, size_t upTo)
{
    _ASSERTE(from->Start() < End());
    size_t bitmapIndex;
    int markBitOffset = from->GetMarkBitAndWord(&bitmapIndex);

    DWORD offset;
    if (BitScanForward64(&offset, m_bitmap[bitmapIndex] >> markBitOffset))
    {
        // got mark bit
        markBitOffset += offset;
    }
    else
    {
        size_t limit = (upTo - Start()) / Satori::BYTES_PER_CARD_BYTE;
        _ASSERTE(limit <= SatoriRegion::BITMAP_LENGTH);
        markBitOffset = 0;
        do
        {
            bitmapIndex++;
            if (BitScanForward64(&offset, m_bitmap[bitmapIndex]))
            {
                // got mark bit
                markBitOffset = offset;
                break;
            }
        } while (bitmapIndex < limit);
    }

    SatoriObject* result = ObjectForMarkBit(bitmapIndex, markBitOffset);
    if (result->Start() > upTo)
    {
        result = (SatoriObject*)upTo;
    }

    _ASSERTE(result->Start() <= upTo);
    return result;
}


void SatoriRegion::UpdateFinalizableTrackers()
{
    // if any finalizable trackers point outside,
    // their objects have been relocated to this region, we need to update these trackers
    if (m_finalizableTrackers)
    {
        SatoriWorkChunk* chunk = m_finalizableTrackers;
        while (chunk)
        {
            size_t count = chunk->Count();
            for (size_t i = 0; i < count; i++)
            {
                SatoriObject* finalizable = chunk->Item(i);
                if (finalizable != nullptr)
                {
                    if (!finalizable->SameRegion(this))
                    {
                        finalizable = finalizable->RelocatedToUnchecked();
                        _ASSERTE(finalizable->SameRegion(this));
                        chunk->Item(i) = finalizable;
                    }
                }
            }

            chunk = chunk->Next();
        }
    }
}

void SatoriRegion::UpdatePointersInObject(SatoriObject* o, size_t size)
{
    // if the containing region is large, do not engage with the entire object,
    // schedule update of separate ranges.
    if (this->Size() == Satori::REGION_SIZE_GRANULARITY ||
        !Recycler()->ScheduleUpdateAsChildRanges(o))
    {
        o->ForEachObjectRef(
            [](SatoriObject** ppObject)
            {
                SatoriObject* child = *ppObject;
                // ignore common cases - child is null or in the same region
                if (child &&
                    (((size_t)ppObject ^ (size_t)child) >= Satori::REGION_SIZE_GRANULARITY))
                {
                    SatoriObject* newLocation;
                    if (child->IsRelocatedTo(&newLocation))
                    {
                        *ppObject = newLocation;
                    }
                }
            },
            size
        );
    }
}

void SatoriRegion::UpdatePointers()
{
    _ASSERTE(!HasMarksSet());

    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;
    SatoriObject* o = FirstObject();
    do
    {
        size_t size = o->Size();
        UpdatePointersInObject(o, size);
        o = (SatoriObject*)(o->Start() + size);
    } while (o->Start() < objLimit);
}

void SatoriRegion::TakeFinalizerInfoFrom(SatoriRegion* other)
{
    _ASSERTE(!other->HasPinnedObjects());
    m_hasFinalizables |= other->m_hasFinalizables;
    SatoriWorkChunk* otherFinalizables = other->m_finalizableTrackers;
    if (otherFinalizables)
    {
        SatoriWorkChunk* tail = otherFinalizables;
        while (tail->Next() != nullptr)
        {
            tail = tail->Next();
        }

        tail->SetNext(m_finalizableTrackers);
        m_finalizableTrackers = otherFinalizables;
        other->m_finalizableTrackers = nullptr;
    }
}

void SatoriRegion::IndividuallyPromote()
{
    _ASSERTE(m_generation == 1);
    _ASSERTE(!m_doNotSweep);

    if(IsAttachedToAllocatingOwner())
    {
        DetachFromAlocatingOwnerRelease();
    }

    SetGeneration(2);
    m_individuallyPromoted = true;
    RearmCardsForTenured();
}

// ideally, we just reuse the region for allocations.
// TUNING: any situations when we want to require more?
bool SatoriRegion::IsReuseCandidate()
{
    // just ask for enough of usable capaciity to make it worthwile.
    // taking a lock when adding/removing the region to reusables
    return FreeSpaceInTopNBuckets(Satori::FREELIST_COUNT) > Satori::REGION_SIZE_GRANULARITY / 32;
}

bool SatoriRegion::IsDemotionCandidate(bool nextGcIsFullGC)
{
    // If next gc is gen2, then demoted objects do not add to marking costs.
    // Everything will be gen2 anyways, thus we can demote anything that is reusable.
    if (!nextGcIsFullGC)
    {
        // We limit the occupancy, since it adds to gen1 costs.
        // We do not want more than 1/8x of gen2 ending up as a gen1 cost.
        // (we typically allow gen1 to be at least 1/8th of gen2 even if gen1 mortality is high.)
        // We also limit the number of demoted objects, since it consumes trackers.
        if (ObjCount() > Satori::MAX_DEMOTED_OBJECTS_IN_REGION ||
            Occupancy() > Satori::REGION_SIZE_GRANULARITY / 8)
        {
            return false;
        }
    }

    return IsReuseCandidate();
}

// regions that were not reused or relocated for a while could be tenured.
// unless it is a reuse candidate
bool SatoriRegion::IsPromotionCandidate()
{
    // If the region is heavy and has not seen an allocation for 4 cycles, perhaps should tenure it.
    // The goal is to make gen1 cheaper by moving expensive regions to gen2
    // Note that other uses for the region could be:
    //     reuse:   that evidently did not happen, either not reusable or gen1 has too may reusables.
    //     compact: that evidently did not happen, either not enough candidates, or it is pinned.
    return Generation() == 1 &&
        SweepsSinceLastAllocation() > 4 &&
        Occupancy() > Satori::REGION_SIZE_GRANULARITY / 2;
}

// how much unusable space is reclaimed if relocated.
// this is used to estimate goodness of overall and individual relocations.
size_t SatoriRegion::ReclaimSizeIfRelocated(bool assumeFullGC)
{
    if (IsLarge() || HasPinnedObjects())
        return 0;        // can't relocate

    if (IsDemoted() && !assumeFullGC)
        return 0;        // effectively pinned

    // with presweepables we do not know exact occupancy or free lists,
    // so make a guess at 1/2 region
    if (IsPreSweepCandidate(assumeFullGC))
        return Satori::REGION_SIZE_GRANULARITY / 2;

    if (Occupancy() > Satori::REGION_SIZE_GRANULARITY / 2)
        return 0;        // too full. we do not want to move this

    // max reclaim is all unoccupied space.
    size_t reclaim =
        Satori::REGION_SIZE_GRANULARITY - Occupancy() - sizeof(SatoriRegion);

    // nothing is reusable in gen2, so all unused is reclaimable
    // demotables are technically reusable, but we would rather relocate, than demote
    if (Generation() == 2 || assumeFullGC)
        return reclaim;

    // it was not reused in two GCs, just treat as not reusable.
    if (SweepsSinceLastAllocation() <= 2)
    {
        // reduce reclaim by reusable size in large buckets. Large buckets are certainly reusable
        reclaim -= FreeSpaceInTopNBuckets(Satori::LARGE_BUCKETS);
    }

    // if reclaim is too small compared to occupancy, do not relocate.
    if (reclaim * 2 < Occupancy())
        return 0;

    _ASSERTE(reclaim <= Satori::REGION_SIZE_GRANULARITY);
    return reclaim;
}

bool SatoriRegion::IsPreSweepCandidate(bool assumeFullGC)
{
    // Large do not participate in relocations.
    if (IsLarge())
        return false;

    // Attached regions will not participate in relocation, unless full GC
    if (!assumeFullGC && IsAttachedToAllocatingOwner())
        return false;

    if (this->IsPreSwept())
        return false;

    // If saw sweeps, then the free lists have seen the first attrition for given generation.
    // Otherwise the free data may be quite off.
    // NOTE: demoted will not relocate, but can accept relocations, so still can presweep.
    // NOTE: gen1 regions will be considered unswept for gen2 GC purposes.
    bool result = SweepsSinceLastAllocation() == 0 ||
        (assumeFullGC && Generation() != 2);

    _ASSERTE(!result || assumeFullGC || !IsPromotionCandidate());
    return result;
}

bool SatoriRegion::TryDemote(bool nextGcIsFullGc)
{
    _ASSERTE(!HasMarksSet());
    _ASSERTE(Generation() == 2);
    _ASSERTE(ObjCount() != 0);

    if (!IsDemotionCandidate(nextGcIsFullGc))
    {
        return false;
    }

    // if next GC is full, this will end up in gen2,
    // so no need to track gen2 objects.
    if (nextGcIsFullGc)
    {
        m_demotedOccupancy = Occupancy();
        // The following is not necessary as when next GC is a full GC, we do not do cards.
        // this->ResetCardsForEphemeral();
        this->SetGeneration(1);
        return true;
    }

    // TUNING: heuristic for demoting -  could consider occupancy, pinning, etc...
    //         the cost here is increasing  gen1, which is supposed to be short as 
    //         demoted objects will have to be marked regardless of cards.
    //         we can't have > MAX_DEMOTED_OBJECTS_IN_REGION objects, but the objects
    //         themselves can be big arrays.
    //
    //         the worst case is if this is a largest possible reusable region that is also
    //         pointer-dense. Thus we will limit the size, just in case.

    SatoriWorkChunk* gen2Objects = Allocator()->TryGetWorkChunk();
    if (!gen2Objects)
    {
        return false;
    }

    // fail if we can't get chunks
    bool failed = false;
    size_t demotedOccupancy = 0;
    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;
    for (SatoriObject* o = FirstObject(); o->Start() < objLimit; o = o->Next())
    {
        if (!o->IsFree())
        {
            if (!gen2Objects->TryPush(o))
            {
                SatoriWorkChunk* chunk = Allocator()->TryGetWorkChunk();
                if (!chunk)
                {
                    failed = true;
                    break;
                }

                chunk->SetNext(gen2Objects);
                gen2Objects = chunk;
                gen2Objects->Push(o);
            }

            demotedOccupancy += o->Size();
        }
    }

    this->m_gen2Objects = gen2Objects;
    if (failed)
    {
        this->FreeDemotedTrackers();
        return false;
    }

    m_demotedOccupancy = demotedOccupancy;
    this->HasUnmarkedDemotedObjects() = true;
    this->ResetCardsForEphemeral();
    this->SetGeneration(1);
    return true;
}

bool SatoriRegion::NothingMarked()
{
    for (size_t bitmapIndex = BITMAP_START; bitmapIndex < BITMAP_LENGTH; bitmapIndex++)
    {
        size_t chunk = m_bitmap[bitmapIndex];
        if (chunk)
        {
            return false;
        }
    }

    return true;
}

void SatoriRegion::ClearMarks()
{
    _ASSERTE(this->HasUnmarkedDemotedObjects() == false);
    memset((void*)&m_bitmap[BITMAP_START], 0, (BITMAP_LENGTH - BITMAP_START) * sizeof(size_t));
}

void SatoriRegion::ClearIndex()
{
    memset((void*)&m_index, 0, sizeof(m_index));
}

void SatoriRegion::ClearFreeLists()
{
    // clear free lists and free list tails
    memset(m_freeListCapacities, 0, sizeof(m_freeListCapacities) + sizeof(m_freeLists) + sizeof(m_freeListTails));
}

void SatoriRegion::PreSweep()
{
    // we should only sweep when we have marks
    _ASSERTE(HasMarksSet());
    _ASSERTE(!DoNotSweep());
    _ASSERTE(!IsLarge());
    _ASSERTE(!this->m_individuallyPromoted);

    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;

    // we will be building new free lists
    ClearFreeLists();

    size_t occupancy = 0;
    int32_t objCount = 0;
    bool hasFinalizables = false;
    SatoriObject* o = FirstObject();
    do
    {
        if (!IsMarked(o))
        {
            size_t lastMarkedEnd = o->Start();
            o = SkipUnmarked(o);
            SatoriUtil::Prefetch(o);
            size_t skipped = o->Start() - lastMarkedEnd;
            SatoriObject* free = SatoriObject::FormatAsFree(lastMarkedEnd, skipped);
            SetIndicesForObject(free, o->Start());
            AddFreeSpace(free, skipped);

            if (o->Start() >= objLimit)
            {
                _ASSERTE(o->Start() == objLimit);
                break;
            }
        }

        _ASSERTE(!o->IsFree());

        size_t size = o->Size();

        if (!hasFinalizables && o->RawGetMethodTable()->HasFinalizer())
        {
            hasFinalizables = true;
        }

        objCount++; 
        occupancy += size;
        o = (SatoriObject*)(o->Start() + size);
    } while (o->Start() < objLimit);

    _ASSERTE(o->Start() == objLimit || End() > objLimit);

    this->m_hasFinalizables = hasFinalizables;
    SetOccupancy(occupancy, objCount);
    this->IsPreSwept() = true;
}

void SatoriRegion::FinishSweepForPreSwept()
{
    // Do final things that Sweep would do for pre-swept regions.
    // That is mostly clearing marks and a few other things.

    // TUNING: could cleaning marks in presweep be a win?
    // 
    // We keep marks in presweep so that we could SkipUnmarked in
    // couple places related to relocation - like RelocateRegion and
    // UpdatePointersInPromotedObjects.
    
    // We could clear marks in presweep and introduce SkipUnmarkedOrFree to use in RelocateRegion.
    // Perhaps need to make HasMarksSet non-debug for that or fold with DoNotSweep.
    // Also would need to make RelocateRegion to sweep src.
    // Reloc sweeping may need to fix index, since scan may look through old objs (for both stack and cards)
    // (presweep already does)
    // 
    // In such model either sweep or reloc destroys marks.
    // It could be interesting if presweep is relatively common
    // (at least in relocating scenarios, as no-compact case does not care).
    ClearMarks();

#if _DEBUG
    HasMarksSet() = false;
#endif
    HasPinnedObjects() = false;
    HasUnmarkedDemotedObjects() = IsDemoted();
    IsPreSwept() = false;
    DoNotSweep() = true;

    m_sweepsSinceLastAllocation++;
}

void SatoriRegion::Verify(bool allowMarked)
{
#ifdef _DEBUG
    for (size_t i = m_used; i < m_committed; i++)
    {
        _ASSERTE(*(uint8_t*)i == 0);
    }

    SatoriObject* prevPrevObj = nullptr;
    SatoriObject* prevObj = nullptr;
    SatoriObject* o = FirstObject();

    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;
    while (o->Start() < objLimit)
    {
        o->Validate();

        if (o->Start() - Start() > Satori::REGION_SIZE_GRANULARITY)
        {
            _ASSERTE(o->IsFree());
        }
        else
        {
            _ASSERTE(allowMarked || !IsMarked(o));
        }

        prevPrevObj = prevObj;
        prevObj = o;
        o = o->Next();
    }

    _ASSERTE(o->Start() == End() ||
        (Size() > Satori::REGION_SIZE_GRANULARITY) && (End() - o->Start()) < Satori::REGION_SIZE_GRANULARITY);
#endif
}
