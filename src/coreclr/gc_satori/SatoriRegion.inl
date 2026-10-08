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
// SatoriRegion.inl
//

#ifndef __SATORI_REGION_INL__
#define __SATORI_REGION_INL__

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "../env/gcenv.ee.h"
#include "SatoriRegion.h"
#include "SatoriPage.h"
#include "SatoriWorkChunk.h"

inline bool SatoriRegion::CanSplitWithoutCommit(size_t size)
{
    return m_committed > (m_end - size + offsetof(SatoriRegion, m_syncBlock) + SatoriUtil::MinZeroInitSize());
}

inline bool SatoriRegion::IsEscapeTracking()
{
    _ASSERTE(!m_ownerThreadTag || m_generation == 0);
    return m_ownerThreadTag;
}

inline bool SatoriRegion::MaybeEscapeTrackingAcquire()
{
    // must check reusable level before the owner tag, before doing whatever follows
    return VolatileLoad((uint8_t*)&m_reusableFor) == (uint8_t)ReuseLevel::Gen0 ||
        VolatileLoad(&m_ownerThreadTag);
}

inline bool SatoriRegion::IsEscapeTrackedByCurrentThread()
{
    return m_ownerThreadTag == SatoriUtil::GetCurrentThreadTag();
}

inline int SatoriRegion::Generation()
{
    return m_generation;
}

inline int SatoriRegion::GenerationAcquire()
{
    return VolatileLoad(&m_generation);
}

inline void SatoriRegion::SetGeneration(int generation)
{
    // Gen0 is controlled by thread-local tracking
    _ASSERTE(generation != 0);
    _ASSERTE(m_generation != 0);

    if (m_generation != generation)
    {
        // generation has changed, reset sweeps counter
        m_sweepsSinceLastAllocation = 0;
    }

    m_generation = generation;
}

inline void SatoriRegion::SetGenerationRelease(int generation)
{
    // at the time when we set generation to 0+, we should make it certain if
    // the region is attached, since 0+ detached regions are assumed parseable.
    _ASSERTE(generation != 0);
    _ASSERTE(m_generation == -1 || IsReusable());

    // No need to reset m_sweepsSinceLastAllocation like in SetGeneration.
    // SetGenerationRelease is always related to an alocation anyways.
    // (promoting/demoting does not need "Release")

    VolatileStore(&m_generation, generation);
}

inline void SatoriRegion::SetHasFinalizables()
{
    m_hasFinalizables = true;
}

inline bool SatoriRegion::IsAllocating()
{
    return m_allocEnd != 0;
}

inline size_t SatoriRegion::Start()
{
    return (size_t)this;
}

inline size_t SatoriRegion::End()
{
    return m_end;
}

inline size_t SatoriRegion::Size()
{
    return End() - Start();
}

inline bool SatoriRegion::IsLarge()
{
    return Size() > Satori::REGION_SIZE_GRANULARITY;
}

inline size_t SatoriRegion::GetAllocStart()
{
    return m_allocStart;
}

inline size_t SatoriRegion::GetAllocRemaining()
{
    // reserve Satori::MIN_FREE_SIZE to be able to make the unused space parseable
    ptrdiff_t diff = m_allocEnd - m_allocStart - Satori::MIN_FREE_SIZE;
    _ASSERTE(!IsAllocating() || diff >=0);
    return diff >= 0 ? (size_t)diff : 0;
}

inline size_t SatoriRegion::RegionSizeForAlloc(size_t allocSize)
{
    return ALIGN_UP(allocSize + offsetof(SatoriRegion, m_firstObject) + Satori::MIN_FREE_SIZE, Satori::REGION_SIZE_GRANULARITY);
}

inline SatoriObject* SatoriRegion::FirstObject()
{
    return &m_firstObject;
}

inline void SatoriRegion::StartEscapeTrackingRelease(size_t threadTag)
{
    _ASSERTE(m_generation == -1 || m_reusableFor == ReuseLevel::Gen0);
    m_escapeFunc = EscapeFn;
    m_ownerThreadTag = threadTag;
    VolatileStore(&m_generation, 0);
}

inline void SatoriRegion::StopEscapeTracking()
{
    if (IsEscapeTracking())
    {
        _ASSERTE(!HasPinnedObjects());
        ClearMarks();

        // must clear ownership after clearing marks
        // to make sure concurrent marking does not use dirty mark table
        VolatileStore(&m_ownerThreadTag, (size_t)0);

        m_escapeFunc = nullptr;
        m_generation = 1;
        m_escapedSize = 0;
        m_allocBytesAtCollect = 0;
    }
}

// Used to simulate writes when containing region is individually promoted.
inline void SatoriRegion::SetCardsForObject(SatoriObject* o, size_t size)
{
    _ASSERTE(this->Size() == Satori::REGION_SIZE_GRANULARITY);

    o->ForEachObjectRef(
        [&](SatoriObject** ppObject)
        {
            SatoriObject* child = VolatileLoadWithoutBarrier(ppObject);
            if (child &&
                !child->IsExternal() &&
                child->ContainingRegion()->Generation() < 2)
           {
                // This could run concurrenlty with mutator or in a blocking stage,
                // but not when the blocking stage does cleaning.
                // It also should be relatively rare, so we will just assume the mutator is running
                // for simplicity and call a concurrent helper.
                ContainingPage()->DirtyCardForAddressConcurrent((size_t)ppObject);
            }
        },
        size
    );
}

template <typename F>
void SatoriRegion::ForEachFinalizable(F lambda)
{
    size_t items = 0;
    size_t nulls = 0;
    SatoriWorkChunk* chunk = m_finalizableTrackers;
    while (chunk)
    {
        size_t count = chunk->Count();
        items += count;
        for (size_t i = 0; i < count; i++)
        {
            SatoriObject* finalizable = chunk->Item(i);
            if (finalizable == nullptr)
            {
                nulls++;
                continue;
            }

            SatoriObject* newFinalizable = lambda(finalizable);
            if (newFinalizable != finalizable)
            {
                chunk->Item(i) = newFinalizable;
                if (newFinalizable == nullptr)
                {
                    nulls++;
                }
            }
        }

        // unfilled slots in the tail chunks count as nulls
        if (chunk != m_finalizableTrackers)
        {
            nulls += chunk->FreeSpace();
        }

        chunk = chunk->Next();
    }

    // typically the list is short, so having some dead entries is not a big deal.
    // compacting at 1/2 occupancy should be good enough.
    // (50% max overhead at O(N) maintenance cost)
    if (nulls * 2 > items)
    {
       CompactFinalizableTrackers();
    }
}

template <typename F>
bool SatoriRegion::PendFinalizables(F markFn, int condemnedGeneration)
{
    bool hasCF = false;
    size_t items = 0;
    size_t nulls = 0;

    SatoriFinalizationQueue* q = m_containingPage->Heap()->FinalizationQueue();
    SatoriWorkChunk* chunk = m_finalizableTrackers;
    while (chunk)
    {
        int32_t toPend = 0;
        int32_t toPendCF = 0;
        size_t count = chunk->Count();
        items += count;
        for (size_t i = 0; i < count; i++)
        {
            SatoriObject* finalizable = chunk->Item(i);
            _ASSERTE(((size_t)finalizable & Satori::FINALIZATION_PENDING_ANY) == 0);

            if (finalizable == nullptr)
            {
                nulls++;
                continue;
            }

            // reachable finalizables are not iteresting in any state.
            // finalizer can be suppressed and re-registered again without creating new trackers.
            // (this is preexisting behavior)
            if (finalizable->IsMarked())
            {
                continue;
            }

            // eager finalization does not respect suppression (preexisting behavior)
            if (GCToEEInterface::EagerFinalized(finalizable))
            {
                finalizable = nullptr;
                nulls++;
            }
            else if (finalizable->IsFinalizationSuppressed())
            {
                // Reset the bit so it will be put back on the queue
                // if resurrected and re-registered.
                // NOTE: if finalizer could run only once until re-registered,
                //       unreachable + suppressed object would not be able to resurrect.
                //       however, re-registering multiple times may result in multiple finalizer runs.
                // (this is preexisting behavior)
                finalizable->UnSuppressFinalization();
                finalizable = nullptr;
                nulls++;
            }
            else
            {
                // finalizable has become unreachable.
                // mark it as it needs to stay live until finalized and/or suppressed.
                markFn(finalizable);

                if (finalizable->RawGetMethodTable()->HasCriticalFinalizer())
                {
                    // can't pend just yet, because CriticalFinalizables must go
                    // after regular finalizables scheduled in the same GC
                    toPendCF++;
                    (size_t&)finalizable |= Satori::FINALIZATION_PENDING_CRITICAL;
                }
                else
                {
                    toPend++;
                    (size_t&)finalizable |= Satori::FINALIZATION_PENDING;
                }
            }

            chunk->Item(i) = finalizable;
        }

        if (toPend)
        {
            size_t pendIdx;
            bool overflow = !q->TryReserveSpace(toPend, &pendIdx);
            for (size_t i = 0; i < count; i++)
            {
                SatoriObject* finalizable = chunk->Item(i);
                if ((size_t)finalizable & Satori::FINALIZATION_PENDING)
                {
                    _ASSERTE(toPend > 0);
#if _DEBUG
                    toPend--;
#endif

                    (size_t&)finalizable &= ~Satori::FINALIZATION_PENDING;
                    if (!overflow)
                    {
                        q->ScheduleForFinalizationAt(pendIdx++, finalizable);
                        finalizable = nullptr;
                        nulls++;
                    }

                    chunk->Item(i) = finalizable;
                }
            }

            if (overflow)
            {
                q->SetOverflow(condemnedGeneration);
            }
        }

        _ASSERTE(toPend == 0);

        // unfilled slots in the tail chunks count as nulls
        if (chunk != m_finalizableTrackers)
        {
            nulls += chunk->FreeSpace();
        }

        if (toPendCF)
        {
            chunk->SetAux(toPendCF);
            hasCF = true;
        }

        chunk = chunk->Next();
    }

    // typically the list is short, so having some dead entries is not a big deal.
    // compacting at 1/2 occupancy should be good enough.
    // (50% max overhead at O(N) maintenance cost)
    if (!hasCF && nulls * 2 > items)
    {
       CompactFinalizableTrackers();
    }

    return hasCF;
}

inline void SatoriRegion::PendCfFinalizables(int condemnedGeneration)
{
    size_t items = 0;
    size_t nulls = 0;

    SatoriFinalizationQueue* q = m_containingPage->Heap()->FinalizationQueue();
    SatoriWorkChunk* chunk = m_finalizableTrackers;
    while (chunk)
    {
        int32_t toPend = chunk->GetAndClearAux();
        size_t count = chunk->Count();
        items += count;
        size_t pendIdx = 0;
        bool overflow = toPend &&
            (q->OverflowedGen() != 0 || !q->TryReserveSpace(toPend, &pendIdx));

        for (size_t i = 0; i < count; i++)
        {
            SatoriObject* finalizable = chunk->Item(i);
            if (finalizable == nullptr)
            {
                nulls++;
                continue;
            }

            if ((size_t)finalizable & Satori::FINALIZATION_PENDING_CRITICAL)
            {
                _ASSERTE(toPend > 0);
#if _DEBUG
                toPend--;
#endif

                (size_t&)finalizable &= ~Satori::FINALIZATION_PENDING_CRITICAL;
                if (!overflow)
                {
                    q->ScheduleForFinalizationAt(pendIdx++, finalizable);
                    finalizable = nullptr;
                    nulls++;
                }

                chunk->Item(i) = finalizable;
            }
        }

        _ASSERTE(toPend == 0);

        if (overflow)
        {
            q->SetOverflow(condemnedGeneration);
        }

        // unfilled slots in the tail chunks count as nulls
        if (chunk != m_finalizableTrackers)
        {
            nulls += chunk->FreeSpace();
        }

        chunk = chunk->Next();
    }

    // typically the list is short, so having some dead entries is not a big deal.
    // compacting at 1/2 occupancy should be good enough.
    // (50% max overhead at O(N) maintenance cost)
    if (nulls * 2 > items)
    {
       CompactFinalizableTrackers();
    }
}

// Used by threadlocal GC concurrently with user threads,
// thus must lock - in case a user thread tries to reregister an object for finalization
template <typename F>
void SatoriRegion::ForEachFinalizableThreadLocal(F lambda)
{
    LockFinalizableTrackers();
    ForEachFinalizable(lambda);
    UnlockFinalizableTrackers();
}

template <bool updatePointers, bool individuallyPromoted, bool isEscapeTracking>
bool SatoriRegion::Sweep()
{
    // we should only sweep when we have marks
    _ASSERTE(HasMarksSet());
    _ASSERTE(!DoNotSweep());

    m_sweepsSinceLastAllocation++;

    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;

    // in a huge region there is only one real object. If it is unreachable the region is all empty.
    if (End() > objLimit)
    {
        SatoriObject* o = FirstObject()->IsFree() ? FirstObject()->Next() : FirstObject();
        if (!IsMarked(o))
        {
            _ASSERTE(this->NothingMarked());
            _ASSERTE(!this->HasPinnedObjects());
#if _DEBUG
            size_t fistObjStart = FirstObject()->Start();
            SatoriObject::FormatAsFree(fistObjStart, objLimit - fistObjStart);
            this->HasMarksSet() = false;
#endif
            this->DoNotSweep() = true;
            SetOccupancy(0, 0);
            return false;
        }
    }
    else
    {
        // we will be building new free lists
        ClearFreeLists();
    }

    m_escapedSize = 0;
    bool cannotRecycle = this->IsAttachedToAllocatingOwner();
    size_t occupancy = 0;
    int32_t objCount = 0;
    bool hasFinalizables = false;
    SatoriObject* o = FirstObject();
    do
    {
        if (!CheckAndClearMarked(o))
        {
            size_t lastMarkedEnd = o->Start();
            o = SkipUnmarkedAndClear(o);
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
        if (isEscapeTracking)
        { 
            this->EscapeShallow(o, size);
        }

        if (updatePointers)
        {
            UpdatePointersInObject(o, size);
        }

        if (individuallyPromoted)
        {
            SetCardsForObject(o, size);
        }

        if (!hasFinalizables && o->RawGetMethodTable()->HasFinalizer())
        {
            hasFinalizables = true;
        }

        objCount++; 
        occupancy += size;
        o = (SatoriObject*)(o->Start() + size);
    } while (o->Start() < objLimit);

    _ASSERTE(o->Start() == objLimit || End() > objLimit);
    _ASSERTE(hasFinalizables || !m_finalizableTrackers);
#if _DEBUG
    this->HasMarksSet() = false;
#endif

    this->HasUnmarkedDemotedObjects() = IsDemoted();
    this->m_hasFinalizables = hasFinalizables;
    this->DoNotSweep() = true;
    this->HasPinnedObjects() = false;
    this->m_individuallyPromoted = false;

    SetOccupancy(occupancy, objCount);
    if (objCount !=0)
    {
        cannotRecycle = true;
    }

    return cannotRecycle;
}

template <bool updatePointers>
bool SatoriRegion::Sweep()
{
    return
        this->m_individuallyPromoted ?
            this->IsEscapeTracking() ?
                Sweep<updatePointers, true, true>() :
                Sweep<updatePointers, true, false>() :
            this->IsEscapeTracking() ?
                Sweep<updatePointers, false, true>() :
                Sweep<updatePointers, false, false>();
}

inline bool SatoriRegion::HasFinalizables()
{
    return m_hasFinalizables;
}

inline bool& SatoriRegion::HasPendingFinalizables()
{
    return m_hasPendingFinalizables;
}

inline size_t SatoriRegion::Occupancy()
{
    return m_occupancy;
}

inline size_t SatoriRegion::DemotedOccupancy()
{
    return m_demotedOccupancy;
}

inline int32_t &SatoriRegion::OccupancyAtReuse()
{
    _ASSERTE(!IsAllocating());
    return m_occupancyAtReuse;
}

inline int32_t SatoriRegion::ObjCount()
{
    return m_objCount;
}

inline bool& SatoriRegion::HasPinnedObjects()
{
    return m_hasPinnedObjects;
}

#if _DEBUG
inline bool& SatoriRegion::HasMarksSet()
{
    return m_hasMarksSet;
}
#endif

inline bool& SatoriRegion::DoNotSweep()
{
    return m_doNotSweep;
}

inline bool& SatoriRegion::IsPreSwept()
{
    return m_isPreSwept;
}

inline bool& SatoriRegion::IsRelocated()
{
    return m_isRelocated;
}

inline bool& SatoriRegion::AcceptedPromotedObjects()
{
    return m_acceptedPromotedObjects;
}

inline bool& SatoriRegion::IndividuallyPromoted()
{
    return m_individuallyPromoted;
}

inline uint32_t SatoriRegion::SweepsSinceLastAllocation()
{
    return m_sweepsSinceLastAllocation;
}

inline bool SatoriRegion::IsReusable()
{
    return m_reusableFor != ReuseLevel::None;
}

inline SatoriRegion::ReuseLevel& SatoriRegion::ReusableFor()
{
    return m_reusableFor;
}

inline SatoriQueue<SatoriRegion>* SatoriRegion::ContainingQueue()
{
    return VolatileLoadWithoutBarrier(&m_containingQueue);
}

inline void SatoriRegion::AttachToAllocatingOwner(SatoriRegion** attachementPoint)
{
    _ASSERTE(!m_allocatingOwnerAttachmentPoint);
    _ASSERTE(!*attachementPoint);

    *attachementPoint = this;
    m_allocatingOwnerAttachmentPoint = attachementPoint;
}

inline void SatoriRegion::DetachFromAlocatingOwnerRelease()
{
    _ASSERTE(*m_allocatingOwnerAttachmentPoint == this);

    if (IsEscapeTracking())
    {
        StopEscapeTracking();
    }

    *m_allocatingOwnerAttachmentPoint = nullptr;
    // all allocations must be committed prior to detachement.
    VolatileStore(&m_allocatingOwnerAttachmentPoint, (SatoriRegion**)nullptr);
}

inline bool SatoriRegion::IsAttachedToAllocatingOwner()
{
    return m_allocatingOwnerAttachmentPoint;
}

inline bool SatoriRegion::MaybeAllocatingAcquire()
{
    // must check reusable level before the attach point, before doing whatever follows
    return VolatileLoad((uint8_t*)&m_reusableFor) ||
        VolatileLoad(&m_allocatingOwnerAttachmentPoint) || m_unfinishedAllocationCount;
}

inline void SatoriRegion::ResetReusableForRelease()
{
    VolatileStore(&m_reusableFor, SatoriRegion::ReuseLevel::None);
}

inline bool SatoriRegion::IsDemoted()
{
    return m_gen2Objects;
}

inline SatoriWorkChunk* &SatoriRegion::DemotedObjects()
{
    return m_gen2Objects;
}

inline bool& SatoriRegion::HasUnmarkedDemotedObjects()
{
    return m_hasUnmarkedDemotedObjects;
}

inline void SatoriRegion::SetMarked(SatoriObject* o)
{
    _ASSERTE(o->SameRegion(this));

    size_t word = o->Start();
    size_t bitmapIndex = (word >> 9) & (SatoriRegion::BITMAP_LENGTH - 1);
    size_t mask = (size_t)1 << ((word >> 3) & 63);

    m_bitmap[bitmapIndex] |= mask;
}

inline void SatoriRegion::SetMarkedAtomic(SatoriObject* o)
{
    _ASSERTE(o->SameRegion(this));
    _ASSERTE(!o->IsFree());

    size_t word = o->Start();
    size_t bitmapIndex = (word >> 9) & (SatoriRegion::BITMAP_LENGTH - 1);
    size_t mask = (size_t)1 << ((word >> 3) & 63);

    //NB: this does not need to be a full fence, if possible. 
    //    just need to set a bit atomically.
    //    ordering WRT other writes is unimportant, as long as the bit is set.
#ifdef _MSC_VER
#if defined(TARGET_AMD64)
    _InterlockedOr64((long long*)&m_bitmap[bitmapIndex], mask);
#else
    _InterlockedOr64_nf((long long*)&m_bitmap[bitmapIndex], mask);
#endif
#else
    __atomic_or_fetch(&m_bitmap[bitmapIndex], mask, __ATOMIC_RELAXED);
#endif
}

inline void SatoriRegion::ClearMarked(SatoriObject* o)
{
    _ASSERTE(o->SameRegion(this));

    size_t word = o->Start();
    size_t bitmapIndex = (word >> 9) & (SatoriRegion::BITMAP_LENGTH - 1);
    size_t mask = (size_t)1 << ((word >> 3) & 63);

    m_bitmap[bitmapIndex] &= ~mask;
}

inline bool SatoriRegion::IsMarked(SatoriObject* o)
{
    _ASSERTE(o->SameRegion(this));

    size_t word = o->Start();
    size_t bitmapIndex = (word >> 9) & (SatoriRegion::BITMAP_LENGTH - 1);
    size_t mask = (size_t)1 << ((word >> 3) & 63);

    return m_bitmap[bitmapIndex] & mask;
}

inline bool SatoriRegion::CheckAndClearMarked(SatoriObject* o)
{
    _ASSERTE(o->SameRegion(this));

    size_t word = o->Start();
    size_t bitmapIndex = (word >> 9) & (SatoriRegion::BITMAP_LENGTH - 1);
    size_t mask = (size_t)1 << ((word >> 3) & 63);

    volatile size_t& bitmapWord = m_bitmap[bitmapIndex];
    bool wasMarked = bitmapWord & mask;
    bitmapWord &= ~mask;
    return wasMarked;
}

inline bool SatoriRegion::IsPinned(SatoriObject* o)
{
    return IsMarked(o + MarkOffset::Pinned);
}

inline void SatoriRegion::SetPinned(SatoriObject* o)
{
    SetMarked(o + MarkOffset::Pinned);
}

inline void SatoriRegion::ClearPinnedAndMarked(SatoriObject* o)
{
    ClearMarked(o);
    if (IsPinned(o))
    {
        // this would be rare. do not inline.
        ClearPinned(o);
    }
}

inline bool SatoriRegion::IsEscaped(SatoriObject* o)
{
    return IsMarked(o + MarkOffset::Escaped);
}

inline void SatoriRegion::SetEscaped(SatoriObject* o)
{
    SetMarked(o + MarkOffset::Escaped);
}

inline bool SatoriRegion::IsEscapedOrPinned(SatoriObject* o)
{
    return IsEscaped(o) || IsPinned(o) || o->IsUnmovable();
}

inline void SatoriRegion::SetExposed(SatoriObject** location)
{
    // set the mark bit corresponding to the location to indicate that it is globally exposed
    // same as: SetMarked((SatoriObject*)location);
    SetMarked((SatoriObject*)location);
}

inline bool SatoriRegion::IsExposed(SatoriObject** location)
{
    // check the mark bit corresponding to the location
    //same as: return IsMarked((SatoriObject*)location);
    return IsMarked((SatoriObject*)location);
}

inline SatoriObject* SatoriRegion::ObjectForMarkBit(size_t bitmapIndex, int offset)
{
    size_t objOffset = bitmapIndex * sizeof(size_t) * 8 + offset;
    return (SatoriObject*)(Start()) + objOffset;
}

inline size_t SatoriRegion::LocationToIndex(size_t location)
{
    return (location - Start()) >> Satori::INDEX_GRANULARITY_BITS;
}

inline void SatoriRegion::SetIndicesForObject(SatoriObject* o, size_t end)
{
    _ASSERTE(end > o->Start());
    _ASSERTE(end <= Start() + Satori::REGION_SIZE_GRANULARITY);

    size_t start = o->Start();

    // if object straddles index granules, record the object in corresponding indices
    if ((start ^ end) >> Satori::INDEX_GRANULARITY_BITS)
    {
        SetIndicesForObjectCore(start, end);
    }
}

inline void SatoriRegion::ClearIndicesForAllocRange()
{
    _ASSERTE(m_allocStart < m_allocEnd);
    _ASSERTE(m_allocStart > Start());

    size_t start = m_allocStart;
    size_t end = min(m_allocEnd, Start() + Satori::REGION_SIZE_GRANULARITY);

    // if range straddles index granules, clear corresponding indices
    if ((start ^ end) >> Satori::INDEX_GRANULARITY_BITS)
    {
        size_t firstIndex = LocationToIndex(start) + 1;
        // if first is clean, all are clean
        if (m_index[firstIndex] != 0)
        {
            size_t lastIndex = LocationToIndex(end);
            memset((void*)&m_index[firstIndex], 0, (lastIndex - firstIndex + 1) * sizeof(int));
        }

#if _DEBUG
        for(size_t i = firstIndex, last = LocationToIndex(end); i <= last; i++)
        {
            _ASSERTE(m_index[i] == 0);
        }
#endif
    }
}

template <bool promotingAllRegions>
void SatoriRegion::UpdatePointersInPromotedObjects()
{
    _ASSERTE(HasMarksSet());

    size_t objLimit = Start() + Satori::REGION_SIZE_GRANULARITY;
    SatoriObject* o = FirstObject();
    do
    {
        o = SkipUnmarked(o);
        if (o->Start() >= objLimit)
        {
            _ASSERTE(o->Start() == objLimit);
            break;
        }

        _ASSERTE(IsMarked(o));

        SatoriObject* promoted = o->RelocatedToUnchecked();
        _ASSERTE(!promoted->IsFree());

        SatoriPage* page = promoted->ContainingRegion()->ContainingPage();
        size_t size = promoted->Size();
        promoted->ForEachObjectRef(
            [&](SatoriObject** ppObject)
            {
                // prevent re-reading o, UpdatePointersThroughCards could be doing the same update.
                SatoriObject* child = VolatileLoadWithoutBarrier(ppObject);
                if (child &&
                    !child->IsExternal())
                {
                    SatoriObject* newLocation;
                    if (child->IsRelocatedTo</*notExternal*/true>(&newLocation))
                    {
                        VolatileStoreWithoutBarrier(ppObject, newLocation);
                    }

                    // update the card as if the relocated object got a child assigned
                    if (!promotingAllRegions)
                    {
                        if (child->ContainingRegion()->Generation() < 2)
                        {
                            page->SetCardForAddress((size_t)ppObject);
                        }
                    }
                }
            },
            size
        );

        o = (SatoriObject*)(o->Start() + size);
    } while (o->Start() < objLimit);
}

inline int SatoriRegion::IncrementUnfinishedAlloc()
{
    return (int)Interlocked::Increment(&m_unfinishedAllocationCount);
}

inline void SatoriRegion::DecrementUnfinishedAlloc()
{
    Interlocked::Decrement(&m_unfinishedAllocationCount);
    _ASSERTE((int)m_unfinishedAllocationCount >= 0);
}

#endif
